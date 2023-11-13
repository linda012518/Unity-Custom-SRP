#ifndef Linda_Lighting
#define Linda_Lighting

bool RenderingLayersOverlap (Surface surface, Light light) {
	return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

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
	data.shadowMask = gi.shadowMask;
	//return gi.shadowMask.shadows.rgb;

	float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
	for (int i = 0; i < GetDirectionalCount(); i++)
	{
		Light light = GetDirectionalLight(i, surface, data);
		if (RenderingLayersOverlap(surface, light)) {
			color += GetLighting(surface, brdf, light);
		}
	}

#if defined(_LIGHTS_PER_OBJECT)
		for (int j = 0; j < min(unity_LightData.y, 8); j++) 
		{
			int lightIndex = unity_LightIndices[(uint)j / 4][(uint)j % 4];
			Light light = GetOtherLight(lightIndex, surface, data);
			if (RenderingLayersOverlap(surface, light)) {
				color += GetLighting(surface, brdf, light);
			}
		}
#else
	for (int n = 0; n < GetOtherLightCount(); n++)
	{
		Light light = GetOtherLight(n, surface, data);
		if (RenderingLayersOverlap(surface, light)) {
			color += GetLighting(surface, brdf, light);
		}
	}
#endif

	return color;
}

#endif
