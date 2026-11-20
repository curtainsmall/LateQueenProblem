using UnityEngine;

[CreateAssetMenu(menuName = "LateQueen/Diorama", fileName = "Diorama")]
public class Diorama : ScriptableObject
{
    [SerializeField, Tooltip("这一帧的几何预制体，可为空。")]
    GameObject _content;

    [SerializeField, Tooltip("主光方向。X 为俯角，Y 为方位角，Z 不使用。")]
    Vector3 _lightDirection = new Vector3(50f, 30f, 0f);

    [SerializeField, Tooltip("主光颜色。")]
    Color _lightColor = Color.white;

    [SerializeField, Tooltip("主光强度。")]
    float _lightIntensity = 1.2f;

    [SerializeField, Tooltip("这一帧的环境光颜色。")]
    Color _ambient = new Color(0.35f, 0.35f, 0.38f, 1f);

    [SerializeField, Tooltip("默认相机方位角。")]
    float _defaultYaw;

    [SerializeField, Tooltip("默认相机俯仰角。")]
    float _defaultPitch = 20f;

    [SerializeField, Tooltip("默认正交尺寸，越大看得越远。")]
    float _defaultZoom = 1.2f;

    public GameObject Content => _content;
    public Vector3 LightDirection => _lightDirection;
    public Color LightColor => _lightColor;
    public float LightIntensity => _lightIntensity;
    public Color Ambient => _ambient;
    public float DefaultYaw => _defaultYaw;
    public float DefaultPitch => _defaultPitch;
    public float DefaultZoom => _defaultZoom;
}