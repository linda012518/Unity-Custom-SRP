Shader "Hidden/Linda RP/Post FX Stack" 
{
	
	SubShader 
	{
		Cull Off
		ZTest Always
		ZWrite Off
		
		HLSLINCLUDE

		#include "../ShaderLibrary/Common.hlsl"
		#include "PostFXStackPasses.hlsl"

		ENDHLSL

		Pass 
		{
			Name "Bloom Scatter Final"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment BloomScatterFinalPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Scatter"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment BloomScatterPassFragment

			ENDHLSL
		}
		
		Pass 
		{
			Name "Bloom Prefilter Fireflies"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment BloomPrefilterFirefliesPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Prefilter"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment BloomPrefilterPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Combine"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment BloomCombinePassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Vertical"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment BloomVerticalPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Horizontal"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment BloomHorizontalPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Copy"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment CopyPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Color Grading None"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment ColorGradingNonePassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Tone Mapping ACES"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment ToneMappingACESPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Tone Mapping Neutral"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment ToneMappingNeutralPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Tone Mapping Reinhard"
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment ToneMappingReinhardPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Final"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment ApplyColorGradingPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Apply Color Grading With Luma"

			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment ApplyColorGradingWithLumaPassFragment

			ENDHLSL
		}

		Pass 
		{
			Name "Final Rescale"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment FinalPassFragmentRescale

			ENDHLSL
		}

		Pass 
		{
			Name "FXAA"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment FXAAPassFragment

			#pragma multi_compile _ FXAA_QUALITY_MEDIUM FXAA_QUALITY_LOW

			#include "FXAAPass.hlsl"

			ENDHLSL
		}

		Pass 
		{
			Name "FXAA With Luma"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM

			#pragma target 3.5

			#pragma vertex DefaultPassVertex
			#pragma fragment FXAAPassFragment

			#pragma multi_compile _ FXAA_QUALITY_MEDIUM FXAA_QUALITY_LOW

			#define FXAA_ALPHA_CONTAINS_LUMA

			#include "FXAAPass.hlsl"

			ENDHLSL
		}
	}
}