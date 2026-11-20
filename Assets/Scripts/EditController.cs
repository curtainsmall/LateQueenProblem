using UnityEngine;
using UnityEngine.InputSystem;

public class EditController : MonoBehaviour
{
    [SerializeField, Tooltip("负责设景与渲染的 diorama 管理器。")]
    DioramaManager _dioramaManager;

    [SerializeField, Tooltip("帧管理器，点击事件从这里取。")]
    FrameManager _frameManager;

    [SerializeField, Tooltip("拖拽旋转的灵敏度，单位度每像素。")]
    float _dragSpeed = 0.25f;

    [SerializeField, Tooltip("滚轮缩放的灵敏度，单位度每格。")]
    float _fovSpeed = 2f;

    [SerializeField, Tooltip("视角上限，单位度。")]
    float _minFov = 12f;

    [SerializeField, Tooltip("视角下限，单位度。")]
    float _maxFov = 90f;

    Frame _editing;

    public bool IsEditing => _editing != null;

    private void Update()
    {
        if (_editing == null)
        {
            Frame clicked = _frameManager.ConsumeClicked();
            if (clicked != null) Begin(clicked);
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            End();
            return;
        }

        HandleView();
    }

    public void Begin(Frame frame)
    {
        if (frame == null || frame.Diorama == null) return;
        if (_editing == frame) return;

        if (_editing != null) End();

        _editing = frame;
        _editing.IsEditing = true;
        _editing.SetHovered(false);

        _dioramaManager.Show(
            frame.Diorama,
            frame.Target,
            frame.SavedPosition,
            frame.SavedRotation,
            frame.SavedFov);
    }

    public void End()
    {
        if (_editing == null) return;

        _editing.IsEditing = false;
        _dioramaManager.Hide();
        _editing = null;
    }

    void HandleView()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 rotation = _editing.SavedRotation;
        float fov = _editing.SavedFov;
        bool changed = false;

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            rotation.x = Mathf.Clamp(rotation.x - delta.y * _dragSpeed, -85f, 85f);
            rotation.y += delta.x * _dragSpeed;
            changed = true;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            fov = Mathf.Clamp(fov - Mathf.Sign(scroll) * _fovSpeed, _minFov, _maxFov);
            changed = true;
        }

        if (!changed) return;

        _editing.SavedRotation = rotation;
        _editing.SavedFov = fov;
        _dioramaManager.SetView(_editing.SavedPosition, rotation, fov);
    }
}