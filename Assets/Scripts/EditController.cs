using UnityEngine;
using UnityEngine.InputSystem;

public class EditController : MonoBehaviour
{
    [SerializeField, Tooltip("负责设景与渲染的 diorama 管理器。")]
    DioramaManager _dioramaManager;

    [SerializeField, Tooltip("帧管理器，点击事件从这里取。")]
    FrameManager _frameManager;

    [SerializeField, Tooltip("拖拽旋转的灵敏度。")]
    float _dragSpeed = 0.25f;

    [SerializeField, Tooltip("滚轮缩放的灵敏度。")]
    float _zoomSpeed = 0.1f;

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
            frame.SavedYaw,
            frame.SavedPitch,
            frame.SavedZoom);
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

        float yaw = _editing.SavedYaw;
        float pitch = _editing.SavedPitch;
        float zoom = _editing.SavedZoom;
        bool changed = false;

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * _dragSpeed;
            pitch = Mathf.Clamp(pitch - delta.y * _dragSpeed, -80f, 85f);
            changed = true;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            zoom = Mathf.Clamp(zoom * (1f - Mathf.Sign(scroll) * _zoomSpeed), 0.35f, 6f);
            changed = true;
        }

        if (!changed) return;

        _editing.SavedYaw = yaw;
        _editing.SavedPitch = pitch;
        _editing.SavedZoom = zoom;
        _dioramaManager.SetView(yaw, pitch, zoom);
    }
}