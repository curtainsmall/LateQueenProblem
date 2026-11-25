using UnityEngine;

/// <summary>通用文字：内容与颜色由调用方给出。</summary>
[RequireComponent(typeof(TextMesh))]
public sealed class TextElement : MonoBehaviour
{
    [SerializeField, Tooltip("文字组件。")]
    TextMesh _text;

    public void Configure(string text, Color color)
    {
        _text.text = text;
        _text.color = color;
    }
}
