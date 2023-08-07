#ifndef Linda_Lighting
#define Linda_Lighting

float3 IncomingLighting(Surface surface, Light light)
{
	return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting(Surface surface, BRDF brdf, Light light)
{
	return IncomingLighting(surface, light) * DirectBRDF(surface, brdf, light) * light.attenuation;
}

float3 GetLighting(Surface surface, BRDF brdf)
{
	Light light = GetDirectionalLight(0, surface);

	float3 color = 0.0;
	for (int i = 0; i < GetDirectionalCount(); i++)
	{
		color += GetLighting(surface, brdf, GetDirectionalLight(i, surface));
	}
	return color;
}

#endif
