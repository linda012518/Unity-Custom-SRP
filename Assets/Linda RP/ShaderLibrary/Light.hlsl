#ifndef Linda_Light
#define Linda_Light

#define Max_Directional_Light_Count 4

CBUFFER_START(_LindaLight)
	float4 _DirectionalLightColors[Max_Directional_Light_Count];
	float4 _DirectionalLightDirections[Max_Directional_Light_Count];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
};

int GetDirectionalCount()
{
	return Max_Directional_Light_Count;
}

Light GetDirectionalLight(int index)
{
	Light light;
	light.color = _DirectionalLightColors[index].xyz;
	light.direction = _DirectionalLightDirections[index].xyz;
	return light;
}

#endif
