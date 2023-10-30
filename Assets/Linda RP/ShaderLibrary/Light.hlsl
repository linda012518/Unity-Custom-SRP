#ifndef Linda_Light
#define Linda_Light

#define Max_Directional_Light_Count 4

CBUFFER_START(_LindaLight)
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[Max_Directional_Light_Count];
	float4 _DirectionalLightDirections[Max_Directional_Light_Count];
	float4 _DirectionalLightShadowData[Max_Directional_Light_Count];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
};

int GetDirectionalCount()
{
	return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
	DirectionalShadowData data = (DirectionalShadowData)0;
	data.strength = _DirectionalLightShadowData[lightIndex].x;// * shadowData.strength;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
	return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _DirectionalLightColors[index].xyz;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	//light.attenuation = shadowData.cascadeIndex * 0.25;//查看级联球
	return light;
}

#endif
