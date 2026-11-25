using UnityEngine;

/// <summary>一张卡的世界空间对象：Quad + 材质 + 文字，由 PuzzleView 生成与拖动。</summary>
public sealed class CardView : MonoBehaviour
{
    MeshRenderer _renderer;
    MeshRenderer _labelRenderer;
    TextMesh _label;
    Vector3 _home;
    Vector3 _baseScale;

    public string CardId { get; private set; }
    public int Row { get; private set; } = -1;
    public int Col { get; private set; } = -1;
    public bool IsPlaced => Row >= 0;

    public Bounds Bounds => _renderer.bounds;
    public int SortingOrder => _renderer.sortingOrder;

    public void Initialize(string cardId, string text, Material material, Font font, Color textColor, Vector2 size, float textSize, int sortingOrder)
    {
        CardId = cardId;
        _baseScale = Vector3.one;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Sprite";
        quad.transform.SetParent(transform, false);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        var collider = quad.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        _renderer = quad.GetComponent<MeshRenderer>();
        _renderer.sharedMaterial = material;
        _renderer.sortingOrder = sortingOrder;

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0f, -0.01f);

        _label = labelObject.AddComponent<TextMesh>();
        _label.font = font;
        _label.fontSize = 64;
        _label.characterSize = textSize;
        _label.anchor = TextAnchor.MiddleCenter;
        _label.alignment = TextAlignment.Center;
        _label.color = textColor;
        _label.text = text;

        _labelRenderer = labelObject.GetComponent<MeshRenderer>();
        _labelRenderer.sharedMaterial = font.material;
        _labelRenderer.sortingOrder = sortingOrder + 1;
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
