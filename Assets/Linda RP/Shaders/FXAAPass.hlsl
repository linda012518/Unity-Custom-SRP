#ifndef Linda_FXAA_Pass
#define Linda_FXAA_Pass

float4 FXAAPassFragment (Varyings input) : SV_TARGET {
	return GetSource(input.screenUV);
}


#endif
