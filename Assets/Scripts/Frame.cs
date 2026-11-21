using System;
using UnityEngine;

public class Frame : MonoBehaviour
{
    Diorama _diorama;

    [SerializeField, Tooltip("显示画面的渲染器，会把渲染纹理贴上去。")]
    Renderer _displayRenderer;

    [SerializeField, Tooltip("悬停时才出现的按钮。")]
    GameObject _hoverButton;

    RenderTexture _renderTexture;
    Material _displayMaterial;

    public event Action<Frame> Clicked;

    public Diorama Diorama => _diorama;
    public RenderTexture TargetTexture => _renderTexture;
    public bool IsEditing { get; set; }
    public Vector3 SavedPosition { get; set; }
    public Vector3 SavedRotation { get; set; }
    public float SavedFov { get; set; }

    public void Bind(Diorama diorama)
    {
        _diorama = diorama;
        if (diorama == null) return;

        name = diorama.name;
        SavedPosition = diorama.CameraPosition;
        SavedRotation = diorama.CameraRotation;
        SavedFov = diorama.CameraFov;
    }

    public void Initialize(int width, int height)
    {
        _renderTexture = new RenderTexture(width, height, 16)
        {
            name = "FrameTarget",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _renderTexture.Create();

        _displayMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _displayMaterial.SetTexture("_BaseMap", _renderTexture);
        _displayMaterial.SetColor("_BaseColor", Color.white);
        _displayRenderer.sharedMaterial = _displayMaterial;

        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        if (_hoverButton != null) _hoverButton.SetActive(hovered && !IsEditing);
    }

    public void Click()
    {
        Clicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (_renderTexture != null) _renderTexture.Release();
        if (_displayMaterial != null) Destroy(_displayMaterial);
    }
}
