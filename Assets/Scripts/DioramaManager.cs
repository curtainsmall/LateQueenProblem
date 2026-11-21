using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DioramaManager : MonoBehaviour
{
    const string DioramaFolder = "Dioramas";

    [SerializeField, Tooltip("渲染帧的相机。位置与朝向都由 diorama 决定。")]
    Camera _renderCamera;

    readonly Dictionary<string, Diorama> _resolved = new Dictionary<string, Diorama>();
    readonly List<CachedCapture> _captures = new List<CachedCapture>();
    readonly List<GameObject> _spawned = new List<GameObject>();

    /// <summary>
    /// 按名字取这个 diorama 的图。首次请求时同步渲染并缓存，之后按名字 + 尺寸复用。
    /// 渲染期间临时替换环境光、天空盒与雾，渲完立刻还原。
    /// </summary>
    public RenderTexture Capture(string dioramaName, int width, int height)
    {
        Diorama diorama = Resolve(dioramaName);
        if (diorama == null || _renderCamera == null) return null;

        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);

        for (int i = 0; i < _captures.Count; i++)
        {
            CachedCapture cached = _captures[i];
            if (cached.Name == dioramaName && cached.Width == width && cached.Height == height) return cached.Texture;
        }

        var texture = new RenderTexture(width, height, 16)
        {
            name = "DioramaCapture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.Create();

        Render(diorama, texture);

        _captures.Add(new CachedCapture
        {
            Name = dioramaName,
            Width = width,
            Height = height,
            Texture = texture
        });

        return texture;
    }

    public void Invalidate(string dioramaName)
    {
        if (string.IsNullOrEmpty(dioramaName)) return;

        for (int i = _captures.Count - 1; i >= 0; i--)
        {
            if (_captures[i].Name != dioramaName) continue;

            Release(_captures[i].Texture);
            _captures.RemoveAt(i);
        }
    }

    Diorama Resolve(string dioramaName)
    {
        if (string.IsNullOrEmpty(dioramaName)) return null;

        if (_resolved.TryGetValue(dioramaName, out Diorama cached)) return cached;

        Diorama loaded = Resources.Load<Diorama>(DioramaFolder + "/" + dioramaName);
        if (loaded == null) return null;

        _resolved[dioramaName] = loaded;
        return loaded;
    }

    public void ReleaseAll()
    {
        foreach (CachedCapture cached in _captures) Release(cached.Texture);
        _captures.Clear();
    }

    private void OnDestroy()
    {
        ReleaseAll();
    }

    void Render(Diorama diorama, RenderTexture target)
    {
        SceneGlobals previous = CaptureGlobals();

        ApplyGlobals(diorama);
        BuildLights(diorama);
        BuildObjects(diorama);

        _renderCamera.transform.localPosition = diorama.Camera.Position;
        _renderCamera.transform.localRotation = Quaternion.Euler(diorama.Camera.Rotation);
        _renderCamera.orthographic = false;
        _renderCamera.fieldOfView = Mathf.Clamp(diorama.Camera.Fov, 5f, 120f);

        bool wasEnabled = _renderCamera.enabled;
        _renderCamera.enabled = false;
        _renderCamera.targetTexture = target;
        _renderCamera.Render();
        _renderCamera.targetTexture = null;
        _renderCamera.enabled = wasEnabled;

        ClearSpawned();
        RestoreGlobals(previous);
    }

    SceneGlobals CaptureGlobals()
    {
        return new SceneGlobals
        {
            AmbientMode = RenderSettings.ambientMode,
            AmbientLight = RenderSettings.ambientLight,
            Skybox = RenderSettings.skybox,
            Fog = RenderSettings.fog,
            FogColor = RenderSettings.fogColor,
            FogDensity = RenderSettings.fogDensity,
            FogMode = RenderSettings.fogMode
        };
    }

    void ApplyGlobals(Diorama diorama)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = diorama.Environment.AmbientColor;

        if (diorama.Environment.Skybox != null) RenderSettings.skybox = diorama.Environment.Skybox;

        RenderSettings.fog = diorama.Environment.FogEnabled;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = diorama.Environment.FogColor;
        RenderSettings.fogDensity = diorama.Environment.FogDensity;
    }

    void RestoreGlobals(SceneGlobals globals)
    {
        RenderSettings.ambientMode = globals.AmbientMode;
        RenderSettings.ambientLight = globals.AmbientLight;
        RenderSettings.skybox = globals.Skybox;

        RenderSettings.fog = globals.Fog;
        RenderSettings.fogMode = globals.FogMode;
        RenderSettings.fogColor = globals.FogColor;
        RenderSettings.fogDensity = globals.FogDensity;
    }

    void BuildLights(Diorama diorama)
    {
        if (diorama.DirectionalLights == null) return;

        int layer = gameObject.layer;

        foreach (DioramaDirectionalLight entry in diorama.DirectionalLights)
        {
            var go = new GameObject("Light");
            go.layer = layer;
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(entry.Direction);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = entry.Color;
            light.intensity = entry.Intensity;
            light.shadows = entry.ShouldShadows ? LightShadows.Hard : LightShadows.None;
            light.cullingMask = 1 << layer;

            _spawned.Add(go);
        }
    }

    void BuildObjects(Diorama diorama)
    {
        if (diorama.PrefabObjects == null) return;

        foreach (DioramaPrefabObject entry in diorama.PrefabObjects)
        {
            if (entry.Prefab == null) continue;

            GameObject go = Instantiate(entry.Prefab, transform);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = entry.Position;
            go.transform.localRotation = Quaternion.Euler(entry.Rotation);
            go.transform.localScale = entry.Scale;
            SetLayer(go);

            _spawned.Add(go);
        }
    }

    void SetLayer(GameObject go)
    {
        int layer = gameObject.layer;
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }

    void ClearSpawned()
    {
        foreach (GameObject go in _spawned)
        {
            if (go == null) continue;

            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
        _spawned.Clear();
    }

    static void Release(RenderTexture texture)
    {
        if (texture == null) return;

        texture.Release();

        if (Application.isPlaying) Destroy(texture);
        else DestroyImmediate(texture);
    }

    struct CachedCapture
    {
        public string Name;
        public int Width;
        public int Height;
        public RenderTexture Texture;
    }

    struct SceneGlobals
    {
        public AmbientMode AmbientMode;
        public Color AmbientLight;
        public Material Skybox;
        public bool Fog;
        public Color FogColor;
        public float FogDensity;
        public FogMode FogMode;
    }
}
