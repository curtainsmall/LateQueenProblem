using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPanner : MonoBehaviour
{
    public float minSize = 2f;
    public float maxSize = 120f;
    public float keyPanSpeed = 1.5f;
    public float zoomStep = 0.12f;

    void Update()
    {
        var cam = GetComponent<Camera>();
        if (cam == null) return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            bool dragging = mouse.middleButton.isPressed || mouse.rightButton.isPressed;
            if (dragging)
            {
                Vector2 delta = mouse.delta.ReadValue();
                float worldPerPixel = (cam.orthographicSize * 2f) / Mathf.Max(1, Screen.height);
                transform.position += new Vector3(-delta.x, 0f, -delta.y) * worldPerPixel;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float target = cam.orthographicSize * (1f - Mathf.Sign(scroll) * zoomStep);
                cam.orthographicSize = Mathf.Clamp(target, minSize, maxSize);
            }
        }

        var kb = Keyboard.current;
        if (kb != null)
        {
            Vector2 dir = Vector2.zero;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dir.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dir.y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dir.y += 1f;

            if (dir.sqrMagnitude > 0f)
            {
                float speed = cam.orthographicSize * keyPanSpeed;
                transform.position += new Vector3(dir.x, 0f, dir.y) * speed * Time.deltaTime;
            }
        }
    }
}