using System.Collections.Generic;
using UnityEngine;

public class FrameManager : MonoBehaviour
{
    [SerializeField, Tooltip("帧的预制体，按传入的 diorama 列表实例化。")]
    GameObject _framePrefab;

    [SerializeField, Tooltip("帧实例的父物体，通常挂在桌面中心。")]
    Transform _framesRoot;

    [SerializeField, Tooltip("一行的帧数，超出后换行。")]
    int _columns = 8;

    [SerializeField, Tooltip("帧在桌面上的水平尺寸，与预制体里的显示面一致。")]
    Vector2 _frameSize = new Vector2(0.64f, 0.36f);

    [SerializeField, Tooltip("相邻帧之间的留白。")]
    float _padding = 0.06f;

    [SerializeField, Tooltip("帧在父物体下的摆放高度，即桌面顶面的局部高度。")]
    float _frameSurfaceHeight = 0.79f;

    [SerializeField, Tooltip("渲染纹理的宽度。")]
    int _textureWidth = 512;

    [SerializeField, Tooltip("渲染纹理的高度。")]
    int _textureHeight = 288;

    readonly List<Frame> _frames = new List<Frame>();
    Frame _clickedFrame;

    public IReadOnlyList<Frame> Frames => _frames;

    public void Build(IReadOnlyList<Diorama> dioramas)
    {
        Clear();

        if (dioramas == null || _framePrefab == null || _framesRoot == null) return;

        int columns = Mathf.Max(1, _columns);
        int rows = Mathf.CeilToInt(dioramas.Count / (float)columns);
        float stepX = _frameSize.x + _padding;
        float stepZ = _frameSize.y + _padding;

        for (int i = 0; i < dioramas.Count; i++)
        {
            Diorama diorama = dioramas[i];
            if (diorama == null) continue;

            int column = i % columns;
            int row = i / columns;
            float x = (column - (columns - 1) * 0.5f) * stepX;
            float z = ((rows - 1) * 0.5f - row) * stepZ;

            GameObject instance = Instantiate(_framePrefab, _framesRoot);
            instance.transform.localPosition = new Vector3(x, _frameSurfaceHeight, z);
            instance.transform.localRotation = Quaternion.identity;

            Frame frame = instance.GetComponent<Frame>();
            if (frame == null) continue;

            frame.Bind(diorama);
            frame.Initialize(_textureWidth, _textureHeight);
            frame.Clicked += OnFrameClicked;

            _frames.Add(frame);
        }
    }

    public void Clear()
    {
        foreach (Frame frame in _frames)
        {
            if (frame == null) continue;
            frame.Clicked -= OnFrameClicked;
            Destroy(frame.gameObject);
        }
        _frames.Clear();
    }

    public Frame ConsumeClicked()
    {
        Frame frame = _clickedFrame;
        _clickedFrame = null;
        return frame;
    }

    void OnFrameClicked(Frame frame)
    {
        _clickedFrame = frame;
    }
}
