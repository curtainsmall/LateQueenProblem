using UnityEngine;
using UnityEngine.Rendering;

public class DioramaManager : MonoBehaviour
{
    [SerializeField, Tooltip("内容挂接点。diorama 的预制体实例化到这里。")]
    Transform _anchor;

    [SerializeField, Tooltip("渲染帧的相机。正交，渲到目标纹理。")]
    Camera _camera;

    [SerializeField, Tooltip("主光，方向由 diorama 决定。")]
    Light _keyLight;

    [SerializeField, Tooltip("补光，不投影，只提亮暗部。")]
    Light _fillLight;

    [SerializeField, Tooltip("相机到锚点的固定距离。")]
    float _distance = 5f;

    GameObject _content;
    RenderTexture _target;
    Color _savedAmbient;

    public bool IsShowing => _target != null;

    public void Show(Diorama diorama, RenderTexture target, float yaw, float pitch, float zoom)
    {
        if (diorama == null || target == null) return;

        Hide();

        _savedAmbient = RenderSettings.ambientLight;
        _target = target;

        _keyLight.transform.rotation = Quaternion.Euler(diorama.LightDirection);
        _keyLight.color = diorama.LightColor;
        _keyLight.intensity = diorama.LightIntensity;

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = diorama.Ambient;

        if (_fillLight != null)
        {
            _fillLight.transform.rotation = Quaternion.Euler(diorama.LightDirection + new Vector3(-25f, 140f, 0f));
            _fillLight.color = diorama.LightColor;
            _fillLight.intensity = diorama.LightIntensity * 0.25f;
        }

        if (diorama.Content != null)
        {
            _content = Instantiate(diorama.Content, _anchor);
            _content.transform.localPosition = Vector3.zero;
            _content.transform.localRotation = Quaternion.identity;
        }

        SetView(yaw, pitch, zoom);

        _camera.targetTexture = _target;
        _camera.enabled = true;
    }

    public void Hide()
    {
        if (_target == null) return;

        _camera.enabled = false;
        _camera.targetTexture = null;
        _target = null;

        if (_content != null)
        {
            Destroy(_content);
            _content = null;
        }

        RenderSettings.ambientLight = _savedAmbient;
    }

    public void SetView(float yaw, float pitch, float zoom)
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        _camera.transform.position = _anchor.position + rotation * new Vector3(0f, 0f, -_distance);
        _camera.transform.rotation = rotation;
        _camera.orthographic = true;
        _camera.orthographicSize = zoom;
    }
}