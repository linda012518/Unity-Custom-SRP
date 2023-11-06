#ifndef Linda_PostFXStack_Passes
#define Linda_PostFXStack_Passes

struct Varyings
{
	float4 positionCS : SV_POSITION;
	float2 screenUV : TEXCOORD0;
};

/*

3	*
2	* *
1	+ + +
0	+ + + * *
-1	+ + + * *

*/
//SV_VertexID 代表传入的是顶点ID序号：0 1 2 3 ......
//一个三角形铺满屏幕，直接返回-1~1顶点数据，最终得到+号区域
Varyings DefaultPassVertex(uint vertexID : SV_VertexID)
{
	Varyings output;

	output.positionCS = float4(
	vertexID <= 1 ? -1.0 : 3.0,
	vertexID == 1 ? 3.0 : -1.0,
	0.0, 1.0
	);

	output.screenUV = float2(
		vertexID <= 1 ? 0.0 : 2.0,
		vertexID == 1 ? 2.0 : 0.0
	);

	return output;
}

float4 CopyPassFragment(Varyings input) : SV_TARGET
{
	return float4(input.screenUV, 0.0, 0.0);
}

#endif
