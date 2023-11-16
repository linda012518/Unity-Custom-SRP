#ifndef Linda_Unity_Input
#define Linda_Unity_Input

CBUFFER_START(UnityPerDraw) //�̶�д����UnityPerDraw
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;

	float4 unity_RenderingLayer;

	real4 unity_LightData; //y�����ǹ�Դ����
	real4 unity_LightIndices[2];//ÿ������ĵƹ����������8��

	float4 unity_ProbesOcclusion;//��̬�����������̽����Ӱ

	float4 unity_SpecCube0_HDR;

	float4 unity_LightmapST;
	float4 unity_DynamicLightmapST;

	float4 unity_SHAr;
	float4 unity_SHAg;
	float4 unity_SHAb;
	float4 unity_SHBr;
	float4 unity_SHBg;
	float4 unity_SHBb;
	float4 unity_SHC;

	//����LPPV��������3D����
	float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

float4x4 unity_MatrixPreviousM; //��һ֡ģ�;���
float4x4 unity_MatrixPreviousMI;

float3 _WorldSpaceCameraPos;

float4 unity_OrthoParams;
float4 _ProjectionParams;//x=����opengl/dxƽ̨Y����y=�����ƽ����룬z=���Զƽ�����

#endif
