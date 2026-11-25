using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>插曲视图（世界空间）：文字面板与继续按钮。</summary>
public sealed class InterludeView : MonoBehaviour
{
    [SerializeField, Tooltip("渲染用的相机。")]
    Camera _camera;

    [SerializeField, Tooltip("文字内容。")]
    TextMesh _body;

    [SerializeField, Tooltip("继续按钮。")]
    Renderer _continue;

    InterludeData _interlude;
    GameManager _manager;
    bool _clicked;

    public void Show(InterludeData interlude, GameManager manager)
    {
        _interlude = interlude;
        _manager = manager;
        _clicked = false;

        gameObject.SetActive(true);

        if (_body != null) _body.text = Wrap(interlude.JoinTexts("\n"));
    }

    // TextMesh 不自动换行，按固定字数手动折行。
    static string Wrap(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var builder = new System.Text.StringBuilder(text.Length + text.Length / 20);
        int count = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];

            if (character == '\n')
            {
                count = 0;
                builder.Append(character);
                continue;
            }

            builder.Append(character);
            count++;

            if (count >= 18)
            {
                builder.Append('\n');
                count = 0;
            }
        }

        return builder.ToString();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (_interlude == null || _manager == null) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector3 world = ScreenToWorld(Mouse.current.position.ReadValue());

        if (!Hits(_continue, world)) return;

        Hide();
        _manager.MarkInterludeAcknowledged();
    }

    Vector3 ScreenToWorld(Vector2 screen)
    {
        Vector3 point = new Vector3(screen.x, screen.y, -_camera.transform.position.z);
        return _camera.ScreenToWorldPoint(point);
    }

    static bool Hits(Renderer target, Vector3 world)
    {
        if (target == null) return false;

        Bounds bounds = target.bounds;
        return world.x >= bounds.min.x && world.x <= bounds.max.x && world.y >= bounds.min.y && world.y <= bounds.max.y;
    }
}
