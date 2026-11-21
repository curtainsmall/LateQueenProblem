using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DioramaManager : MonoBehaviour
{
    [SerializeField, Tooltip("渲染帧的相机。位置与朝向都由 diorama 决定。")]
    Camera _renderCamera;

    readonly List<GameObject> _spawned = new List<GameObject>();
    RenderTexture _targetTexture;
    Color _previousAmbient;

    public void Show(Diorama diorama, RenderTexture target, Vector3 position, Vector3 rotation, float fov)
    {
        if (diorama == null || target == null || _renderCamera == null) return;

        Hide();

        _previousAmbient = RenderSettings.ambientLight;
        _targetTexture = target;

        BuildLights(diorama);
        BuildObjects(diorama);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = diorama.AmbientColor;

        SetView(position, rotation, fov);

        _renderCamera.targetTexture = _targetTexture;
        _renderCamera.enabled = true;
    }

    public void Hide()
    {
        if (_targetTexture == null) return;

        _renderCamera.enabled = false;
        _renderCamera.targetTexture = null;
        _targetTexture = null;

        ClearSpawned();
        RenderSettings.ambientLight = _previousAmbient;
    }

    public void SetView(Vector3 position, Vector3 rotation, float fov)
    {
        _renderCamera.transform.localPosition = position;
        _renderCamera.transform.localRotation = Quaternion.Euler(rotation);
        _renderCamera.orthographic = false;
        _renderCamera.fieldOfView = Mathf.Clamp(fov, 5f, 120f);
    }

    void BuildLights(Diorama diorama)
    {
        if (diorama.DirectionalLights == null) return;

        foreach (DioramaDirectionalLight entry in diorama.DirectionalLights)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(entry.Direction);

            Light light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = entry.Color;
            light.intensity = entry.Intensity;
            light.shadows = entry.ShouldShadows ? LightShadows.Hard : LightShadows.None;

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
            if (go != null) Destroy(go);
        }
        _spawned.Clear();
    }
}
