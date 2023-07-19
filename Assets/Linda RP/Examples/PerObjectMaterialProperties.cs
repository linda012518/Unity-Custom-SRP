using UnityEngine;

[DisallowMultipleComponent]//防止添加多个同种类型的脚本
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutoffId = Shader.PropertyToID("_Cutoff");

    [SerializeField]
    Color baseColor = Color.white;

    [SerializeField, Range(0.0f, 1.0f)]
    float cutoff = 0;

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
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

}
