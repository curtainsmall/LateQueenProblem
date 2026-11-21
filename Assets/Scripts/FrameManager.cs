using System.Collections.Generic;
using UnityEngine;

public class FrameManager : MonoBehaviour
{
    [SerializeField, Tooltip("帧的预制体，按传入的 diorama 名字列表实例化。")]
    GameObject _framePrefab;

    [SerializeField, Tooltip("帧实例的父物体，通常挂在桌面中心。")]
    Transform _framesRoot;

    [SerializeField, Tooltip("按名字取图并渲染的 diorama 管理器。")]
    DioramaManager _dioramaManager;

    [SerializeField, Tooltip("一行的帧数，超出后换行。")]
    int _columns = 8;

    [SerializeField, Tooltip("帧在桌面上的水平尺寸，与预制体里的显示面一致。")]
    Vector2 _frameSize = new Vector2(0.64f, 0.36f);

    [SerializeField, Tooltip("相邻帧之间的留白。")]
    float _padding = 0.06f;

    [SerializeField, Tooltip("帧在父物体下的摆放高度，即桌面顶面的局部高度。")]
    float _frameSurfaceHeight = 0.79f;

    [SerializeField, Tooltip("渲染纹理的宽度。")]
    int _textureWidth = 480;

    [SerializeField, Tooltip("渲染纹理的高度。")]
    int _textureHeight = 256;

    readonly List<Frame> _frames = new List<Frame>();

    public IReadOnlyList<Frame> Frames => _frames;

    /// <summary>按名字列表摆放帧。这里不渲染，帧显示空白相纸，等 Capture 请求出图。</summary>
    public void Build(IReadOnlyList<string> dioramaNames)
    {
        Clear();

        if (dioramaNames == null || _framePrefab == null || _framesRoot == null) return;

        int columns = Mathf.Max(1, _columns);
        int rows = Mathf.CeilToInt(dioramaNames.Count / (float)columns);
        float stepX = _frameSize.x + _padding;
        float stepZ = _frameSize.y + _padding;

        for (int i = 0; i < dioramaNames.Count; i++)
        {
            string dioramaName = dioramaNames[i];
            if (string.IsNullOrEmpty(dioramaName)) continue;

            int column = i % columns;
            int row = i / columns;
            float x = (column - (columns - 1) * 0.5f) * stepX;
            float z = ((rows - 1) * 0.5f - row) * stepZ;

            GameObject instance = Instantiate(_framePrefab, _framesRoot);
            instance.transform.localPosition = new Vector3(x, _frameSurfaceHeight, z);
            instance.transform.localRotation = Quaternion.identity;
            SetLayer(instance, _framesRoot.gameObject.layer);

            Frame frame = instance.GetComponent<Frame>();
            if (frame == null) continue;

            frame.Bind(dioramaName);
            frame.Show(null);

            _frames.Add(frame);
        }
    }

    /// <summary>请求某帧的图并交给它显示。diorama 静态，重复请求只会复用缓存。</summary>
    public RenderTexture Capture(Frame frame)
    {
        if (frame == null || _dioramaManager == null) return null;

        RenderTexture texture = _dioramaManager.Capture(frame.DioramaName, _textureWidth, _textureHeight);
        frame.Show(texture);
        return texture;
    }

    public void Clear()
    {
        foreach (Frame frame in _frames)
        {
            if (frame == null) continue;

            if (Application.isPlaying) Destroy(frame.gameObject);
            else DestroyImmediate(frame.gameObject);
        }

        _frames.Clear();

        if (_dioramaManager != null) _dioramaManager.ReleaseAll();
    }

    static void SetLayer(GameObject go, int layer)
    {
        foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = layer;
        }
    }
}
