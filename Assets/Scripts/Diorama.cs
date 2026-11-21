using UnityEngine;

[System.Serializable]
public struct DioramaPrefabObject
{
    [SerializeField, Tooltip("物件预制体。必填，内容与材质都由它决定。")]
    GameObject _prefab;

    [SerializeField, Tooltip("整个预制体相对工作室原点的位置。")]
    Vector3 _position;

    [SerializeField, Tooltip("欧拉角旋转。")]
    Vector3 _rotation;

    [SerializeField, Tooltip("缩放。全零按 1 处理。")]
    Vector3 _scale;

    public GameObject Prefab => _prefab;
    public Vector3 Position => _position;
    public Vector3 Rotation => _rotation;
    public Vector3 Scale => _scale == Vector3.zero ? Vector3.one : _scale;
}

[System.Serializable]
public struct DioramaDirectionalLight
{
    [SerializeField, Tooltip("方向，欧拉角。diorama 的光只有方向光。")]
    Vector3 _direction;

    [SerializeField, Tooltip("颜色。alpha 为 0 时按白色处理。")]
    Color _color;

    [SerializeField, Tooltip("强度。")]
    float _intensity;

    [SerializeField, Tooltip("是否投射阴影。")]
    bool _shouldShadows;

    public Vector3 Direction => _direction == Vector3.zero ? new Vector3(50f, 30f, 0f) : _direction;
    public Color Color => _color.a <= 0f ? Color.white : _color;
    public float Intensity => _intensity <= 0f ? 1.2f : _intensity;
    public bool ShouldShadows => _shouldShadows;
}

[CreateAssetMenu(menuName = "LateQueenProblem/Diorama", fileName = "Diorama")]
public class Diorama : ScriptableObject
{
    [SerializeField, Tooltip("这个 diorama 摆放的物件。每项必须是预制体。")] DioramaPrefabObject[] _prefabObjects;
    [SerializeField, Tooltip("这个 diorama 的方向光。")] DioramaDirectionalLight[] _directionalLights;
    [SerializeField, Tooltip("这个 diorama 的环境光颜色。")] Color _ambientColor = new Color(0.35f, 0.35f, 0.38f, 1f);
    [SerializeField, Tooltip("这个 diorama 的天空盒。留空时沿用场景当前的天空盒。")] Material _skybox;
    [SerializeField, Tooltip("是否开启雾。")] bool _fogEnabled;
    [SerializeField, Tooltip("雾的颜色。")] Color _fogColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField, Tooltip("雾的浓度。")] float _fogDensity = 0.02f;

    [SerializeField, Tooltip("相机相对工作室的位置。")] Vector3 _cameraPosition = new Vector3(0f, 2f, -4f);
    [SerializeField, Tooltip("相机的欧拉角朝向。")] Vector3 _cameraRotation = new Vector3(18f, 25f, 0f);
    [SerializeField, Tooltip("相机的垂直视角，单位度。")] float _cameraFov = 50f;

    public DioramaPrefabObject[] PrefabObjects => _prefabObjects;
    public DioramaDirectionalLight[] DirectionalLights => _directionalLights;
    public Color AmbientColor => _ambientColor;
    public Material Skybox => _skybox;
    public bool FogEnabled => _fogEnabled;
    public Color FogColor => _fogColor;
    public float FogDensity => _fogDensity;
    public Vector3 CameraPosition => _cameraPosition;
    public Vector3 CameraRotation => _cameraRotation;
    public float CameraFov => _cameraFov;
}
