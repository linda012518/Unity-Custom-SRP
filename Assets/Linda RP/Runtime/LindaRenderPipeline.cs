using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LindaRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();
    bool useDynamicBatching, useGPUInstancing;

    public LindaRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
    {
        //ʹ��SRP Batcher�������ڴ沼��Ҫ��ͬ�Ϳ��ԣ��Ѳ������Դ浽�˳�����������������������drawcall�������˻���ǰ��׼������
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstancing);
        }
    }

}
