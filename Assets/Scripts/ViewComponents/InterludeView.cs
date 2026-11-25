using System;
using UnityEngine;

/// <summary>插曲用途：用底板、文字与按钮组装出文字页。</summary>
public sealed class InterludeView : MonoBehaviour
{
    [Header("元素预制体")]
    [SerializeField, Tooltip("底板。")]
    GameObject _panelPrefab;

    [SerializeField, Tooltip("文字。")]
    GameObject _textPrefab;

    [SerializeField, Tooltip("按钮。")]
    GameObject _buttonPrefab;

    [Header("配置")]
    [SerializeField, Tooltip("继续按钮的布局。")]
    ButtonSpec _continueSpec;

    [SerializeField, Tooltip("Esc 按钮的布局。")]
    ButtonSpec _escapeSpec;

    [SerializeField, Tooltip("UI 元素的父节点。")]
    Transform _uiRoot;

    public event Action ContinueClicked;
    public event Action EscapeClicked;

    GameObject _panel;
    GameObject _text;
    GameObject _continueObject;
    GameObject _escapeObject;

    UIText _textElement;

    public void Show(InterludeData interlude)
    {
        EnsureElements();

        SetActive(_panel, true);
        SetActive(_text, true);
        SetActive(_continueObject, true);
        SetActive(_escapeObject, true);

        if (_textElement != null) _textElement.SetText(interlude.JoinTexts("\n\n"));
    }

    public void Hide()
    {
        SetActive(_panel, false);
        SetActive(_text, false);
        SetActive(_continueObject, false);
        SetActive(_escapeObject, false);
    }

    void EnsureElements()
    {
        if (_panel == null)
        {
            _panel = Instantiate(_panelPrefab, _uiRoot != null ? _uiRoot : transform);
            _panel.name = "Panel";
        }

        if (_text == null)
        {
            _text = Instantiate(_textPrefab, _uiRoot != null ? _uiRoot : transform);
            _text.name = "Text";
            _textElement = _text.GetComponent<UIText>();
        }

        if (_continueObject == null)
        {
            _continueObject = Instantiate(_buttonPrefab, _uiRoot != null ? _uiRoot : transform);
            _continueObject.name = "ContinueButton";

            var button = _continueObject.GetComponent<UIButton>();
            if (button != null && _continueSpec != null)
            {
                button.Setup(_continueSpec.Label, _continueSpec.Anchor, _continueSpec.Position, _continueSpec.Size, _continueSpec.HierarchyIndex);
                button.Clicked += () => ContinueClicked?.Invoke();
            }
        }

        if (_escapeObject == null)
        {
            _escapeObject = Instantiate(_buttonPrefab, _uiRoot != null ? _uiRoot : transform);
            _escapeObject.name = "EscButton";

            var escape = _escapeObject.GetComponent<UIButton>();
            if (escape != null && _escapeSpec != null)
            {
                escape.Setup(_escapeSpec.Label, _escapeSpec.Anchor, _escapeSpec.Position, _escapeSpec.Size, _escapeSpec.HierarchyIndex);
                escape.Clicked += () => EscapeClicked?.Invoke();
            }
        }
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}
