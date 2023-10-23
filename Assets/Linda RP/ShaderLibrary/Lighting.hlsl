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

float3 GetLighting(Surface surface, BRDF brdf, GI gi)
{
	ShadowData data = GetShadowData(surface);

	float3 color = gi.diffuse * brdf.diffuse;
	for (int i = 0; i < GetDirectionalCount(); i++)
	{
		Light light = GetDirectionalLight(i, surface, data);
		color += GetLighting(surface, brdf, light);
	}
	return color;
}

#endif
