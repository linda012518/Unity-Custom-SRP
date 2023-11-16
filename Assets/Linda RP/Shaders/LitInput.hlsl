#ifndef Linda_Lit_Input
#define Linda_Lit_Input

TEXTURE2D(_NormalMap);
TEXTURE2D(_BaseMap);
TEXTURE2D(_MaskMap); //M(Metallic) O(Occlusion) D(Detail) S(Smoothness)
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_DetailNormalMap);
TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial) //CBUFFER_START�̶�д����UnityPerMaterial
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
	UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
	UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
	UNITY_DEFINE_INSTANCED_PROP(float, _Occlusion)
	UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _Fresnel)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)
	UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
	UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define Input_Prop(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig {
	Fragment fragment;
	float2 baseUV;
	float2 detailUV;
	bool useMask;
	bool useDetail;
};

InputConfig GetInputConfig (float4 positionSS, float2 baseUV, float2 detailUV = 0.0) {
	InputConfig c;
	c.fragment = GetFragment(positionSS);
	c.baseUV = baseUV;
	c.detailUV = detailUV;
	c.useMask = false;
	c.useDetail = false;
	return c;
}

float2 TransformBaseUV (float2 baseUV) {
	float4 baseST = Input_Prop(_BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}

float2 TransformDetailUV (float2 detailUV) {
	float4 detailST = Input_Prop(_DetailMap_ST);
	return detailUV * detailST.xy + detailST.zw;
}

float4 GetMask (InputConfig c) {
	if (c.useMask) {
		return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, c.baseUV);
	}
	return 1.0;
}

float4 GetDetail (InputConfig c) {
	if (c.useDetail) {
		float4 map = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, c.detailUV);
		return map * 2.0 - 1.0;
	}
	return 0.0;
}

float4 GetBase (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
	float4 color = Input_Prop(_BaseColor);

	if (c.useDetail) {
		float detail = GetDetail(c).r * Input_Prop(_DetailAlbedo);
		float mask = GetMask(c).b;
		map.rgb = lerp(sqrt(map.rgb), detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
		map.rgb *= map.rgb;
	}

	return map * color;
}

float3 GetEmission (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, c.baseUV);
	float4 color = Input_Prop(_EmissionColor);
	return map.rgb * color.rgb;
}

float GetCutoff (InputConfig c) {
	return Input_Prop(_Cutoff);
}

float GetMetallic (InputConfig c) {
	float metallic = Input_Prop(_Metallic);
	metallic *= GetMask(c).r;
	return metallic;
}

float GetSmoothness (InputConfig c) {
	float smoothness = Input_Prop(_Smoothness);
	smoothness *= GetMask(c).a;

	if (c.useDetail) {
		float detail = GetDetail(c).b * Input_Prop(_DetailSmoothness);
		float mask = GetMask(c).b;
		smoothness = lerp(smoothness, detail < 0.0 ? 0.0 : 1.0, abs(detail) * mask);
	}

	return smoothness;
}

float GetFresnel (InputConfig c) {
	return Input_Prop(_Fresnel);
}

float GetOcclusion (InputConfig c) {
	return lerp(GetMask(c).g, 1.0, Input_Prop(_Occlusion));
}

float3 GetNormalTS (InputConfig c) {
	float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, c.baseUV);
	float scale = Input_Prop(_NormalScale);
	float3 normal = DecodeNormal(map, scale);

	if (c.useDetail) {
		map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, c.detailUV);
		scale = Input_Prop(_DetailNormalScale) * GetMask(c).b;
		float3 detail = DecodeNormal(map, scale);
		normal = BlendNormalRNM(normal, detail);
	}

	return normal;
}

float GetFinalAlpha (float alpha) {
	return Input_Prop(_ZWrite) ? 1.0 : alpha;
}

#endif
