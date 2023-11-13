#ifndef Linda_Surface
#define Linda_Surface

struct Surface
{
	float3 position;
	float3 normal;
	float3 interpolatedNormal;
	float3 viewDirection;
	float depth;//�۲�ռ����
	float3 color;
	float alpha;
	float metallic;
	float occlusion;
	float smoothness;
	float fresnelStrength;
	float dither;
	uint renderingLayerMask;
};

#endif
