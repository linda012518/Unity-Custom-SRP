using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class LindaRenderPipeline : RenderPipeline
{
    CameraRenderer renderer;
    CameraBufferSettings cameraBufferSettings;
    bool useDynamicBatching, useGPUInstancing, useLightsPerObject;
    ShadowSettings shadowSettings;
    PostFXSettings postFXSetting;
    int colorLUTResolution;

    public LindaRenderPipeline(CameraBufferSettings cameraBufferSettings, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSetting, int colorLUTResolution, Shader cameraRendererShader)
    {
        //使用SRP Batcher，材质内存布局要相同就可以，把材质属性存到了常量缓冲区，不会真正减少drawcall，减少了绘制前的准备工作
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.shadowSettings = shadowSettings;
        this.useLightsPerObject = useLightsPerObject;
        this.postFXSetting = postFXSetting;
        this.cameraBufferSettings = cameraBufferSettings;
        this.colorLUTResolution = colorLUTResolution;

        renderer = new CameraRenderer(cameraRendererShader);

        InitializeForEditor();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, cameraBufferSettings, useDynamicBatching, useGPUInstancing, useLightsPerObject, shadowSettings, postFXSetting, colorLUTResolution);
        }
    }

}
