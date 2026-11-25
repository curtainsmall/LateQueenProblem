using UnityEngine;

/// <summary>通用底板：一个 Quad + 材质，尺寸由调用方决定。</summary>
[RequireComponent(typeof(MeshRenderer))]
public sealed class PanelElement : MonoBehaviour
{
    [SerializeField, Tooltip("渲染器。")]
    MeshRenderer _renderer;

    public void Configure(Material material, Vector2 size)
    {
        transform.localScale = new Vector3(size.x, size.y, 1f);
        _renderer.sharedMaterial = material;
    }
}
