#ifndef Linda_Common
#define Linda_Common

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"

#define UNITY_MATRIX_M		unity_ObjectToWorld
#define UNITY_MATRIX_I_M	unity_WorldToObject
#define UNITY_MATRIX_V		unity_MatrixV
#define UNITY_MATRIX_VP		unity_MatrixVP
#define UNITY_MATRIX_P		glstate_matrix_projection

#define UNITY_PREV_MATRIX_M		unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M	unity_MatrixPreviousMI

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
	#define SHADOWS_SHADOWMASK //动态物体采光照探针阴影会破坏实例渲染，加宏开启遮挡数据实例渲染
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

float Square (float v) {
	return v * v;
}

float DistanceSquared(float3 pA, float3 pB) {
	return dot(pA - pB, pA - pB);
}

#endif
