#ifndef Linda_Surface
#define Linda_Surface

struct Surface
{
	float3 position;
	float3 normal;
	float3 viewDirection;
	float depth;//�۲�ռ����
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
	float dither;
};

#endif
