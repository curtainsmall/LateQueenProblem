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

    [SerializeField, Tooltip("缩放。未填（全零）时按 1 处理。")]
    Vector3 _scale;

    public GameObject Prefab => _prefab;
    public Vector3 Position => _position;
    public Vector3 Rotation => _rotation;
    public Vector3 Scale => _scale == Vector3.zero ? Vector3.one : _scale;
}

[System.Serializable]
public struct DioramaDirectionalLight
{
    static readonly Color DefaultColor = new Color(1f, 1f, 1f, 1f);
    const float DefaultIntensity = 1f;

    [SerializeField, Tooltip("方向，欧拉角。diorama 的光只有方向光。")]
    Vector3 _direction;

    [SerializeField, Tooltip("颜色。未填（alpha 为 0）时按白色处理。")]
    Color _color;

    [SerializeField, Tooltip("强度。未填（0）时按 1 处理。")]
    float _intensity;

    [SerializeField, Tooltip("是否投射阴影。")]
    bool _shouldShadows;

    public Vector3 Direction => _direction;
    public Color Color => _color.a > 0f ? _color : DefaultColor;
    public float Intensity => _intensity > 0f ? _intensity : DefaultIntensity;
    public bool ShouldShadows => _shouldShadows;
}

[System.Serializable]
public struct DioramaEnvironment
{
    static readonly Color DefaultAmbientColor = new Color(0.212f, 0.227f, 0.259f, 1f);
    static readonly Color DefaultFogColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    const float DefaultFogDensity = 0.01f;

    [SerializeField, Tooltip("环境光颜色。未填（alpha 为 0）时按 Unity 默认环境光处理。")]
    Color _ambientColor;

    [SerializeField, Tooltip("天空盒。留空时沿用场景当前的天空盒。")]
    Material _skybox;

    [SerializeField, Tooltip("是否开启雾。")]
    bool _fogEnabled;

    [SerializeField, Tooltip("雾的颜色。未填（alpha 为 0）时按中灰处理。")]
    Color _fogColor;

    [SerializeField, Tooltip("雾的浓度。未填（0）时按 0.01 处理。")]
    float _fogDensity;

    public Color AmbientColor => _ambientColor.a > 0f ? _ambientColor : DefaultAmbientColor;
    public Material Skybox => _skybox;
    public bool FogEnabled => _fogEnabled;
    public Color FogColor => _fogColor.a > 0f ? _fogColor : DefaultFogColor;
    public float FogDensity => _fogDensity > 0f ? _fogDensity : DefaultFogDensity;
}

[System.Serializable]
public struct DioramaCamera
{
    const float DefaultFov = 60f;

    [SerializeField, Tooltip("相机相对工作室的位置。")]
    Vector3 _position;

    [SerializeField, Tooltip("相机的欧拉角朝向。")]
    Vector3 _rotation;

    [SerializeField, Tooltip("相机的垂直视角，单位度。未填（0）时按 60 处理。")]
    float _fov;

    public Vector3 Position => _position;
    public Vector3 Rotation => _rotation;
    public float Fov => _fov > 0f ? _fov : DefaultFov;
}

[CreateAssetMenu(menuName = "LateQueenProblem/Diorama", fileName = "Diorama")]
public class Diorama : ScriptableObject
{
    [SerializeField, Tooltip("这个 diorama 摆放的物件。每项必须是预制体。")] DioramaPrefabObject[] _prefabObjects;
    [SerializeField, Tooltip("这个 diorama 的方向光。")] DioramaDirectionalLight[] _directionalLights;
    [SerializeField, Tooltip("这个 diorama 的环境设置：环境光、天空盒、雾。")] DioramaEnvironment _environment;
    [SerializeField, Tooltip("这个 diorama 的相机设置。")] DioramaCamera _camera;

    public DioramaPrefabObject[] PrefabObjects => _prefabObjects;
    public DioramaDirectionalLight[] DirectionalLights => _directionalLights;
    public DioramaEnvironment Environment => _environment;
    public DioramaCamera Camera => _camera;
}
