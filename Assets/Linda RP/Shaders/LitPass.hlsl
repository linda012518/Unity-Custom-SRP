#ifndef Linda_Lit_Pass
#define Linda_Lit_Pass

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

struct Attributes
{
	float3 positionOS : POSITION;
	float2 uv0 : TEXCOORD0;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	GI_ATTRIBUTE_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 uv0 : TEXCOORD0;
	float3 normalWS : TEXCOORD1;
	float3 positionWS : TEXCOORD2;
	float2 detailUV : TEXCOORD3;
#if defined(_NORMAL_MAP)
	float4 tangentWS : VAR_TANGENT;
#endif
	GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);//初始化实例化渲染ID
	UNITY_TRANSFER_INSTANCE_ID(input, output);////实例化渲染ID传到片元
	TRANSFER_GI_DATA(input, output);
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
	output.uv0 = TransformBaseUV(input.uv0);
#if defined(_DETAIL_MAP)
	output.detailUV = TransformDetailUV(input.uv0);
#endif
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_NORMAL_MAP)
	output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif
	return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);

//#if defined(LOD_FADE_CROSSFADE)
//	return unity_LODFade.x; //查看LOD值不要勾选Animate Cross-fading
//#endif

	ClipLOD(input.positionCS.xy, unity_LODFade.x);

	InputConfig config = GetInputConfig(input.uv0);
#if defined(_MASK_MAP)
	config.useMask = true;
#endif
#if defined(_DETAIL_MAP)
	config.detailUV = input.detailUV;
	config.useDetail = true;
#endif

	float4 base = GetBase(config);
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(config));
#endif

	Surface surface;
	surface.position = input.positionWS;
#if defined(_NORMAL_MAP)
	surface.normal = NormalTangentToWorld(GetNormalTS(config), input.normalWS, input.tangentWS);
	surface.interpolatedNormal = input.normalWS;
#else
	surface.normal = normalize(input.normalWS);
	surface.interpolatedNormal = surface.normal;
#endif
	surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = GetMetallic(config);
	surface.occlusion = GetOcclusion(config);
	surface.smoothness = GetSmoothness(config);
	surface.fresnelStrength = GetFresnel(config);
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

	#if defined(_PREMULTIPLY_ALPHA)
		BRDF brdf = GetBRDF(surface, true);
	#else
		BRDF brdf = GetBRDF(surface);
	#endif

	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
	float3 color = GetLighting(surface, brdf, gi);
	color += GetEmission(config);

	return float4(color, surface.alpha);
}

#endif
