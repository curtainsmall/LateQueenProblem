using UnityEngine;
using UnityEngine.InputSystem;

public class CameraEdgePan : MonoBehaviour
{
    [SerializeField, Tooltip("相机可移动范围的参照物。范围尺寸取其 lossyScale 的 X 与 Z，位置取世界坐标。")]
    Transform _region;

    [SerializeField, Tooltip("光标进入距屏幕边缘多少像素时开始平移。")]
    float _edgePixels = 40f;

    [SerializeField, Tooltip("平移速度，单位是世界单位每秒，与缩放无关。")]
    float _speed = 1.2f;

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || _region == null) return;

        Vector2 point = mouse.position.ReadValue();
        float width = Screen.width;
        float height = Screen.height;

        float dx = 0f;
        float dy = 0f;
        if (point.x < _edgePixels) dx = -1f;
        else if (point.x > width - _edgePixels) dx = 1f;
        if (point.y < _edgePixels) dy = -1f;
        else if (point.y > height - _edgePixels) dy = 1f;

        if (dx == 0f && dy == 0f) return;

        Vector3 position = transform.position;
        position.x += dx * _speed * Time.deltaTime;
        position.z += dy * _speed * Time.deltaTime;

        Vector3 center = _region.position;
        Vector3 size = _region.lossyScale;
        float halfX = size.x * 0.5f;
        float halfZ = size.z * 0.5f;

        position.x = Mathf.Clamp(position.x, center.x - halfX, center.x + halfX);
        position.z = Mathf.Clamp(position.z, center.z - halfZ, center.z + halfZ);
        transform.position = position;
    }

    private void OnDrawGizmos()
    {
        if (_region == null) return;

        Vector3 center = _region.position;
        Vector3 size = _region.lossyScale;
        Vector3 drawCenter = new Vector3(center.x, center.y + 0.01f, center.z);
        Vector3 drawSize = new Vector3(size.x, 0.02f, size.z);

        Gizmos.color = new Color(0.15f, 0.75f, 1.00f, 0.85f);
        Gizmos.DrawWireCube(drawCenter, drawSize);

        Gizmos.color = new Color(0.15f, 0.75f, 1.00f, 0.12f);
        Gizmos.DrawCube(drawCenter, drawSize);
    }
}