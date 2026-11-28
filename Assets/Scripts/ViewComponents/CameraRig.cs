using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>相机架：Away 为自由机位，Focus 可绕枢轴旋转缩放，Top 为俯视透视机位。</summary>
[ExecuteAlways]
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

    [SerializeField, Tooltip("Focus / Top 的旋转枢轴（世界坐标）。")]
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

    [SerializeField, Tooltip("模式切换的过渡时长（秒）；0 表示瞬切。")]
    float _transitionDuration = 0.4f;

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
    [SerializeField] float _topFov = 32f;

    ViewMode _mode = ViewMode.Focus;
    float _yaw;
    float _pitch;
    float _distance;

    float _lastYaw;
    float _lastPitch;
    float _lastDistance;
    bool _hasMemory;
    bool _initialized;

    bool _transitioning;
    float _transitionTime;
    float _lastTickTime;
    Vector3 _fromPosition;
    Quaternion _fromRotation;
    float _fromFov;
    Vector3 _toPosition;
    Quaternion _toRotation;
    float _toFov;

    public ViewMode Mode => _mode;

    void Awake()
    {
        EnsureInitialized();

        if (Application.isPlaying)
        {
            Show(ViewMode.Focus);
            return;
        }

        _mode = ViewMode.Focus;
        Apply();
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

    // ---------- 模式切换 ----------

    public void Show(ViewMode mode)
    {
        EnsureInitialized();

        if (_mode == ViewMode.Focus && mode != ViewMode.Focus) RememberFocus();

        _mode = mode;

        switch (mode)
        {
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

                break;

            case ViewMode.Top:
                _yaw = _topYaw;
                _pitch = 90f;
                _distance = _topDistance;
                break;
        }

        if (_transitionDuration <= 0f)
        {
            Apply();
            return;
        }

        BeginTransition();
    }

    void RememberFocus()
    {
        _lastYaw = _yaw;
        _lastPitch = _pitch;
        _lastDistance = _distance;
        _hasMemory = true;
    }

    /// <summary>把当前相机变换存成指定模式的预设。</summary>
    public void CapturePreset(ViewMode mode)
    {
        switch (mode)
        {
            case ViewMode.Away:
                _awayPosition = _camera.transform.position;
                _awayRotation = _camera.transform.eulerAngles;
                break;

            case ViewMode.Focus:
                _focusYaw = Mathf.DeltaAngle(0f, transform.eulerAngles.y);
                _focusPitch = Mathf.DeltaAngle(0f, transform.eulerAngles.x);
                _focusDistance = Mathf.Abs(_camera.transform.localPosition.z);
                _lastYaw = _focusYaw;
                _lastPitch = _focusPitch;
                _lastDistance = _focusDistance;
                _hasMemory = true;
                break;

            case ViewMode.Top:
                _topYaw = Mathf.DeltaAngle(0f, transform.eulerAngles.y);
                _topDistance = Mathf.Abs(_camera.transform.localPosition.z);
                break;
        }
    }

    // ---------- 过渡 ----------

    void BeginTransition()
    {
        _fromPosition = _camera.transform.position;
        _fromRotation = _camera.transform.rotation;
        _fromFov = _camera.fieldOfView;

        GetTargetPose(out _toPosition, out _toRotation);
        _toFov = FovOf(_mode);

        _camera.transform.SetPositionAndRotation(_fromPosition, _fromRotation);
        _camera.fieldOfView = _fromFov;

        _transitionTime = 0f;
        _lastTickTime = Time.realtimeSinceStartup;
        _transitioning = true;

#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorApplication.update += EditorTick;
#endif
    }

#if UNITY_EDITOR
    void EditorTick()
    {
        if (Application.isPlaying)
        {
            UnityEditor.EditorApplication.update -= EditorTick;
            return;
        }

        if (!_transitioning)
        {
            UnityEditor.EditorApplication.update -= EditorTick;
            return;
        }

        AdvanceTransition();
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    void AdvanceTransition()
    {
        float now = Time.realtimeSinceStartup;
        _transitionTime += now - _lastTickTime;
        _lastTickTime = now;

        float t = Mathf.Clamp01(_transitionTime / Mathf.Max(0.0001f, _transitionDuration));
        float eased = t * t * (3f - 2f * t);

        _camera.transform.position = Vector3.Lerp(_fromPosition, _toPosition, eased);
        _camera.transform.rotation = Quaternion.Slerp(_fromRotation, _toRotation, eased);
        _camera.fieldOfView = Mathf.Lerp(_fromFov, _toFov, eased);

        if (t < 1f) return;

        _transitioning = false;
        Apply();
    }

    // ---------- 应用 ----------

    void Apply()
    {
        _camera.orthographic = false;
        _camera.fieldOfView = FovOf(_mode);

        GetTargetPose(out Vector3 position, out Quaternion rotation);

        if (_mode == ViewMode.Away)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            _camera.transform.SetPositionAndRotation(position, rotation);
            return;
        }

        transform.position = _pivot;
        transform.rotation = rotation;
        _camera.transform.localPosition = new Vector3(0f, 0f, -_distance);
        _camera.transform.localRotation = Quaternion.identity;
    }

    float FovOf(ViewMode mode)
    {
        switch (mode)
        {
            case ViewMode.Away: return _awayFov;
            case ViewMode.Top: return _topFov;
            default: return _focusFov;
        }
    }

    void GetTargetPose(out Vector3 position, out Quaternion rotation)
    {
        if (_mode == ViewMode.Away)
        {
            position = _awayPosition;
            rotation = Quaternion.Euler(_awayRotation);
            return;
        }

        rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        position = _pivot + rotation * new Vector3(0f, 0f, -_distance);
    }

    // ---------- 输入 ----------

    void Update()
    {
        if (_transitioning)
        {
            AdvanceTransition();

#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.SceneView.RepaintAll();
#endif
            return;
        }

        if (!Application.isPlaying) return;
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
}
