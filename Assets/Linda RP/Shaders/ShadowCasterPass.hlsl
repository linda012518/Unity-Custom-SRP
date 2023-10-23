#ifndef Linda_Lit_Pass
#define Linda_Lit_Pass

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

Varyings ShadowCasterVertex(Attributes input)
{
	Varyings output;
	UNITY_SETUP_INSTANCE_ID(input);//初始化实例化渲染ID
	UNITY_TRANSFER_INSTANCE_ID(input, output);////实例化渲染ID传到片元
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	//物体超出近裁剪面，阴影会被裁掉，把顶点压缩到近平面以内，但物体很大的时候也会有问题
	#if UNITY_REVERSED_Z
		output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
	#else
		output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
	#endif

	output.uv0 = TransformBaseUV(input.uv0);
	return output;
}

void ShadowCasterFragment(Varyings input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	float4 base = GetBase(input.uv0);
#if defined(_SHADOWS_CLIP)
	clip(base.a - GetCutoff(input.uv0));
#elif defined(_SHADOWS_DITHER)
	float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	clip(base.a - dither);
#endif
}

#endif
