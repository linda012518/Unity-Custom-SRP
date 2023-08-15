#ifndef Linda_Shadows
#define Linda_Shadows

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(_DIRECTIONAL_PCF3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define Max_Shadowed_Directional_Light_Count 4
#define Max_Cascade_Count 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare //定义合适的阴影采样器
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_LindaShadows)
	int _CascadeCount;
	float4 _ShadowAtlasSize;
	float4 _ShadowDistanceFade;
	float4 _CascadeCullingShperes[Max_Cascade_Count];
	float4 _CascadeData[Max_Cascade_Count];
	float4x4 _DirectionalShadowMatrices[Max_Shadowed_Directional_Light_Count * Max_Cascade_Count];
CBUFFER_END

struct ShadowData
{
	int cascadeIndex;
	float strength;
	float cascadeBlend;
};

struct DirectionalShadowData
{
	float strength;
	int tileIndex;
	float normalBias;
};

//(1 - depth / maxDistance) / fade			最大距离淡入淡出范围就是fade
//(1 - maxDistance * maxDistance / radius * radius) / (1 - (1 - fade) * (1 - fade))  最后一个级联淡入淡出
float FadedShadowStrength (float distance, float scale, float fade) {
	return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
	ShadowData data;
	////对比最大距离和深度，超过最大距离给0，不要阴影
	//data.strength = surfaceWS.depth < _ShadowDistance ? 1.0 : 0.0;
	//最大距离淡入淡出阴影边
	data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
	data.cascadeBlend = 1.0;
	int i = 0;
	for (int i = 0; i < _CascadeCount; i++)
	{
		float4 sphere = _CascadeCullingShperes[i];
		float distance = DistanceSquared(sphere.xyz, surfaceWS.position);
		//_CascadeCullingShperes是从小到大传入的
		if (distance < _CascadeCullingShperes[i].w)
		{
			float fade = FadedShadowStrength(distance, 1.0 / sphere.w, _ShadowDistanceFade.z);
			if (i == _CascadeCount - 1) 
			{ 
				//计算最后一个级联淡入淡出
				data.strength *= fade;
			}
			else
			{
				data.cascadeBlend = fade;
			}
			break;
		}
	}
	if (i == _CascadeCount) 
	{ 
		//超出距离就不要阴影
		data.strength = 0.0;
	}
	data.cascadeIndex = i;
	return data;
}

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow (float3 positionSTS) {
	#if defined(DIRECTIONAL_FILTER_SETUP)
		float weights[DIRECTIONAL_FILTER_SAMPLES];
		float2 positions[DIRECTIONAL_FILTER_SAMPLES];
		float4 size = _ShadowAtlasSize.yyxx;
		DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
		float shadow = 0;
		for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) {
			shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
		}
		return shadow;
	#else
		return SampleDirectionalShadowAtlas(positionSTS);
	#endif
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData shadowData, Surface surfaceWS)
{
	if (directional.strength <= 0.0)
	{
		return 1.0;
	}
	float3 normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[shadowData.cascadeIndex].y);
	float4 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position + normalBias, 1.0));
	positionSTS.xyz /= positionSTS.w;
	float shadow = FilterDirectionalShadow(positionSTS.xyz);
	//如果小于1，则处理阴影级联过渡区，从下个级联采样差值
	if (shadowData.cascadeBlend < 1.0) {
		normalBias = surfaceWS.normal *(directional.normalBias * _CascadeData[shadowData.cascadeIndex + 1].y);
		positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0));
		shadow = lerp(FilterDirectionalShadow(positionSTS.xyz), shadow, shadowData.cascadeBlend);
	}

	return lerp(1.0, shadow, directional.strength);
}

#endif
