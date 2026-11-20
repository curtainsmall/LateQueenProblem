using System.Collections.Generic;
using UnityEngine;

public class FrameManager : MonoBehaviour
{
    [SerializeField, Tooltip("帧的预制体，按传入的 diorama 列表实例化。")]
    GameObject _framePrefab;

    [SerializeField, Tooltip("帧实例的父物体，通常挂在桌面中心。")]
    Transform _framesRoot;

    [SerializeField, Tooltip("相邻帧中心之间的间距。")]
    float _spacing = 1f;

    [SerializeField, Tooltip("帧在父物体下的高度。")]
    float _height = 0.79f;

    [SerializeField, Tooltip("每张渲染纹理的边长。")]
    int _resolution = 512;

    readonly List<Frame> _created = new List<Frame>();
    Frame _clickedFrame;

    public IReadOnlyList<Frame> Frames => _created;

    public void Build(IReadOnlyList<Diorama> dioramas)
    {
        Clear();

        if (dioramas == null || _framePrefab == null || _framesRoot == null) return;

        for (int i = 0; i < dioramas.Count; i++)
        {
            Diorama diorama = dioramas[i];
            if (diorama == null) continue;

            GameObject instance = Instantiate(_framePrefab, _framesRoot);
            float offset = (i - (dioramas.Count - 1) * 0.5f) * _spacing;
            instance.transform.localPosition = new Vector3(offset, _height, 0f);
            instance.transform.localRotation = Quaternion.identity;

            Frame frame = instance.GetComponent<Frame>();
            if (frame == null) continue;

            frame.Bind(diorama);
            frame.Initialize(_resolution, _resolution);
            frame.Clicked += OnFrameClicked;

            _created.Add(frame);
        }
    }

    public void Clear()
    {
        foreach (Frame frame in _created)
        {
            if (frame == null) continue;
            frame.Clicked -= OnFrameClicked;
            Destroy(frame.gameObject);
        }
        _created.Clear();
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