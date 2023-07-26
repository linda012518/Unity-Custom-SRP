using UnityEngine;

[DisallowMultipleComponent]//防止添加多个同种类型的脚本
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutoffId = Shader.PropertyToID("_Cutoff");
    static int metallicId = Shader.PropertyToID("_Metallic");
    static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, Range(0.0f, 1.0f)]
    float cutoff = 0;

    [SerializeField, Range(0.0f, 1.0f)]
    float metallic = 0;

    [SerializeField, Range(0.0f, 1.0f)]
    float smoothness = 0.5f;

    static MaterialPropertyBlock block;

    private void Awake()
    {
        OnValidate();
    }

    //类有值变化就会调用
    private void OnValidate()
    {
        if (block == null)
            block = new MaterialPropertyBlock();
        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, cutoff);
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

}
