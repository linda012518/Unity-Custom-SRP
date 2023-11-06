using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostFXStack
{
    enum Pass
    {
        Copy
    }

    const string bufferName = "Post FX";

    CommandBuffer buffer = new CommandBuffer() { name = bufferName };

    Camera camera;

    ScriptableRenderContext context;

    PostFXSettings setting;

    int fxSourceId = Shader.PropertyToID("_PostFXSource");

    public bool IsActive => setting != null;

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings setting)
    {
        this.context = context;
        this.camera = camera;
        //场景里没有相机不用后处理
        this.setting = camera.cameraType <= CameraType.SceneView ? setting : null;
        //使编辑窗口后处理按钮生效，可以控制是否显示后处理
        ApplySceneViewState();
    }

    void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
    {
        buffer.SetGlobalTexture(fxSourceId, from);
        buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(Matrix4x4.identity, setting.Material, (int)pass, MeshTopology.Triangles, 3);
    }

    public void Render(int sourceId)
    {
        Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}
