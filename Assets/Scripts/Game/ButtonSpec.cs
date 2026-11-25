using System;
using UnityEngine;

/// <summary>按钮的运行时布局数据：由 ViewManager 提供给同一个按钮预制体。</summary>
[Serializable]
public sealed class ButtonSpec
{
    [SerializeField, Tooltip("按钮文字。")]
    string _label;

    [SerializeField, Tooltip("锚点（同时作为 pivot）。")]
    Vector2 _anchor;

    [SerializeField, Tooltip("相对锚点的位置。")]
    Vector2 _position;

    [SerializeField, Tooltip("尺寸。")]
    Vector2 _size;

    [SerializeField, Tooltip("绘制顺序。")]
    int _hierarchyIndex;

    public string Label => _label;
    public Vector2 Anchor => _anchor;
    public Vector2 Position => _position;
    public Vector2 Size => _size;
    public int HierarchyIndex => _hierarchyIndex;
}
