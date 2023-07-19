using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Linda Render Pipeline")]
public class LindaRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new LindaRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher);
    }
}
