#ifndef Linda_BRDF
#define Linda_BRDF

struct BRDF
{
	float3 diffuse;
	float3 specular;
	float roughness;
	float perceptualRoughness;
	float fresnel;
};

#define Min_Reflectivity 0.04

//����������Ϊ1 ȫ�Ǿ��淴���������䣻�ǽ���������Ϊ0.04 ������༸��û�о��淴��
float OneMinusReflectivity (float metallic) {
	float range = 1.0 - Min_Reflectivity;
	return range - metallic * range;
}

BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false)
{
	BRDF brdf;
	float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
	//����������Ϊ0
	brdf.diffuse = surface.color * oneMinusReflectivity;
	if (applyAlphaToDiffuse) 
	{ 
		brdf.diffuse *= surface.alpha;
	}
	
	//����Ӱ�쾵�淴�����ɫ���ǽ�����Ӱ�����ʵ
	brdf.specular = lerp(Min_Reflectivity, surface.color, surface.metallic);
	brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);//�ֲڶȵ�ƽ�� ����a2
	brdf.fresnel = saturate(surface.smoothness + 1.0 - oneMinusReflectivity);
	return brdf;
}

float SpecularStrength (Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

float3 IndirectBRDF(Surface surface, BRDF brdf, float diffuse, float specular)
{
	float3 indirectDiffuse = diffuse * brdf.diffuse;

	float fresnelStrength = surface.fresnelStrength * Pow4(1.0 - saturate(dot(surface.normal, surface.viewDirection)));
	float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
	reflection /= brdf.roughness * brdf.roughness + 1.0;

	return indirectDiffuse + reflection;
}

#endif
