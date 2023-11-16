#ifndef Linda_Unity_Input
#define Linda_Unity_Input

CBUFFER_START(UnityPerDraw) //固定写法用UnityPerDraw
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;
	real4 unity_WorldTransformParams;

	float4 unity_RenderingLayer;

	real4 unity_LightData; //y分量是光源数量
	real4 unity_LightIndices[2];//每个对象的灯光的索引，共8个

	float4 unity_ProbesOcclusion;//动态物体采样光照探针阴影

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

	//采样LPPV，类似于3D纹理
	float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

float4x4 unity_MatrixPreviousM; //上一帧模型矩阵
float4x4 unity_MatrixPreviousMI;

float3 _WorldSpaceCameraPos;

float4 unity_OrthoParams;
float4 _ProjectionParams;//x=区分opengl/dx平台Y方向，y=相机近平面距离，z=相机远平面距离

#endif
