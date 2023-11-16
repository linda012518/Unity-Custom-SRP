using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    partial void DrawGizmosBeforeFX();
    partial void DrawGizmosAfterFX();

    partial void DrawUnsupportedShaders();

    partial void PrepareForSceneWindow();

    partial void PrepareBuffer();

#if UNITY_EDITOR

    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    static Material errorMaterial;

    string SampleName { get; set; }

    partial void PrepareBuffer()
    {
        //只在Editor里有这个标注
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }

    partial void PrepareForSceneWindow()
    {
        if(camera.cameraType == CameraType.SceneView)
        {
            //场景窗口的UI是由另一个RP绘制的，正常不会显示在场景窗口，这个方法专门渲染UI
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void DrawGizmosBeforeFX()
    {
        if(Handles.ShouldRenderGizmos())
        {
            if (useIntermediateBuffer)
            {
                Draw(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                ExecuteBuffer();
            }
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
        }
    }

    partial void DrawGizmosAfterFX()
    {
        if (Handles.ShouldRenderGizmos())
        {
            if (postFXStack.IsActive)
            {
                Draw(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                ExecuteBuffer();
            }
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        SortingSettings sortingSettings = new SortingSettings(camera);
        //overrideMaterial设置所有对象用的材质
        DrawingSettings drawingSettings = new DrawingSettings(legacyShaderTagIds[0], sortingSettings) { overrideMaterial = errorMaterial };
        for (int i = 1; i < legacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

#else

    const string SampleName = bufferName;

#endif
}
