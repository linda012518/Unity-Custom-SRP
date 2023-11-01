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
#define SHADOW_SAMPLER sampler_linear_clamp_compare //������ʵ���Ӱ������
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_LindaShadows)
	int _CascadeCount;
	float4 _ShadowAtlasSize;
	float4 _ShadowDistanceFade;
	float4 _CascadeCullingShperes[Max_Cascade_Count];
	float4 _CascadeData[Max_Cascade_Count];
	float4x4 _DirectionalShadowMatrices[Max_Shadowed_Directional_Light_Count * Max_Cascade_Count];
CBUFFER_END

struct ShadowMask {
	bool always;
	bool distance;
	float4 shadows;
};

struct ShadowData
{
	int cascadeIndex;
	float strength;
	float cascadeBlend;
	ShadowMask shadowMask;
};

struct OtherShadowData {
	float strength;
	int shadowMaskChannel;
};

struct DirectionalShadowData
{
	float strength;
	int tileIndex;
	float normalBias;//�����������ཻ������Ӱ������б�����õ���
	int shadowMaskChannel;
};

//(1 - depth / maxDistance) / fade			�����뵭�뵭����Χ����fade
//(1 - maxDistance * maxDistance / radius * radius) / (1 - (1 - fade) * (1 - fade))  ���һ���������뵭��
float FadedShadowStrength (float distance, float scale, float fade) {
	return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
	ShadowData data;
	data.shadowMask.always = false;
	data.shadowMask.distance = false;
	data.shadowMask.shadows = 1.0;
	////�Ա����������ȣ������������0����Ҫ��Ӱ
	//data.strength = surfaceWS.depth < _ShadowDistance ? 1.0 : 0.0;
	//�����뵭�뵭����Ӱ��
	data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
	data.cascadeBlend = 1.0;
	int i = 0;
	for (int i = 0; i < _CascadeCount; i++)
	{
		float4 sphere = _CascadeCullingShperes[i];
		float distance = DistanceSquared(sphere.xyz, surfaceWS.position);
		//_CascadeCullingShperes�Ǵ�С�������
		if (distance < _CascadeCullingShperes[i].w)
		{
			float fade = FadedShadowStrength(distance, 1.0 / sphere.w, _ShadowDistanceFade.z);
			if (i == _CascadeCount - 1) 
			{ 
				//�������һ���������뵭��
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
		//��������Ͳ�Ҫ��Ӱ
		data.strength = 0.0;
	}
#if defined(_CASCADE_BLEND_DITHER)
	//��Ӱdither����������л�������Ӱͼ��
	else if (data.cascadeBlend < surfaceWS.dither) {
		i += 1;
	}
#endif
#if !defined(_CASCADE_BLEND_SOFT)
	data.cascadeBlend = 1.0;
#endif
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

float GetCascadedShadow (DirectionalShadowData directional, ShadowData shadowData, Surface surfaceWS) 
{
	float3 normalBias = surfaceWS.interpolatedNormal * (directional.normalBias * _CascadeData[shadowData.cascadeIndex].y);
	float4 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position + normalBias, 1.0));
	//positionSTS.xyz /= positionSTS.w; //���ﲻ�ܳ�w������Զ�����ڣ����о�
	float shadow = FilterDirectionalShadow(positionSTS.xyz);
	//���С��1��������Ӱ���������������¸�����������ֵ
	if (shadowData.cascadeBlend < 1.0) {
		normalBias = surfaceWS.interpolatedNormal *(directional.normalBias * _CascadeData[shadowData.cascadeIndex + 1].y);
		positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0));
		shadow = lerp(FilterDirectionalShadow(positionSTS.xyz), shadow, shadowData.cascadeBlend);
	}

	return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel)
{
	float shadow = 1.0;
	if (mask.always || mask.distance) 
	{ 
		if (channel >= 0) 
		{ 
			shadow = mask.shadows[channel];
		}
	}
	return shadow;
}

float GetBakedShadow(ShadowMask mask, int channel, float strength)
{
	if (mask.always || mask.distance) 
	{ 
		return lerp(1.0, GetBakedShadow(mask, channel), strength);
	}
	return 1.0;
}

float MixBakedAndRealtimeShadows(ShadowData shadowData, float shadow, int shadowMaskChannel, float strength)
{
	float baked = GetBakedShadow(shadowData.shadowMask, shadowMaskChannel);
	//����ShadowMask��Ӱģʽ����̬����û��ʵʱ��Ӱ
	if (shadowData.shadowMask.always) 
	{
		//��ȡʱʵ��Ӱ����ȡ�決��Ӱ�Ա��ĸ���Ӱ�����ĸ�
		shadow = lerp(1.0, shadow, shadowData.strength);
		shadow = min(baked, shadow);
		return lerp(1.0, shadow, strength);
	}
	//����Distance ShadowMask��Ӱģʽ���������嶼��ʵʱ��Ӱ��ͨ�������жϾ�̬������ʵʱ��決
	if (shadowData.shadowMask.distance) 
	{ 
		shadow = lerp(baked, shadow, shadowData.strength);
		return lerp(1.0, shadow, strength);
	}
	return lerp(1.0, shadow, strength * shadowData.strength);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directional, ShadowData shadowData, Surface surfaceWS)
{
#if !defined(_RECEIVE_SHADOWS)
	return 1.0;
#endif
	
	float shadow;

	if (directional.strength * shadowData.strength <= 0.0)
	{
		shadow = GetBakedShadow(shadowData.shadowMask, directional.shadowMaskChannel, directional.strength);
	}
	else
	{
		shadow = GetCascadedShadow(directional, shadowData, surfaceWS);
		shadow = MixBakedAndRealtimeShadows(shadowData, shadow, directional.shadowMaskChannel, directional.strength);
	}


	return shadow;
}

float GetOtherShadowAttenuation (OtherShadowData other, ShadowData shadowData, Surface surfaceWS) 
{
	#if !defined(_RECEIVE_SHADOWS)
		return 1.0;
	#endif
	
	float shadow;
	if (other.strength > 0.0) 
	{
		shadow = GetBakedShadow(shadowData.shadowMask, other.shadowMaskChannel, other.strength);
	}
	else 
	{
		shadow = 1.0;
	}
	return shadow;
}

#endif
