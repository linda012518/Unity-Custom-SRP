using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Linda Render Pipeline")]
public partial class LindaRenderPipelineAsset : RenderPipelineAsset
{
    //[SerializeField]
    //bool allowHDR = true;

    [SerializeField]
    CameraBufferSettings cameraBuffer = new CameraBufferSettings
    {
        allowHDR = true,
        renderScale = 1f,
        fxaa = new CameraBufferSettings.FXAA
        {
            fixedThreshold = 0.0833f,
            relativeThreshold = 0.166f
        }
    };

    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true;

    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField]
    PostFXSettings postFXSetting = default;

    public enum ColorLUTResolution { _16 = 16, _32 = 32, _64 = 64 }

    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    [SerializeField]
    Shader cameraRendererShader = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new LindaRenderPipeline(cameraBuffer, useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSetting, (int)colorLUTResolution, cameraRendererShader);
    }
}
