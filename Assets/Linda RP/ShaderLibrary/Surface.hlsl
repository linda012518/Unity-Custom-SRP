#ifndef Linda_Surface
#define Linda_Surface

struct Surface
{
	float3 position;
	float3 normal;
	float3 viewDirection;
	float depth;//观察空间深度
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
	float dither;
};

#endif
