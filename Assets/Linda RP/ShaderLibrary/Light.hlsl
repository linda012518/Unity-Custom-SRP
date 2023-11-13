#ifndef Linda_Light
#define Linda_Light

#define Max_Directional_Light_Count 4
#define Max_Other_Light_Count 64

CBUFFER_START(_LindaLight)
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[Max_Directional_Light_Count];
	float4 _DirectionalLightDirectionsAndMasks[Max_Directional_Light_Count];
	float4 _DirectionalLightShadowData[Max_Directional_Light_Count];

	int _OtherLightCount;
	float4 _OtherLightColors[Max_Other_Light_Count];
	float4 _OtherLightPositions[Max_Other_Light_Count];
	float4 _OtherLightDirectionsAndMasks[Max_Other_Light_Count];
	float4 _OtherLightSpotAngles[Max_Other_Light_Count];
	float4 _OtherLightShadowData[Max_Other_Light_Count];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
	uint renderingLayerMask;
};

int GetDirectionalCount()
{
	return _DirectionalLightCount;
}

int GetOtherLightCount()
{
	return _OtherLightCount;	
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

OtherShadowData GetOtherShadowData (int lightIndex) {
	OtherShadowData data;
	data.strength = _OtherLightShadowData[lightIndex].x;
	data.tileIndex = _OtherLightShadowData[lightIndex].y;
	data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
	data.isPoint = _OtherLightShadowData[lightIndex].z == 1.0;
	data.lightPositionWS = 0.0;
	data.lightDirectionWS = 0.0;
	data.spotDirectionWS = 0.0;
	return data;
}

Light GetDirectionalLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _DirectionalLightColors[index].xyz;
	light.direction = _DirectionalLightDirectionsAndMasks[index].xyz;
	light.renderingLayerMask = asuint(_DirectionalLightDirectionsAndMasks[index].w);
	DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, shadowData);
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	//light.attenuation = shadowData.cascadeIndex * 0.25;//查看级联球
	return light; 
}

//点光源衰减(但所有物体都会计算)： 强度 / 距离平方
//点光源强度衰减：(max(0, 1 - (距离平方/半径平方) * (距离平方/半径平方)))平方，半径平方倒数在CPU逻辑预计算，存位置W分量
//聚光灯衰减：saturate(dot(聚光灯方向, 片元的聚光灯方向) * a + b)^2
//a = 1 / (cos(内角 / 2) - cos(外角 / 2))
//b = -cos(外角 / 2) * a
Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData)
{
	Light light;
	light.color = _OtherLightColors[index].xyz;
	float3 position = _OtherLightPositions[index].xyz;
	float3 ray = position - surfaceWS.position;
	light.direction = normalize(ray);
	float distanceSqr = max(dot(ray, ray), 0.00001);
	//点光源强度衰减
	float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w)));

	float4 spotAngles = _OtherLightSpotAngles[index];
	float3 spotDirection = _OtherLightDirectionsAndMasks[index].xyz;
	light.renderingLayerMask = asuint(_OtherLightDirectionsAndMasks[index].w);
	float spotAttenuation = Square(saturate(dot(spotDirection, light.direction) * spotAngles.x + spotAngles.y));
	OtherShadowData otherShadowData = GetOtherShadowData(index);
	otherShadowData.lightPositionWS = position;
	otherShadowData.lightDirectionWS = light.direction;
	otherShadowData.spotDirectionWS = spotDirection;
	light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS) * spotAttenuation * rangeAttenuation / distanceSqr;
	return light; 
}

#endif
