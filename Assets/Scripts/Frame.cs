using UnityEngine;

public class Frame : MonoBehaviour
{
    [SerializeField, Tooltip("显示画面的渲染器，会把渲染纹理贴上去。")]
    Renderer _displayRenderer;

    [SerializeField, Tooltip("悬停时才出现的按钮，同时也是射线命中的碰撞体。")]
    GameObject _hoverButton;

    Material _displayMaterial;

    public string DioramaName { get; private set; }
    public RenderTexture Texture { get; private set; }

    public void Bind(string dioramaName)
    {
        DioramaName = dioramaName;

        if (!string.IsNullOrEmpty(dioramaName)) name = dioramaName;
    }

    /// <summary>把图交给帧显示。传 null 时显示空白相纸。</summary>
    public void Show(RenderTexture texture)
    {
        Texture = texture;

        if (_displayMaterial == null)
        {
            _displayMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = "FrameDisplay"
            };
            _displayMaterial.SetColor("_BaseColor", Color.white);
        }

        _displayMaterial.SetTexture("_BaseMap", texture != null ? texture : Texture2D.whiteTexture);
        _displayRenderer.sharedMaterial = _displayMaterial;

        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        if (_hoverButton != null) _hoverButton.SetActive(hovered);
    }

    private void OnDestroy()
    {
        if (_displayMaterial != null) Destroy(_displayMaterial);
    }
}
