using UnityEngine;
using UnityEngine.InputSystem;

public class FrameInteractor : MonoBehaviour
{
    [SerializeField, Tooltip("用来打射线的相机。")]
    Camera _raycastCamera;

    [SerializeField, Tooltip("帧管理器，用来处理点击。")]
    FrameManager _frameManager;

    [SerializeField, Tooltip("射线能打到的最大距离。")]
    float _maxDistance = 100f;

    Frame _hoveredFrame;

    public Frame HoveredFrame => _hoveredFrame;

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Frame hit = Raycast(mouse.position.ReadValue());
        SetHovered(hit);

        if (hit != null && mouse.leftButton.wasPressedThisFrame)
        {
            hit.Click();
        }
    }

    Frame Raycast(Vector2 screenPosition)
    {
        Ray ray = _raycastCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance)) return null;
        return hit.collider.GetComponentInParent<Frame>();
    }

    void SetHovered(Frame frame)
    {
        if (_hoveredFrame == frame) return;

        if (_hoveredFrame != null) _hoveredFrame.SetHovered(false);
        _hoveredFrame = frame;
        if (_hoveredFrame != null) _hoveredFrame.SetHovered(true);
    }
}
