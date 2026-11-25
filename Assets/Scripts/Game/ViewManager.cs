using System;
using UnityEngine;

/// <summary>界面路由：按 stage 类型把请求转给对应的用途组件。</summary>
public sealed class ViewManager : MonoBehaviour
{
    [SerializeField, Tooltip("关卡用途。")]
    LevelView _levelView;

    [SerializeField, Tooltip("插曲用途。")]
    InterludeView _interludeView;

    public event Action ContinueClicked;
    public event Action EscapeClicked;

    void Awake()
    {
        if (_levelView != null) _levelView.EscapeClicked += () => EscapeClicked?.Invoke();
        if (_interludeView != null)
        {
            _interludeView.ContinueClicked += () => ContinueClicked?.Invoke();
            _interludeView.EscapeClicked += () => EscapeClicked?.Invoke();
        }
    }

    public void ShowLevel(LevelData level)
    {
        if (_interludeView != null) _interludeView.Hide();
        if (_levelView != null) _levelView.Show(level);
    }

    public void ShowInterlude(InterludeData interlude)
    {
        if (_levelView != null) _levelView.Hide();
        if (_interludeView != null) _interludeView.Show(interlude);
    }

    public void HideAll()
    {
        if (_levelView != null) _levelView.Hide();
        if (_interludeView != null) _interludeView.Hide();
    }
}
