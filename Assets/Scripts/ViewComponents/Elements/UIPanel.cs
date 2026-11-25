using UnityEngine;
using UnityEngine.UI;

/// <summary>UI 底板：颜色与边距在组件上设置，外部只负责显示/隐藏。</summary>
[RequireComponent(typeof(Image))]
public sealed class UIPanel : MonoBehaviour
{
    [SerializeField, Tooltip("底板颜色。")]
    Color _color;

    [SerializeField, Tooltip("四周边距。")]
    Vector2 _margin;
    [SerializeField, Tooltip("在同级 UI 中的绘制顺序。")]
    int _hierarchyIndex;

    Image _image;

    void Awake()
    {
        transform.SetSiblingIndex(_hierarchyIndex);
        _image = GetComponent<Image>();
        _image.color = _color;

        var rect = (RectTransform)transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = _margin;
        rect.offsetMax = -_margin;
    }
}
