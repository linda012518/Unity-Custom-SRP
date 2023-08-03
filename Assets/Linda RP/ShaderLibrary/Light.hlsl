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
	float attenaution;
};

int GetDirectionalCount()
{
	return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex)
{
	DirectionalShadowData data = (DirectionalShadowData)0;
	data.strength = _DirectionalLightShadowData[lightIndex].x;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
	return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS)
{
	Light light;
	light.color = _DirectionalLightColors[index].xyz;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData shadowData = GetDirectionalShadowData(index);
	light.attenaution = GetDirectionalShadowAttenuation(shadowData, surfaceWS);
	return light;
}

#endif
