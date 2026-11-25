using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>UI 按钮：锚点、位置、尺寸、颜色、文案都在组件上设置，外部只订阅点击。</summary>
[RequireComponent(typeof(Button), typeof(Image))]
public sealed class UIButtonElement : MonoBehaviour
{
    [SerializeField, Tooltip("锚点（同时作为 pivot）。")]
    Vector2 _anchor;

    [SerializeField, Tooltip("相对锚点的位置。")]
    Vector2 _position;

    [SerializeField, Tooltip("尺寸。")]
    Vector2 _size;

    [SerializeField, Tooltip("底色。")]
    Color _color;

    [SerializeField, Tooltip("文字颜色。")]
    Color _textColor;

    [SerializeField, Tooltip("按钮文字。")]
    string _label;

    [SerializeField, Tooltip("文字字号占按钮高度的比例。")]
    float _fontRatio;
    [SerializeField, Tooltip("在同级 UI 中的绘制顺序。")]
    int _hierarchyIndex;

    public event Action Clicked;

    void Awake()
    {
        transform.SetSiblingIndex(_hierarchyIndex);
        var rect = (RectTransform)transform;
        rect.anchorMin = _anchor;
        rect.anchorMax = _anchor;
        rect.pivot = _anchor;
        rect.sizeDelta = _size;
        rect.anchoredPosition = _position;

        GetComponent<Image>().color = _color;

        var text = GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = _label;
            text.color = _textColor;
            text.fontSize = Mathf.RoundToInt(Mathf.Min(_size.x, _size.y) * _fontRatio);
        }

        GetComponent<Button>().onClick.AddListener(() => Clicked?.Invoke());
    }
}
