using UnityEngine;
using UnityEngine.UI;

/// <summary>UI 文字：字号、颜色、对齐与边距在组件上设置，外部只负责写入内容。</summary>
[RequireComponent(typeof(Text))]
public sealed class UITextElement : MonoBehaviour
{
    [SerializeField, Tooltip("字号。")]
    int _fontSize;

    [SerializeField, Tooltip("颜色。")]
    Color _color;

    [SerializeField, Tooltip("对齐方式。")]
    TextAnchor _alignment;

    [SerializeField, Tooltip("四周边距。")]
    Vector2 _margin;
    [SerializeField, Tooltip("在同级 UI 中的绘制顺序。")]
    int _hierarchyIndex;

    Text _text;

    void Awake()
    {
        transform.SetSiblingIndex(_hierarchyIndex);
        _text = GetComponent<Text>();
        _text.fontSize = _fontSize;
        _text.color = _color;
        _text.alignment = _alignment;

        var rect = (RectTransform)transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = _margin;
        rect.offsetMax = -_margin;
    }

    public void SetText(string content)
    {
        if (_text == null) _text = GetComponent<Text>();

        _text.text = content;
    }
}
