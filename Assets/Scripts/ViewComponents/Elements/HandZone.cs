using UnityEngine;

/// <summary>贴在屏幕右侧的手牌区：无文字，靠边缘悬停展开，可钉住常开。</summary>
public sealed class HandZone : MonoBehaviour
{
    [SerializeField, Tooltip("抽屉面板。")]
    Renderer _panel;

    [SerializeField, Tooltip("折叠时露出的符号底块。")]
    Renderer _symbol;

    [SerializeField, Tooltip("符号字形。")]
    TextMesh _symbolText;

    [SerializeField, Tooltip("屏幕右边缘的世界坐标。")]
    float _right;

    [SerializeField, Tooltip("面板宽度。")]
    float _panelWidth;

    [SerializeField, Tooltip("面板高度。")]
    float _panelHeight;

    [SerializeField, Tooltip("符号尺寸。")]
    Vector2 _symbolSize;

    [SerializeField, Tooltip("悬停触发区的宽度（可以大于符号）。")]
    float _triggerWidth;

    [SerializeField, Tooltip("悬停触发区的高度。")]
    float _triggerHeight;

    [SerializeField, Tooltip("第一张卡的位置（x 固定，y 为最高处）。")]
    Vector2 _slotOrigin;

    [SerializeField, Tooltip("卡与卡的垂直间距。")]
    float _slotSpacing;

    [SerializeField, Tooltip("隐藏时卡片右移的距离。")]
    float _hiddenOffset;

    bool _pinned;
    bool _hovered;

    public bool IsOpen => _pinned || _hovered;
    public bool IsPinned => _pinned;

    void Start()
    {
        Apply();
    }

    public void SetHovered(bool hovered)
    {
        if (_hovered == hovered) return;

        _hovered = hovered;
        Apply();
    }

    public void TogglePinned()
    {
        _pinned = !_pinned;
        Apply();
    }

    /// <summary>指针是否落在放大的悬停触发区内。</summary>
    public bool IsInTrigger(Vector3 world)
    {
        float minX = _right - _triggerWidth;
        float maxX = _right + 0.2f;
        float halfHeight = _triggerHeight * 0.5f;

        return world.x >= minX && world.x <= maxX && world.y >= -halfHeight && world.y <= halfHeight;
    }

    /// <summary>指针是否点在面板或符号上（用于钉住/取消钉住）。</summary>
    public bool Contains(Vector3 world)
    {
        if (_panel != null && Inside(_panel.bounds, world)) return true;
        if (_symbol != null && Inside(_symbol.bounds, world)) return true;

        return false;
    }

    public Vector3 Slot(int index, int count)
    {
        if (!IsOpen) return new Vector3(_right + _hiddenOffset, _slotOrigin.y - index * _slotSpacing, 0f);

        return new Vector3(_slotOrigin.x, _slotOrigin.y - index * _slotSpacing, 0f);
    }

    void Apply()
    {
        if (_panel != null)
        {
            float x = IsOpen ? _right - _panelWidth * 0.5f : _right + _panelWidth;

            _panel.transform.position = new Vector3(x, 0f, _panel.transform.position.z);
            _panel.transform.localScale = new Vector3(_panelWidth, _panelHeight, 1f);
        }

        if (_symbol != null)
        {
            float x = _right - _symbolSize.x * 0.5f;

            _symbol.transform.position = new Vector3(x, 0f, _symbol.transform.position.z);
            _symbol.transform.localScale = new Vector3(_symbolSize.x, _symbolSize.y, 1f);
            _symbol.gameObject.SetActive(!IsOpen);
            if (_symbolText != null) _symbolText.gameObject.SetActive(!IsOpen);
        }
    }

    static bool Inside(Bounds bounds, Vector3 point)
    {
        return point.x >= bounds.min.x && point.x <= bounds.max.x
            && point.y >= bounds.min.y && point.y <= bounds.max.y;
    }
}
