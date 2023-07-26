#ifndef Linda_Surface
#define Linda_Surface

struct Surface
{
	float3 normal;
	float3 viewDirection;
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
};

#endif
