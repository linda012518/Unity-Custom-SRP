using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostFXStack
{
    const string bufferName = "Post FX";

    CommandBuffer buffer = new CommandBuffer() { name = bufferName };

    Camera camera;

    ScriptableRenderContext context;

    PostFXSettings setting;

    public bool IsActive => setting != null;

    public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings setting)
    {
        this.context = context;
        this.camera = camera;
        this.setting = setting;
    }

    public void Render(int sourceId)
    {
        buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}
