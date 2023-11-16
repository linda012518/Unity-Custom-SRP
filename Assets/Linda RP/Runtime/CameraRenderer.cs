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

    static int 
        colorAttachmentId = Shader.PropertyToID("_CameraFrameBuffer"), 
        depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment"),
        depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),
        sourceTextureId = Shader.PropertyToID("_SourceTexture");

    ScriptableRenderContext context;

    Camera camera;

    CullingResults cullingResults;

    Lighting lighting = new Lighting();

    PostFXStack postFXStack = new PostFXStack();

    static CameraSettings defaultCameraSettings = new CameraSettings();

    bool useHDR;

    bool useDepthTexture, useIntermediateBuffer;

    Material material;

    Texture2D missingTexture;

    public CameraRenderer(Shader shader)
    {
        material = CoreUtils.CreateEngineMaterial(shader);
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "Missing"
        };
        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);
    }

    public void Render(ScriptableRenderContext context, Camera camera, CameraBufferSettings bufferSettings, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, ShadowSettings shadowSettings, PostFXSettings postFXSetting, int colorLUTResolution)
    {
        this.context = context;
        this.camera = camera;

        var crpCamera = camera.GetComponent<LindaRenderPipelineCamera>();
        CameraSettings cameraSettings = crpCamera ? crpCamera.Settings : defaultCameraSettings;

        if (camera.cameraType == CameraType.Reflection)
        {
            useDepthTexture = bufferSettings.copyDepthReflections;
        }
        else
        {
            useDepthTexture = bufferSettings.copyDepth && cameraSettings.copyDepth;
        }

        if (cameraSettings.overridePostFX)
        {
            postFXSetting = cameraSettings.postFXSettings;
        }

        PrepareBuffer();
        //Scene窗口渲染UI，会给场景添加几何体，因此必须在剔除之前完成，不然会剔除掉
        PrepareForSceneWindow();

        if (false == Cull(shadowSettings.maxDistance))
            return;

        useHDR = bufferSettings.allowHDR && camera.allowHDR;

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
        //先Setup相机的东西会在渲染常规几何体之前切换到阴影图集，这样会有错，先渲染阴影
        lighting.Setup(context, cullingResults, shadowSettings, useLightsPerObject, cameraSettings.maskLights ? cameraSettings.renderingLayerMask : -1);
        postFXStack.Setup(context, camera, postFXSetting, useHDR, colorLUTResolution, cameraSettings.finalBlendMode);
        buffer.EndSample(SampleName);
        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing, useLightsPerObject, cameraSettings.renderingLayerMask);
        DrawUnsupportedShaders();
        DrawGizmosBeforeFX();
        if (postFXStack.IsActive)
            postFXStack.Render(colorAttachmentId);
        else if (useIntermediateBuffer)
        {
            Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
        }
        DrawGizmosAfterFX();
        Cleanup();
        Submit();
    }

    bool Cull(float maxShadowDistance)
    {
        if(camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
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

        useIntermediateBuffer = useDepthTexture || postFXStack.IsActive;

        if (useIntermediateBuffer)
        {
            if (flags > CameraClearFlags.Color)
                flags = CameraClearFlags.Color;
            buffer.GetTemporaryRT(colorAttachmentId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            buffer.GetTemporaryRT(depthAttachmentId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.SetRenderTarget(
                colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }

        //清屏 CameraClearFlags前4个枚举都要清除深度，只有等于Color时才清除颜色，如果清除纯色使用相机背景色，把背景色改到线性空间
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color, flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        //frame debug和性能分析器 标注开始点，好像和参数无关，最终使用的buffer.name，需要执行ExecuteCommandBuffer
        buffer.BeginSample(SampleName);
        buffer.SetGlobalTexture(depthTextureId, missingTexture);
        ExecuteBuffer();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, int renderingLayerMask)
    {
        PerObjectData lightsPerObjectFlags = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
        //传入相机，使用相机投影方式进行排序，criteria指定排序方式
        SortingSettings sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
        //指定Pass LightMode=unlitShaderTagId
        DrawingSettings drawingSettings = 
            new DrawingSettings(unlitShaderTagId, sortingSettings) { enableDynamicBatching = useDynamicBatching, enableInstancing = useGPUInstancing, 
                perObjectData = lightsPerObjectFlags | PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.LightProbe | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.LightProbeProxyVolume //给每个物体生成光照图UV
            };
        drawingSettings.SetShaderPassName(1, litShaderTagId);

        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: (uint)renderingLayerMask);

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);

        CopyAttachments();

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

    void Cleanup()
    {
        lighting.Cleanup();
        if (useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorAttachmentId);
            buffer.ReleaseTemporaryRT(depthAttachmentId);

            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);
            }
        }
    }

    void CopyAttachments()
    {
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(depthTextureId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.CopyTexture(depthAttachmentId, depthTextureId);
            ExecuteBuffer();
        }
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
        CoreUtils.Destroy(missingTexture);
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to)
    {
        buffer.SetGlobalTexture(sourceTextureId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
    }
}
