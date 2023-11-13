#ifndef Linda_Unlit_Pass
#define Linda_Unlit_Pass

struct Attributes
{
	float3 positionOS : POSITION;
	float2 uv0 : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 uv0 : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);//初始化实例化渲染ID
	UNITY_TRANSFER_INSTANCE_ID(input, output);////实例化渲染ID传到片元
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	output.uv0 = TransformBaseUV(input.uv0);
	return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);

	InputConfig config = GetInputConfig(input.uv0);

	float4 base = GetBase(config);
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(config));
#endif
	return float4(base.rgb, GetFinalAlpha(base.a));
}

#endif
