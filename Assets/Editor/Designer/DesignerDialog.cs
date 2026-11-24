using System;
using UnityEditor;
using UnityEngine;

/// <summary>设计器的模态对话框：用于路径输入与随机生成参数。</summary>
public class DesignerDialog : EditorWindow
{
    enum Mode
    {
        Path,
        Random
    }

    Mode _mode;
    string _title = string.Empty;
    string _hint = string.Empty;
    string _path = string.Empty;

    int _totalCards = 12;
    int _fixedCards = 1;

    Action<string> _onPath;
    Action<int, int> _onRandom;

    public static void OpenPath(string title, string hint, string path, Action<string> onConfirm)
    {
        DesignerDialog dialog = CreateInstance<DesignerDialog>();
        dialog._mode = Mode.Path;
        dialog._title = title;
        dialog._hint = hint;
        dialog._path = path;
        dialog._onPath = onConfirm;
        ShowCentered(dialog, 420f, 150f);
    }

    public static void OpenRandom(string title, int totalCards, int fixedCards, Action<int, int> onConfirm)
    {
        DesignerDialog dialog = CreateInstance<DesignerDialog>();
        dialog._mode = Mode.Random;
        dialog._title = title;
        dialog._totalCards = totalCards;
        dialog._fixedCards = fixedCards;
        dialog._onRandom = onConfirm;
        ShowCentered(dialog, 300f, 120f);
    }

    static void ShowCentered(DesignerDialog dialog, float width, float height)
    {
        Rect main = EditorGUIUtility.GetMainWindowPosition();

        dialog.minSize = new Vector2(width, height);
        dialog.maxSize = new Vector2(width, height);
        dialog.position = new Rect(
            main.center.x - width * 0.5f,
            main.center.y - height * 0.5f,
            width,
            height);

        dialog.ShowModalUtility();
    }

    void OnGUI()
    {
        titleContent = new GUIContent(_title);
        EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);

        if (_mode == Mode.Path)
        {
            if (!string.IsNullOrEmpty(_hint)) EditorGUILayout.HelpBox(_hint, MessageType.Info);
            _path = EditorGUILayout.TextField("路径", _path);
        }
        else
        {
            _totalCards = Mathf.Max(4, EditorGUILayout.IntField("卡数", _totalCards));
            _fixedCards = Mathf.Clamp(EditorGUILayout.IntField("固定卡数", _fixedCards), 0, _totalCards);
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("取消")) Close();

        if (GUILayout.Button("确定"))
        {
            if (_mode == Mode.Path)
            {
                _onPath?.Invoke(_path);
            }
            else
            {
                _onRandom?.Invoke(_totalCards, _fixedCards);
            }

            Close();
        }

        EditorGUILayout.EndHorizontal();
    }
}
