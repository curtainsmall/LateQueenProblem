using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DioramaManager : MonoBehaviour
{
    [SerializeField, Tooltip("渲染帧的相机。位置与朝向都由 diorama 决定。")]
    Camera _camera;

    readonly List<GameObject> _built = new List<GameObject>();
    RenderTexture _target;
    Color _savedAmbient;

    public void Show(Diorama diorama, RenderTexture target, Vector3 position, Vector3 rotation, float fov)
    {
        if (diorama == null || target == null || _camera == null) return;

        Hide();

        _savedAmbient = RenderSettings.ambientLight;
        _target = target;

        BuildLights(diorama);
        BuildObjects(diorama);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = diorama.Ambient;

        SetView(position, rotation, fov);

        _camera.targetTexture = _target;
        _camera.enabled = true;
    }

    public void Hide()
    {
        if (_target == null) return;

        _camera.enabled = false;
        _camera.targetTexture = null;
        _target = null;

        Clear();
        RenderSettings.ambientLight = _savedAmbient;
    }

    public void SetView(Vector3 position, Vector3 rotation, float fov)
    {
        _camera.transform.localPosition = position;
        _camera.transform.localRotation = Quaternion.Euler(rotation);
        _camera.orthographic = false;
        _camera.fieldOfView = Mathf.Clamp(fov, 5f, 120f);
    }

    void BuildLights(Diorama diorama)
    {
        if (diorama.Lights == null) return;

        foreach (DioramaLight entry in diorama.Lights)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = entry.Position;
            go.transform.localRotation = Quaternion.Euler(entry.Direction);

            Light light = go.AddComponent<Light>();
            light.type = entry.Type;
            light.color = entry.Color;
            light.intensity = entry.Intensity;
            light.range = entry.Range;
            light.shadows = entry.Shadows ? LightShadows.Hard : LightShadows.None;

            _built.Add(go);
        }
    }

    void BuildObjects(Diorama diorama)
    {
        if (diorama.Objects == null) return;

        foreach (DioramaObject entry in diorama.Objects)
        {
            if (entry.Prefab == null) continue;

            GameObject go = Instantiate(entry.Prefab, transform);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = entry.Position;
            go.transform.localRotation = Quaternion.Euler(entry.Rotation);
            go.transform.localScale = entry.Scale;

            _built.Add(go);
        }
    }

    void Clear()
    {
        foreach (GameObject go in _built)
        {
            if (go != null) Destroy(go);
        }
        _built.Clear();
    }
}