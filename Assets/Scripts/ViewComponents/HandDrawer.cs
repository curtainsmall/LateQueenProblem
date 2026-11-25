using UnityEngine;

/// <summary>可折叠的手牌抽屉：未上桌的卡停在这里，新发的卡也先放到这里。</summary>
public sealed class HandDrawer : MonoBehaviour
{
    [SerializeField, Tooltip("抽屉面板。")]
    Renderer _panel;

    [SerializeField, Tooltip("抽屉把手（点击折叠/展开）。")]
    Renderer _handle;

    [SerializeField, Tooltip("把手上的文字。")]
    TextMesh _label;

    [SerializeField, Tooltip("展开时的抽屉高度（世界单位）。")]
    float _openHeight;

    [SerializeField, Tooltip("折叠时露出的高度。")]
    float _closedHeight;

    [SerializeField, Tooltip("屏幕底边的世界坐标。")]
    float _bottom;

    [SerializeField, Tooltip("卡在手牌区距底边的高度。")]
    float _slotY;

    [SerializeField, Tooltip("手牌之间的水平间距。")]
    float _slotSpacing;


    [SerializeField, Tooltip("隐藏时卡片下移的距离。")]
    float _hiddenOffset;

    bool _open = true;

    public bool IsOpen => _open;
    public float HiddenPosition => _bottom - _hiddenOffset;

    public void Initialize()
    {
        _open = true;
        Apply();
    }

    public void Toggle()
    {
        _open = !_open;
        Apply();
    }

    void Apply()
    {
        float height = _open ? _openHeight : _closedHeight;

        if (_panel != null)
        {
            _panel.transform.localScale = new Vector3(_panel.transform.localScale.x, height, 1f);
            _panel.transform.position = new Vector3(0f, _bottom + height * 0.5f, _panel.transform.position.z);
        }

        if (_handle != null)
        {
            _handle.transform.position = new Vector3(0f, _bottom + height, _handle.transform.position.z);
        }

        if (_label != null)
        {
            _label.text = _open ? "收起手牌" : "展开手牌";
            _label.transform.position = new Vector3(0f, _bottom + height, _label.transform.position.z - 0.01f);
        }
    }

    public Vector3 Slot(int index, int count)
    {
        float total = Mathf.Max(0, count - 1) * _slotSpacing;
        float x = -total * 0.5f + index * _slotSpacing;

        return new Vector3(x, _bottom + _slotY, 0f);
    }

    public bool Contains(Vector3 world)
    {
        if (_handle != null && Inside(_handle.bounds, world)) return true;
        if (_open && _panel != null && Inside(_panel.bounds, world)) return true;

        return false;
    }

    static bool Inside(Bounds bounds, Vector3 point)
    {
        return point.x >= bounds.min.x && point.x <= bounds.max.x
            && point.y >= bounds.min.y && point.y <= bounds.max.y;
    }
}
