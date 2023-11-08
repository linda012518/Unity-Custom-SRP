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

//引入亮度阈值来限制影响Bloom效果，这样就会是全局都亮
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

//HDR会产生比周围亮很多的区域，当区域很小到一个象素时，转动相机会导致这个区域时有进无，加上bloom会闪烁，采样周围象素加权平均可解
//权重公式 weight = 1 / 1 + l
//l = Luminance(color)
float4 BloomPrefilterFirefliesPassFragment (Varyings input) : SV_TARGET {
	float3 color = 0.0;
	float weightSum = 0.0;

	float2 offsets[] = {
		float2(0.0, 0.0),
		float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)
	};
	for (int i = 0; i < 5; i++) {
		float3 c = GetSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
		c = ApplyBloomThreshold(c);

		float w = 1.0 / (Luminance(c) + 1.0);
		color += c * w;
		weightSum += w;
	}
	color /= weightSum;
	return float4(color, 1.0);
}

float4 BloomScatterPassFragment (Varyings input) : SV_TARGET {
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

	return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

float4 BloomScatterFinalPassFragment (Varyings input) : SV_TARGET {
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
	lowRes += highRes - ApplyBloomThreshold(highRes);

	return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

float4 _ColorAdjustments;
float4 _ColorFilter;
float4 _WhiteBalance;
float4 _SplitToningShadows, _SplitToningHighlights;
float4 _ChannelMixerRed, _ChannelMixerGreen, _ChannelMixerBlue;
float4 _SMHShadows, _SMHMidtones, _SMHHighlights, _SMHRange;

float Luminance (float3 color, bool useACES) {
	return useACES ? AcesLuminance(color) : Luminance(color);
}

float3 ColorGradePostExposure (float3 color) {
	//曝光
	return color * _ColorAdjustments.x;
}

float3 ColorGradeWhiteBalance (float3 color) {
	color = LinearToLMS(color);
	color *= _WhiteBalance.rgb;
	return LMSToLinear(color);
}

float3 ColorGradingContrast (float3 color, bool useACES) {
	//对比度
	color = useACES ? ACES_to_ACEScc(unity_to_ACES(color)) : LinearToLogC(color);
	color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
	return useACES ? ACES_to_ACEScg(ACEScc_to_ACES(color)) : LogCToLinear(color);
}

float3 ColorGradeColorFilter (float3 color) {
	//颜色滤镜
	return color * _ColorFilter.rgb;
}

float3 ColorGradingHueShift (float3 color) {
	//色相
	color = RgbToHsv(color);
	float hue = color.x + _ColorAdjustments.z;
	color.x = RotateHue(hue, 0.0, 1.0);
	return HsvToRgb(color);
}

float3 ColorGradingSaturation (float3 color, bool useACES) {
	//饱和度
	float luminance = Luminance(color, useACES);
	return (color - luminance) * _ColorAdjustments.w + luminance;
}

float3 ColorGradeSplitToning (float3 color, bool useACES) {
	//色调分离
	color = PositivePow(color, 1.0 / 2.2);
	float t = saturate(Luminance(saturate(color), useACES) + _SplitToningShadows.w);
	float3 shadows = lerp(0.5, _SplitToningShadows.rgb, 1.0 - t);
	float3 highlights = lerp(0.5, _SplitToningHighlights.rgb, t);
	color = SoftLight(color, shadows);
	color = SoftLight(color, highlights);
	return PositivePow(color, 2.2);
	return PositivePow(color, 2.2);
}

float3 ColorGradingChannelMixer (float3 color) {
	return mul(
		float3x3(_ChannelMixerRed.rgb, _ChannelMixerGreen.rgb, _ChannelMixerBlue.rgb),
		color
	);
}

float3 ColorGradingShadowsMidtonesHighlights (float3 color, bool useACES) {
	//调阴影、中间调、高光
	float luminance = Luminance(color, useACES);
	float shadowsWeight = 1.0 - smoothstep(_SMHRange.x, _SMHRange.y, luminance);
	float highlightsWeight = smoothstep(_SMHRange.z, _SMHRange.w, luminance);
	float midtonesWeight = 1.0 - shadowsWeight - highlightsWeight;
	return
		color * _SMHShadows.rgb * shadowsWeight +
		color * _SMHMidtones.rgb * midtonesWeight +
		color * _SMHHighlights.rgb * highlightsWeight;
}

float3 ColorGrade(float3 color, bool useACES = false)
{
	color = min(color, 60);
	color = ColorGradePostExposure(color);
	color = ColorGradeWhiteBalance(color);
	color = ColorGradingContrast(color, useACES);
	color = ColorGradeColorFilter(color);
	color = max(color, 0.0);
	color = ColorGradeSplitToning(color, useACES);
	color = ColorGradingChannelMixer(color);
	color = max(color, 0.0);
	color = ColorGradingShadowsMidtonesHighlights(color, useACES);
	color = ColorGradingHueShift(color);
	color = ColorGradingSaturation(color, useACES);
	return  max(useACES ? ACEScg_to_ACES(color) : color, 0.0);
}

float4 ColorGradingNonePassFragment (Varyings input) : SV_TARGET {
	float4 color = GetSource(input.screenUV);
	color.rgb = ColorGrade(color.rgb);
	return color;
}

//公式：color / color + 1.0
float4 ToneMappingReinhardPassFragment (Varyings input) : SV_TARGET {
	float4 color = GetSource(input.screenUV);
	color.rgb = ColorGrade(color.rgb);
	color.rgb /= color.rgb + 1.0;
	return color;
}

float4 ToneMappingNeutralPassFragment (Varyings input) : SV_TARGET {
	float4 color = GetSource(input.screenUV);
	color.rgb = ColorGrade(color.rgb);
	color.rgb = NeutralTonemap(color.rgb);
	return color;
}

float4 ToneMappingACESPassFragment (Varyings input) : SV_TARGET {
	float4 color = GetSource(input.screenUV);
	color.rgb = ColorGrade(color.rgb, true);
	color.rgb = AcesTonemap(color.rgb);
	return color;
}

#endif
