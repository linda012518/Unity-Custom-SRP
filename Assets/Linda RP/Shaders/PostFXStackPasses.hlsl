#ifndef Linda_PostFXStack_Passes
#define Linda_PostFXStack_Passes

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 screenUV : TEXCOORD0;
};

TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
SAMPLER(sampler_linear_clamp);

float4 _ProjectionParams;

float4 _PostFXSource_TexelSize;

bool _BloomBicubicUpsampling;

float4 _BloomThreshold;

float _BloomIntensity;

float4 GetSourceTexelSize () {
	return _PostFXSource_TexelSize;
}

float4 GetSource(float2 screenUV) 
{
	//用lod主动规避mipmap
	return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV, 0);
}

float4 GetSource2(float2 screenUV) {
	return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV, 0);
}

float4 GetSourceBicubic (float2 screenUV) {
	return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp), screenUV, _PostFXSource_TexelSize.zwxy, 1.0, 0.0);
}

/*

3	*
2	* *
1	+ + +
0	+ + + * *
-1	+ + + * *

*/
//SV_VertexID 代表传入的是顶点ID序号：0 1 2 3 ......
//一个三角形铺满屏幕，直接返回-1~1顶点数据，最终得到+号区域
Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
	Varyings output;

	output.positionCS = float4(
	vertexID <= 1 ? -1.0 : 3.0,
	vertexID == 1 ? 3.0 : -1.0,
	0.0, 1.0
	);

	output.screenUV = float2(
		vertexID <= 1 ? 0.0 : 2.0,
		vertexID == 1 ? 2.0 : 0.0
	);

	if (_ProjectionParams.x < 0.0) 
	{
		output.screenUV.y = 1.0 - output.screenUV.y;
	}
	return output;
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
	return GetSource(input.screenUV);
}

float4 BloomHorizontalPassFragment (Varyings input) : SV_TARGET {
	float3 color = 0.0;
	float offsets[] = { -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923 };
	float weights[] = {
		0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	};
	for (int i = 0; i < 5; i++) {
		float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
		color += GetSource(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
	}
	return float4(color, 1.0);
}

float4 BloomVerticalPassFragment (Varyings input) : SV_TARGET {
	float3 color = 0.0;
	float offsets[] = { -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923 };
	float weights[] = {
		0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	};
	for (int i = 0; i < 5; i++) {
		float offset = offsets[i] * GetSourceTexelSize().y;
		color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
	}
	return float4(color, 1.0);
}

float4 BloomCombinePassFragment (Varyings input) : SV_TARGET {
	float3 lowRes;
	if (_BloomBicubicUpsampling) 
	{ 
		lowRes = GetSourceBicubic(input.screenUV).rgb;
	}
	else
	{
		lowRes = GetSource(input.screenUV).rgb;
	}
	
	float3 highRes = GetSource2(input.screenUV).rgb;
	return float4(lowRes * _BloomIntensity + highRes, 1.0);
}

//计算权重，不是平均相加
//_BloomThreshold：x=t;y=-t+tk;z=2tk;w=1/4tk+0.00001
//weight = max(s, b - t) / max(b, 0.00001)
//s = min(max(0, b - t + tk), 2tk)^2 / 4tk + 0.00001
//b 是亮度，t k 是输入slider
float3 ApplyBloomThreshold (float3 color) {
	float brightness = Max3(color.r, color.g, color.b);
	float soft = brightness + _BloomThreshold.y;
	soft = clamp(soft, 0.0, _BloomThreshold.z);
	soft = soft * soft * _BloomThreshold.w;
	float contribution = max(soft, brightness - _BloomThreshold.x);
	contribution /= max(brightness, 0.00001);
	return color * contribution;
}

float4 BloomPrefilterPassFragment (Varyings input) : SV_TARGET {
	float3 color = ApplyBloomThreshold(GetSource(input.screenUV).rgb);
	return float4(color, 1.0);
}

//权重公式 weight = 1 / 1 + l
//l = Luminance(color)
float4 BloomPrefilterFirefliesPassFragment (Varyings input) : SV_TARGET {
	float3 color = 0.0;
	float weightSum = 0.0;

	float2 offsets[] = {
		float2(0.0, 0.0),
		float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0),
		float2(-1.0, 0.0), float2(1.0, 0.0), float2(0.0, -1.0), float2(0.0, 1.0)
	};
	for (int i = 0; i < 9; i++) {
		float3 c = GetSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
		c = ApplyBloomThreshold(c);

		float w = 1.0 / (Luminance(c) + 1.0);
		color += c * w;
		weightSum += w;
	}
	color /= weightSum;
	return float4(color, 1.0);
}

#endif
