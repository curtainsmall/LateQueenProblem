using UnityEngine;

/// <summary>通用按钮：底板 + 文字，尺寸、材质、文案由调用方给出。</summary>
[RequireComponent(typeof(MeshRenderer))]
public sealed class ButtonElement : MonoBehaviour
{
    [SerializeField, Tooltip("按钮文字。")]
    TextMesh _label;

    [SerializeField, Tooltip("渲染器。")]
    MeshRenderer _renderer;

    public void Configure(string label, Material material, Vector2 size)
    {
        transform.localScale = new Vector3(size.x, size.y, 1f);
        _renderer.sharedMaterial = material;

        if (_label == null) return;

        _label.text = label;
        _label.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, size.x), 1f / Mathf.Max(0.0001f, size.y), 1f);
    }

    public bool Contains(Vector3 world)
    {
        Bounds bounds = _renderer.bounds;

        return world.x >= bounds.min.x && world.x <= bounds.max.x
            && world.y >= bounds.min.y && world.y <= bounds.max.y;
    }
}
