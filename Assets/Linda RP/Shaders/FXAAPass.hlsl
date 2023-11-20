#ifndef Linda_FXAA_Pass
#define Linda_FXAA_Pass

float GetLuma (float2 uv) {
	#if defined(FXAA_ALPHA_CONTAINS_LUMA)
		return GetSource(uv).a;
	#else
		return GetSource(uv).g;
	#endif
}

float4 FXAAPassFragment (Varyings input) : SV_TARGET {
	return GetLuma(input.screenUV);
}

#endif
