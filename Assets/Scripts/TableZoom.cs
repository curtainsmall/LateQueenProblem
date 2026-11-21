using UnityEngine;
using UnityEngine.InputSystem;

public class TableZoom : MonoBehaviour
{
    [SerializeField, Tooltip("桌面，用来按桌面尺寸换算视野与平移范围。")]
    Renderer _table;

    [SerializeField, Tooltip("相机可移动范围的参照物，缩放时改写它的尺寸。")]
    Transform _region;

    [SerializeField, Tooltip("编辑帧视角时接管滚轮的控制者，此时不缩放桌面。")]
    EditController _editController;

    [SerializeField, Tooltip("缩到最近时的正交尺寸，0.36 时一帧正好占画面宽高各一半。")]
    float _minSize = 0.36f;

    [SerializeField, Tooltip("缩到最远时的正交尺寸，2 时整张桌子连同桌外的余量都在画面里。")]
    float _maxSize = 2f;

    [SerializeField, Tooltip("每格滚轮改变的正交尺寸。")]
    float _step = 0.12f;

    Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        Apply(_camera.orthographicSize);
    }

    private void Update()
    {
        if (_camera == null) return;
        if (_editController != null && _editController.IsEditing) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        float size = Mathf.Clamp(_camera.orthographicSize - Mathf.Sign(scroll) * _step, _minSize, _maxSize);
        if (Mathf.Approximately(size, _camera.orthographicSize)) return;

        Apply(size);
    }

    void Apply(float size)
    {
        _camera.orthographicSize = size;

        if (_region == null || _table == null) return;

        float viewHeight = size * 2f;
        float viewWidth = viewHeight * _camera.aspect;

        Bounds bounds = _table.bounds;
        float halfX = Mathf.Max(0f, bounds.extents.x - viewWidth * 0.25f);
        float halfZ = Mathf.Max(0f, bounds.extents.z - viewHeight * 0.25f);

        Vector3 scale = _region.localScale;
        scale.x = halfX * 2f;
        scale.z = halfZ * 2f;
        _region.localScale = scale;

        Vector3 center = _region.position;
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, center.x - halfX, center.x + halfX);
        position.z = Mathf.Clamp(position.z, center.z - halfZ, center.z + halfZ);
        transform.position = position;
    }
}
