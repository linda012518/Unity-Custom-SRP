Shader "Linda RP/Unlit"
{
	Properties
	{
		_BaseMap("Texture", 2D) = "white" { }
		_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1.0
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0.0
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1.0
	}

	SubShader
	{
		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment

			#include "UnlitPass.hlsl"

			ENDHLSL
		}
		
		Pass
		{
			Tags { "LightMode" = "ShadowCaster" }

			HLSLPROGRAM

			#pragma target 3.5

			//#pragma shader_feature _CLIPPING
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma multi_compile_instancing

			#pragma vertex ShadowCasterVertex
			#pragma fragment ShadowCasterFragment

			#include "ShadowCasterPass.hlsl"

			ENDHLSL
		}
	}

}
