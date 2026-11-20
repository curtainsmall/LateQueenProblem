using UnityEngine;

[System.Serializable]
public class DioramaLight
{
    [SerializeField, Tooltip("光源类型。")] LightType _type = LightType.Directional;
    [SerializeField, Tooltip("方向，欧拉角。点光忽略此项。")] Vector3 _direction = new Vector3(50f, 30f, 0f);
    [SerializeField, Tooltip("相对工作室原点的位置。定向光忽略此项。")] Vector3 _position = Vector3.zero;
    [SerializeField, Tooltip("颜色。")] Color _color = Color.white;
    [SerializeField, Tooltip("强度。")] float _intensity = 1.2f;
    [SerializeField, Tooltip("射程。定向光忽略此项。")] float _range = 8f;
    [SerializeField, Tooltip("是否投射阴影。")] bool _shadows = true;

    public LightType Type => _type;
    public Vector3 Direction => _direction;
    public Vector3 Position => _position;
    public Color Color => _color;
    public float Intensity => _intensity;
    public float Range => _range;
    public bool Shadows => _shadows;
}

[System.Serializable]
public class DioramaObject
{
    [SerializeField, Tooltip("物件预制体。必填，材质与形状都由它决定。")] GameObject _prefab;
    [SerializeField, Tooltip("相对工作室原点的位置。")] Vector3 _position;
    [SerializeField, Tooltip("欧拉角旋转。")] Vector3 _rotation;
    [SerializeField, Tooltip("缩放。")] Vector3 _scale = Vector3.one;

    public GameObject Prefab => _prefab;
    public Vector3 Position => _position;
    public Vector3 Rotation => _rotation;
    public Vector3 Scale => _scale;
}

[CreateAssetMenu(menuName = "LateQueen/Diorama", fileName = "Diorama")]
public class Diorama : ScriptableObject
{
    [SerializeField, Tooltip("这个 diorama 摆放的物件。每项必须是预制体。")] DioramaObject[] _objects;
    [SerializeField, Tooltip("这个 diorama 的光源。")] DioramaLight[] _lights;
    [SerializeField, Tooltip("这个 diorama 的环境光颜色。")] Color _ambient = new Color(0.35f, 0.35f, 0.38f, 1f);

    [SerializeField, Tooltip("相机相对工作室的位置。")] Vector3 _cameraPosition = new Vector3(0f, 2f, -4f);
    [SerializeField, Tooltip("相机的欧拉角朝向。")] Vector3 _cameraRotation = new Vector3(18f, 25f, 0f);
    [SerializeField, Tooltip("相机的垂直视角，单位度。")] float _cameraFov = 50f;

    public DioramaObject[] Objects => _objects;
    public DioramaLight[] Lights => _lights;
    public Color Ambient => _ambient;
    public Vector3 CameraPosition => _cameraPosition;
    public Vector3 CameraRotation => _cameraRotation;
    public float CameraFov => _cameraFov;
}