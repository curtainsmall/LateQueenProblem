using System;
using UnityEngine;

/// <summary>左上角的 Esc 按钮。目前只发出点击事件，行为留给之后接菜单。</summary>
public sealed class EscButton : MonoBehaviour
{
    [SerializeField, Tooltip("按钮底块。")]
    Renderer _button;

    [SerializeField, Tooltip("按钮文字。")]
    TextMesh _label;

    public event Action Clicked;

    public bool Contains(Vector3 world)
    {
        if (_button == null) return false;

        Bounds bounds = _button.bounds;

        return world.x >= bounds.min.x && world.x <= bounds.max.x
            && world.y >= bounds.min.y && world.y <= bounds.max.y;
    }

    public void Raise()
    {
        Clicked?.Invoke();
    }
}
