#ifndef Linda_Shadows
#define Linda_Shadows

#define Max_Shadowed_Directional_Light_Count 4
#define Max_Cascade_Count 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare //������ʵ���Ӱ������
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_LindaShadows)
	int _CascadeCount;
	float4 _ShadowDistanceFade;
	float4 _CascadeCullingShperes[Max_Cascade_Count];
	float4 _CascadeData[Max_Cascade_Count];
	float4x4 _DirectionalShadowMatrices[Max_Shadowed_Directional_Light_Count * Max_Cascade_Count];
CBUFFER_END

struct ShadowData
{
	int cascadeIndex;
	float strength;
};

struct DirectionalShadowData
{
	float strength;
	int tileIndex;
	float normalBias;
};

//(1 - depth / maxDistance) / fade			�����뵭�뵭����Χ����fade
//(1 - maxDistance * maxDistance / radius * radius) / (1 - (1 - fade) * (1 - fade))  ���һ���������뵭��
float FadedShadowStrength (float distance, float scale, float fade) {
	return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData(Surface surfaceWS)
{
	ShadowData data;
	////�Ա����������ȣ������������0����Ҫ��Ӱ
	//data.strength = surfaceWS.depth < _ShadowDistance ? 1.0 : 0.0;
	//�����뵭�뵭����Ӱ��
	data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
	int i = 0;
	for (int i = 0; i < _CascadeCount; i++)
	{
		float4 sphere = _CascadeCullingShperes[i];
		float distance = DistanceSquared(sphere.xyz, surfaceWS.position);
		//_CascadeCullingShperes�Ǵ�С�������
		if (distance < _CascadeCullingShperes[i].w)
		{
			if (i == _CascadeCount - 1) 
			{ 
				data.strength *= FadedShadowStrength(distance, 1.0 / sphere.w, _ShadowDistanceFade.z);
			}
			break;
		}
	}
	if (i == _CascadeCount) 
	{ 
		//��������Ͳ�Ҫ��Ӱ
		data.strength = 0.0;
	}
	data.cascadeIndex = i;
	return data;
}

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
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
	float shadow = SampleDirectionalShadowAtlas(positionSTS.xyz);
	return lerp(1.0, shadow, directional.strength);
}

#endif
