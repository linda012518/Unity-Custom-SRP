using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    //CommandBuffer���֣�������frame debug��鿴
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
        //Scene������ȾUI�����������Ӽ����壬��˱������޳�֮ǰ��ɣ���Ȼ���޳���
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
        //����mvp�����ȵ����������������glClear������ᵥ����һ���ı���glDraw
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        //���� CameraClearFlagsǰ4��ö�ٶ�Ҫ�����ȣ�ֻ�е���Colorʱ�������ɫ����������ɫʹ���������ɫ���ѱ���ɫ�ĵ����Կռ�
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        //frame debug�����ܷ����� ��ע��ʼ�㣬����Ͳ����޹أ�����ʹ�õ�buffer.name����Ҫִ��ExecuteCommandBuffer
        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        //���������ʹ�����ͶӰ��ʽ��������criteriaָ������ʽ
        SortingSettings sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        //ָ��Pass LightMode=unlitShaderTagId
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
        //frame debug�����ܷ����� ��ע�����㣬����Ͳ����޹أ�����ʹ�õ�buffer.name����Ҫִ��ExecuteCommandBuffer
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
