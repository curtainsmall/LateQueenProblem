using System;
using UnityEngine;

public class Frame : MonoBehaviour
{
    Diorama _diorama;

    [SerializeField, Tooltip("显示画面的渲染器，会把渲染纹理贴上去。")]
    Renderer _display;

    [SerializeField, Tooltip("悬停时才出现的按钮。")]
    GameObject _button;

    RenderTexture _target;
    Material _material;

    public event Action<Frame> Clicked;

    public Diorama Diorama => _diorama;
    public RenderTexture Target => _target;
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
        _target = new RenderTexture(width, height, 16)
        {
            name = "FrameTarget",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _target.Create();

        _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _material.SetTexture("_BaseMap", _target);
        _material.SetColor("_BaseColor", Color.white);
        _display.sharedMaterial = _material;

        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        if (_button != null) _button.SetActive(hovered && !IsEditing);
    }

    public void Click()
    {
        Clicked?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (_target != null) _target.Release();
        if (_material != null) Destroy(_material);
    }
}