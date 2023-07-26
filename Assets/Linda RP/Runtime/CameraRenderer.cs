using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    //CommandBuffer名字，方便在frame debug里查看
    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer() { name = bufferName };

    static ShaderTagId 
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"), 
        litShaderTagId = new ShaderTagId("LindaLit");

    ScriptableRenderContext context;

    Camera camera;

    CullingResults cullingResults;

    Lighting lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        //Scene窗口渲染UI，会给场景添加几何体，因此必须在剔除之前完成，不然会剔除掉
        PrepareForSceneWindow();

        if (false == Cull())
            return;

        Setup();
        lighting.Setup(context, cullingResults);
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        Submit();
    }

    bool Cull()
    {
        if(camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    void Setup()
    {
        //设置mvp矩阵，先调置相机再清屏调用glClear，否则会单独画一个四边形glDraw
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        //清屏 CameraClearFlags前4个枚举都要清除深度，只有等于Color时才清除颜色，如果清除纯色使用相机背景色，把背景色改到线性空间
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        //frame debug和性能分析器 标注开始点，好像和参数无关，最终使用的buffer.name，需要执行ExecuteCommandBuffer
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        //传入相机，使用相机投影方式进行排序，criteria指定排序方式
        SortingSettings sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        //指定Pass LightMode=unlitShaderTagId
        DrawingSettings drawingSettings = 
            new DrawingSettings(unlitShaderTagId, sortingSettings) { enableDynamicBatching = useDynamicBatching, enableInstancing = useGPUInstancing };
        drawingSettings.SetShaderPassName(1, litShaderTagId);

        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Submit()
    {
        //frame debug和性能分析器 标注结束点，好像和参数无关，最终使用的buffer.name，需要执行ExecuteCommandBuffer
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

}
