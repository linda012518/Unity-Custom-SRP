#ifndef Linda_Fragment
#define Linda_Fragment

TEXTURE2D(_CameraColorTexture);
TEXTURE2D(_CameraDepthTexture);

struct Fragment {
	float2 positionSS;
	float2 screenUV;
	float depth;//物体的世界深度，对应具体一个点，同一象素可能对应多个物体
	float bufferDepth;//屏幕象素的深度缓冲深度
};

Fragment GetFragment (float4 positionSS) {
	Fragment f;
	f.positionSS = positionSS.xy;
	f.screenUV = f.positionSS / _ScreenParams.xy;//屏幕象素位置除屏幕宽高得UV
	f.depth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
	f.bufferDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_point_clamp, f.screenUV, 0);
	f.bufferDepth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(f.bufferDepth) : LinearEyeDepth(f.bufferDepth, _ZBufferParams);
	return f;
}

float4 GetBufferColor (Fragment fragment, float2 uvOffset = float2(0.0, 0.0)) {
	float2 uv = fragment.screenUV + uvOffset;
	return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_linear_clamp, uv, 0);
}

#endif
