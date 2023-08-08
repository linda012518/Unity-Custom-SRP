#ifndef Linda_Unity_Input
#define Linda_Unity_Input

CBUFFER_START(UnityPerDraw) //�̶�д����UnityPerDraw
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

float4x4 unity_MatrixPreviousM; //��һ֡ģ�;���
float4x4 unity_MatrixPreviousMI;

float3 _WorldSpaceCameraPos;

#endif