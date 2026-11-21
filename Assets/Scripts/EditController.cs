using UnityEngine;
using UnityEngine.InputSystem;

public class EditController : MonoBehaviour
{
    [SerializeField, Tooltip("负责设景与渲染的 diorama 管理器。")]
    DioramaManager _dioramaManager;

    [SerializeField, Tooltip("帧管理器，点击事件从这里取。")]
    FrameManager _frameManager;

    [SerializeField, Tooltip("拖拽旋转的灵敏度，单位度每像素。")]
    float _rotateSpeed = 0.25f;

    [SerializeField, Tooltip("滚轮改变视角的步长，单位度每格。")]
    float _fovStep = 2f;

    [SerializeField, Tooltip("视角的最小值，单位度。")]
    float _minFov = 12f;

    [SerializeField, Tooltip("视角的最大值，单位度。")]
    float _maxFov = 90f;

    Frame _editingFrame;

    public bool IsEditing => _editingFrame != null;

    private void Update()
    {
        if (_editingFrame == null)
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
        if (_editingFrame == frame) return;

        if (_editingFrame != null) End();

        _editingFrame = frame;
        _editingFrame.IsEditing = true;
        _editingFrame.SetHovered(false);

        _dioramaManager.Show(
            frame.Diorama,
            frame.TargetTexture,
            frame.SavedPosition,
            frame.SavedRotation,
            frame.SavedFov);
    }

    public void End()
    {
        if (_editingFrame == null) return;

        _editingFrame.IsEditing = false;
        _dioramaManager.Hide();
        _editingFrame = null;
    }

    void HandleView()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 rotation = _editingFrame.SavedRotation;
        float fov = _editingFrame.SavedFov;
        bool changed = false;

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            rotation.x = Mathf.Clamp(rotation.x - delta.y * _rotateSpeed, -85f, 85f);
            rotation.y += delta.x * _rotateSpeed;
            changed = true;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            fov = Mathf.Clamp(fov - Mathf.Sign(scroll) * _fovStep, _minFov, _maxFov);
            changed = true;
        }

        if (!changed) return;

        _editingFrame.SavedRotation = rotation;
        _editingFrame.SavedFov = fov;
        _dioramaManager.SetView(_editingFrame.SavedPosition, rotation, fov);
    }
}
