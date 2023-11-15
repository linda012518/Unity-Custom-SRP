#ifndef Linda_Unlit_Pass
#define Linda_Unlit_Pass

struct Attributes
{
	float3 positionOS : POSITION;
	float4 color : COLOR;
	#if defined(_FLIPBOOK_BLENDING)
		float4 uv0 : TEXCOORD0;
		float flipbookBlend : TEXCOORD1;
	#else
		float2 uv0 : TEXCOORD0;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 uv0 : TEXCOORD0;
	#if defined(_VERTEX_COLORS)
		float4 color : VAR_COLOR;
	#endif
	#if defined(_FLIPBOOK_BLENDING)
		float3 flipbookUVB : VAR_FLIPBOOK;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitPassVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);//初始化实例化渲染ID
	UNITY_TRANSFER_INSTANCE_ID(input, output);////实例化渲染ID传到片元
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	output.uv0.xy = TransformBaseUV(input.uv0.xy);
	#if defined(_FLIPBOOK_BLENDING)
		output.flipbookUVB.xy = TransformBaseUV(input.uv0.zw);
		output.flipbookUVB.z = input.flipbookBlend;
	#endif
	#if defined(_VERTEX_COLORS)
		output.color = input.color;
	#endif
	return output;
}

float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);

	InputConfig config = GetInputConfig(input.uv0);
	#if defined(_VERTEX_COLORS)
		config.color = input.color;
	#endif

	#if defined(_FLIPBOOK_BLENDING)
		config.flipbookUVB = input.flipbookUVB;
		config.flipbookBlending = true;
	#endif

	float4 base = GetBase(config);
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(config));
#endif
	return float4(base.rgb, GetFinalAlpha(base.a));
}

#endif
