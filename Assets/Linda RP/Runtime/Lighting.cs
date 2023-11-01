using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string bufferName = "Lighting";

    CommandBuffer buffer = new CommandBuffer() { name = bufferName };

    const int maxDirLightCount = 4, maxOtherLightCount = 64;

    static int
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
        dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    static Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],
        dirLightDirections = new Vector4[maxDirLightCount],
        dirLightShadowData = new Vector4[maxDirLightCount];

    static int
        otherLightCountId = Shader.PropertyToID("_OtherLightCount"),
        otherLightColorsId = Shader.PropertyToID("_OtherLightColors"),
        otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions"),
        otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections"),
        otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles"),
        otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

    static Vector4[]
        otherLightColors = new Vector4[maxOtherLightCount],
        otherLightPositions = new Vector4[maxOtherLightCount],
        otherLightDirections = new Vector4[maxOtherLightCount],
        otherLightSpotAngles = new Vector4[maxOtherLightCount],
        otherLightShadowData = new Vector4[maxOtherLightCount];

    CullingResults cullingResults;

    Shadows shadows = new Shadows();

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;

        buffer.BeginSample(bufferName);
        shadows.Setup(context, cullingResults, shadowSettings);
        SetupLights();
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupLights()
    {
        NativeArray<VisibleLight> lights = cullingResults.visibleLights;

        int dirLightCount = 0, otherLightCount = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            VisibleLight visibleLight = lights[i];

            switch (visibleLight.lightType)
            {
                case LightType.Spot:
                    if (otherLightCount < maxOtherLightCount)
                        SetupSpotLight(otherLightCount++, ref visibleLight);
                    break;
                case LightType.Directional:
                    if (dirLightCount < maxDirLightCount)
                        SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    break;
                case LightType.Point:
                    if (otherLightCount < maxOtherLightCount)
                        SetupPointLight(otherLightCount++, ref visibleLight);
                    break;
            }
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        if (dirLightCount > 0)
        {
            buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
            buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
            buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
        }

        buffer.SetGlobalInt(otherLightCountId, otherLightCount);
        if (otherLightCount > 0)
        {
            buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
            buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
            buffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
            buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);
            buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);
        }
    }
    void SetupDirectionalLight(int index, ref VisibleLight light)
    {
        dirLightColors[index] = light.finalColor;
        //矩阵第3列是Z轴，可通过矩阵乘法推导，1列X轴，2列Y轴
        dirLightDirections[index] = -light.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(light.light, index);
    }

    void SetupPointLight(int index, ref VisibleLight light)
    {
        otherLightColors[index] = light.finalColor;
        Vector4 position = light.localToWorldMatrix.GetColumn(3);
        //计算半径平方倒数，shader减少计算量
        position.w = 1.0f / Mathf.Max(light.range * light.range, 0.00001f);
        otherLightPositions[index] = position;
        //确保点光源不受角度衰减计算的影响
        otherLightSpotAngles[index] = new Vector4(0f, 1f);

        otherLightShadowData[index] = shadows.ReserveOtherShadows(light.light, index);
    }

    void SetupSpotLight(int index, ref VisibleLight light)
    {
        otherLightColors[index] = light.finalColor;
        Vector4 position = light.localToWorldMatrix.GetColumn(3);
        //计算半径平方倒数，shader减少计算量
        position.w = 1f / Mathf.Max(light.range * light.range, 0.00001f);
        otherLightPositions[index] = position;
        otherLightDirections[index] = -light.localToWorldMatrix.GetColumn(2);

        Light lightRuntime = light.light;
        float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * lightRuntime.innerSpotAngle);
        float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.spotAngle);
        float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
        otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);

        otherLightShadowData[index] = shadows.ReserveOtherShadows(lightRuntime, index);
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }

}
