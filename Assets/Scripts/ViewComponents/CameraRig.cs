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
    [SerializeField] float _awayYaw = 324.9f;
    [SerializeField] float _awayPitch = 22f;
    [SerializeField] float _awayDistance = 16f;
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

    public ViewMode Mode => _mode;

    void Awake()
    {
        _lastYaw = _focusYaw;
        _lastPitch = _focusPitch;
        _lastDistance = _focusDistance;
        _hasMemory = true;

        Show(ViewMode.Focus);
    }

    public void Show(ViewMode mode)
    {
        if (_mode == ViewMode.Focus && mode != ViewMode.Focus) RememberFocus();

        _mode = mode;

        switch (mode)
        {
            case ViewMode.Away:
                _yaw = _awayYaw;
                _pitch = _awayPitch;
                _distance = _awayDistance;
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
        transform.position = _pivot;
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        _camera.transform.localPosition = new Vector3(0f, 0f, -_distance);
        _camera.transform.localRotation = Quaternion.identity;
    }
}
