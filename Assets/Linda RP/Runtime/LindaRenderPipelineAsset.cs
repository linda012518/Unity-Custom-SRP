using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Linda Render Pipeline")]
public class LindaRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true, useLightsPerObject = true;

    [SerializeField]
    ShadowSettings shadows = default;

    [SerializeField]
    PostFXSettings postFXSetting = default;

    protected override RenderPipeline CreatePipeline()
    {
        return new LindaRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, useLightsPerObject, shadows, postFXSetting);
    }
}
