using UnityEngine;
using UnityEngine.InputSystem;

public class FrameInteractor : MonoBehaviour
{
    [SerializeField, Tooltip("用来打射线的相机。")]
    Camera _camera;

    [SerializeField, Tooltip("帧管理器，用来处理点击。")]
    FrameManager _frameManager;

    [SerializeField, Tooltip("射线能打到的最大距离。")]
    float _maxDistance = 100f;

    Frame _hovered;

    public Frame Hovered => _hovered;

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
        Ray ray = _camera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, _maxDistance)) return null;
        return hit.collider.GetComponentInParent<Frame>();
    }

    void SetHovered(Frame frame)
    {
        if (_hovered == frame) return;

        if (_hovered != null) _hovered.SetHovered(false);
        _hovered = frame;
        if (_hovered != null) _hovered.SetHovered(true);
    }
}