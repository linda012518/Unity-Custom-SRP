#ifndef Linda_Unity_Input
#define Linda_Unity_Input

CBUFFER_START(UnityPerDraw) //固定写法用UnityPerDraw
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

float4x4 unity_MatrixPreviousM; //上一帧模型矩阵
float4x4 unity_MatrixPreviousMI;

float3 _WorldSpaceCameraPos;

#endif
