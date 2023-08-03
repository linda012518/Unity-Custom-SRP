#ifndef Linda_Shadows
#define Linda_Shadows

#define Max_Shadowed_Directional_Light_Count 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare //定义合适的阴影采样器
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_LindaShadows)
	float4 _DirectionalShadowMatrices[Max_Shadowed_Directional_Light_Count];
CBUFFER_END

struct DirectionalShadowData
{
	float strength;
	int tileIndex;
};

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
	return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, float3(0.5,0.5,0.5));
}

float GetDirectionalShadowAttenuation(DirectionalShadowData data, Surface surfaceWS)
{
	if (data.strength <= 0.0)
	{
		return 1.0;
	}
	float4 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(surfaceWS.position, 1.0));
	positionSTS.xyz /= positionSTS.w;
	float shadow = SampleDirectionalShadowAtlas(positionSTS.xyz);
	return shadow;
	return lerp(1.0, shadow, data.strength);
}

#endif
