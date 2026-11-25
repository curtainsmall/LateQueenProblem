using UnityEngine;

/// <summary>一张卡：底板 Quad + 文字，内容与尺寸在实例化后设置。</summary>
public sealed class CardView : MonoBehaviour
{
    [SerializeField, Tooltip("卡面。")]
    MeshRenderer _renderer;

    [SerializeField, Tooltip("文字。")]
    TextMesh _label;

    [SerializeField, Tooltip("文字的渲染器。")]
    MeshRenderer _labelRenderer;

    Vector3 _home;
    Vector3 _baseScale = Vector3.one;

    public string CardId { get; private set; }
    public int Row { get; private set; } = -1;
    public int Col { get; private set; } = -1;
    public bool IsPlaced => Row >= 0;

    public Bounds Bounds => _renderer.bounds;
    public int SortingOrder => _renderer.sortingOrder;

    public void Configure(string cardId, string text, Material material, Font font, Color textColor, Vector2 size, float characterSize)
    {
        CardId = cardId;

        transform.localScale = _baseScale;
        _renderer.transform.localScale = new Vector3(size.x, size.y, 1f);
        _renderer.sharedMaterial = material;

        _label.font = font;
        _label.text = text;
        _label.color = textColor;
        _label.characterSize = characterSize;
        _labelRenderer.sharedMaterial = font.material;
        _labelRenderer.sortingOrder = _renderer.sortingOrder + 1;
    }

    public void SetHome(Vector3 position)
    {
        _home = position;
        transform.position = position;
    }

    public void MoveTo(Vector3 position)
    {
        transform.position = position;
    }

    public void ReturnHome()
    {
        Row = -1;
        Col = -1;
        transform.position = _home;
        SetDragging(false);
        SetOrder(10);
    }

    public void PlaceAt(int row, int col, Vector3 position)
    {
        Row = row;
        Col = col;
        transform.position = position;
        SetDragging(false);
        SetOrder(20);
    }

    public void SetDragging(bool dragging)
    {
        transform.localScale = dragging ? _baseScale * 1.05f : _baseScale;
    }

    public void SetOrder(int order)
    {
        _renderer.sortingOrder = order;
        if (_labelRenderer != null) _labelRenderer.sortingOrder = order + 1;
    }
}
