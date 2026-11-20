using UnityEngine;
using UnityEngine.InputSystem;

public class CameraEdgePan : MonoBehaviour
{
    public Transform region;
    public float edgePixels = 40f;
    public float speed = 1.2f;

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || region == null) return;

        Vector2 p = mouse.position.ReadValue();
        float w = Screen.width;
        float h = Screen.height;

        float dx = 0f;
        float dy = 0f;
        if (p.x < edgePixels) dx = -1f;
        else if (p.x > w - edgePixels) dx = 1f;
        if (p.y < edgePixels) dy = -1f;
        else if (p.y > h - edgePixels) dy = 1f;

        if (dx == 0f && dy == 0f) return;

        Vector3 pos = transform.position;
        pos.x += dx * speed * Time.deltaTime;
        pos.z += dy * speed * Time.deltaTime;

        Vector3 c = region.position;
        Vector3 s = region.lossyScale;
        float hx = s.x * 0.5f;
        float hz = s.z * 0.5f;

        pos.x = Mathf.Clamp(pos.x, c.x - hx, c.x + hx);
        pos.z = Mathf.Clamp(pos.z, c.z - hz, c.z + hz);
        transform.position = pos;
    }

    void OnDrawGizmos()
    {
        if (region == null) return;

        Vector3 c = region.position;
        Vector3 s = region.lossyScale;
        Vector3 center = new Vector3(c.x, c.y + 0.01f, c.z);
        Vector3 size = new Vector3(s.x, 0.02f, s.z);

        Gizmos.color = new Color(0.15f, 0.75f, 1.00f, 0.85f);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = new Color(0.15f, 0.75f, 1.00f, 0.12f);
        Gizmos.DrawCube(center, size);
    }
}