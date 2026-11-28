using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>相机架：绕枢轴旋转。Away / Top 为固定预设，Focus 允许旋转与缩放。</summary>
public sealed class CameraRig : MonoBehaviour
{
    public enum ViewMode
    {
        Away,
        Focus,
        Top,
    }

    [SerializeField, Tooltip("受控相机。")]
    Camera _camera;

    [SerializeField, Tooltip("旋转枢轴（世界坐标）。")]
    Vector3 _pivot;

    [Header("Focus 限制")]
    [SerializeField, Tooltip("最小俯角。")]
    float _minPitch = 15f;

    [SerializeField, Tooltip("最大俯角。")]
    float _maxPitch = 75f;

    [SerializeField, Tooltip("拖动一像素转动的角度。")]
    float _dragSpeed = 0.25f;

    [SerializeField, Tooltip("滚轮一格的缩放比例。")]
    float _zoomStep = 0.12f;

    [SerializeField, Tooltip("最近距离。")]
    float _minDistance = 4f;

    [SerializeField, Tooltip("最远距离。")]
    float _maxDistance = 24f;

    [Header("Away")]
    [SerializeField, Tooltip("相机世界位置（不绕枢轴）。")]
    Vector3 _awayPosition = new Vector3(8.5f, 6.6f, -12.1f);

    [SerializeField, Tooltip("相机世界旋转。")]
    Vector3 _awayRotation = new Vector3(22f, 334.9f, 0f);

    [SerializeField] float _awayFov = 40f;

    [Header("Focus")]
    [SerializeField] float _focusYaw = 324.9f;
    [SerializeField] float _focusPitch = 32.67f;
    [SerializeField] float _focusDistance = 10.75f;
    [SerializeField] float _focusFov = 32f;

    [Header("Top")]
    [SerializeField] float _topYaw = 0f;
    [SerializeField] float _topDistance = 10f;
    [SerializeField] float _topOrthoSize = 2.6f;

    ViewMode _mode = ViewMode.Focus;
    float _yaw;
    float _pitch;
    float _distance;

    float _lastYaw;
    float _lastPitch;
    float _lastDistance;
    bool _hasMemory;
    bool _initialized;

    public ViewMode Mode => _mode;

    void Awake()
    {
        EnsureInitialized();
        Show(ViewMode.Focus);
    }

    void EnsureInitialized()
    {
        if (_initialized) return;

        _yaw = _focusYaw;
        _pitch = _focusPitch;
        _distance = _focusDistance;
        _lastYaw = _yaw;
        _lastPitch = _pitch;
        _lastDistance = _distance;
        _hasMemory = true;
        _initialized = true;
    }

    public void Show(ViewMode mode)
    {
        EnsureInitialized();

        if (_mode == ViewMode.Focus && mode != ViewMode.Focus) RememberFocus();

        _mode = mode;

        switch (mode)
        {
            case ViewMode.Away:
                _camera.orthographic = false;
                _camera.fieldOfView = _awayFov;
                break;

            case ViewMode.Focus:
                if (_hasMemory)
                {
                    _yaw = _lastYaw;
                    _pitch = _lastPitch;
                    _distance = _lastDistance;
                }
                else
                {
                    _yaw = _focusYaw;
                    _pitch = _focusPitch;
                    _distance = _focusDistance;
                }

                _camera.orthographic = false;
                _camera.fieldOfView = _focusFov;
                break;

            case ViewMode.Top:
                _yaw = _topYaw;
                _pitch = 90f;
                _distance = _topDistance;
                _camera.orthographic = true;
                _camera.orthographicSize = _topOrthoSize;
                break;
        }

        Apply();
    }

    /// <summary>把当前的角度与距离存成指定模式的预设。</summary>
    public void CapturePreset(ViewMode mode)
    {
        float yaw = Mathf.DeltaAngle(0f, transform.eulerAngles.y);
        float pitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
        float distance = Mathf.Abs(_camera.transform.localPosition.z);

        switch (mode)
        {
            case ViewMode.Away:
                _awayPosition = _camera.transform.position;
                _awayRotation = _camera.transform.eulerAngles;
                break;

            case ViewMode.Focus:
                _focusYaw = yaw;
                _focusPitch = pitch;
                _focusDistance = distance;
                _lastYaw = yaw;
                _lastPitch = pitch;
                _lastDistance = distance;
                _hasMemory = true;
                break;

            case ViewMode.Top:
                _topYaw = yaw;
                _topDistance = distance;
                break;
        }
    }

    void RememberFocus()
    {
        _lastYaw = _yaw;
        _lastPitch = _pitch;
        _lastDistance = _distance;
        _hasMemory = true;
    }

    void Update()
    {
        if (_mode != ViewMode.Focus) return;
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            _yaw += delta.x * _dragSpeed;
            _pitch = Mathf.Clamp(_pitch - delta.y * _dragSpeed, _minPitch, _maxPitch);
        }

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _distance = Mathf.Clamp(_distance * (1f - Mathf.Sign(scroll) * _zoomStep), _minDistance, _maxDistance);
        }

        Apply();
    }

    void Apply()
    {
        if (_mode == ViewMode.Away)
        {
            // Away 是自由的叙事机位：不绕枢轴，直接使用保存的世界位置与旋转。
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            _camera.transform.position = _awayPosition;
            _camera.transform.rotation = Quaternion.Euler(_awayRotation);
            return;
        }

        transform.position = _pivot;
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        _camera.transform.localPosition = new Vector3(0f, 0f, -_distance);
        _camera.transform.localRotation = Quaternion.identity;
    }
}
