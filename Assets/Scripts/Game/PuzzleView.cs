using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>关卡视图（世界空间）：桌面、网格、卡牌与会折叠的手牌抽屉；判定暂未接入。</summary>
public sealed class PuzzleView : MonoBehaviour
{
    [SerializeField, Tooltip("渲染用的相机。")]
    Camera _camera;

    [SerializeField, Tooltip("网格绘制与坐标换算。")]
    GridMesh _grid;

    [SerializeField, Tooltip("手牌抽屉。")]
    HandDrawer _drawer;

    [SerializeField, Tooltip("左上角 Esc 按钮。")]
    EscButton _escape;

    [SerializeField, Tooltip("格子的世界尺寸。")]
    Vector2 _cellSize;

    [SerializeField, Tooltip("卡的世界尺寸。")]
    Vector2 _cardSize;

    [SerializeField, Tooltip("卡的材质。")]
    Material _cardMaterial;

    [SerializeField, Tooltip("卡的字体。")]
    Font _cardFont;


    [SerializeField, Tooltip("卡文字颜色。")]
    Color _cardTextColor;

    [SerializeField, Tooltip("卡文字大小。")]
    float _cardTextSize;

    LevelData _level;

    readonly Dictionary<string, CardView> _cards = new Dictionary<string, CardView>();
    readonly Dictionary<int, string> _cellCards = new Dictionary<int, string>();

    CardView _dragging;
    Vector3 _dragOffset;
    int _dragOrder;

    void Awake()
    {
        if (_drawer != null) _drawer.Initialize();
    }

    public void Show(LevelData level, GameManager manager)
    {
        _level = level;

        gameObject.SetActive(true);

        ClearCards();

        if (_grid == null) return;

        _grid.Build(level.Rows, level.Cols, _cellSize);

        int count = level.Cards.Count;

        for (int i = 0; i < count; i++)
        {
            CardData data = level.Cards[i];
            CardView card = CreateCard(data);
            card.SetHome(HandPosition(i, count));
        }
    }

    public void Hide()
    {
        _dragging = null;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (_level == null) return;
        if (Mouse.current == null) return;

        Vector2 screen = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame) OnPressed(screen);
        else if (Mouse.current.leftButton.wasReleasedThisFrame) OnReleased(screen);
        else if (Mouse.current.leftButton.isPressed && _dragging != null) OnDragged(screen);
    }

    void OnPressed(Vector2 screen)
    {
        Vector3 world = ScreenToWorld(screen);

        if (_escape != null && _escape.Contains(world))
        {
            _escape.Raise();
            return;
        }

        if (_drawer != null && _drawer.Contains(world))
        {
            _drawer.Toggle();
            LayoutHand();
            return;
        }

        CardView card = FindCard(world);
        if (card == null) return;

        _dragging = card;
        _dragOffset = card.transform.position - world;
        _dragOrder += 2;
        card.SetOrder(_dragOrder);
        card.SetDragging(true);
    }

    void OnDragged(Vector2 screen)
    {
        if (_dragging == null) return;
        _dragging.MoveTo(ScreenToWorld(screen) + _dragOffset);
    }

    void OnReleased(Vector2 screen)
    {
        if (_dragging == null) return;

        CardView card = _dragging;
        _dragging = null;

        HandleDrop(card, ScreenToWorld(screen));
    }

    Vector3 ScreenToWorld(Vector2 screen)
    {
        Vector3 point = new Vector3(screen.x, screen.y, -_camera.transform.position.z);
        return _camera.ScreenToWorldPoint(point);
    }

    CardView FindCard(Vector3 world)
    {
        CardView found = null;
        int order = int.MinValue;

        foreach (KeyValuePair<string, CardView> pair in _cards)
        {
            CardView card = pair.Value;
            if (card == null) continue;
            if (card.Bounds.Contains(new Vector3(world.x, world.y, card.Bounds.center.z)) && card.SortingOrder > order)
            {
                found = card;
                order = card.SortingOrder;
            }
        }

        return found;
    }

    Vector3 HandPosition(int index, int count)
    {
        if (_drawer == null) return Vector3.zero;
        if (!_drawer.IsOpen) return new Vector3(0f, _drawer.HiddenPosition, 0f);

        return _drawer.Slot(index, count);
    }

    void LayoutHand()
    {
        if (_level == null) return;

        int count = HandCount();
        int index = 0;

        foreach (KeyValuePair<string, CardView> pair in _cards)
        {
            CardView card = pair.Value;
            if (card == null || card.IsPlaced) continue;

            card.SetHome(HandPosition(index, count));
            card.ReturnHome();
            index++;
        }
    }

    CardView CreateCard(CardData data)
    {
        var cardObject = new GameObject("Card_" + data.Id);
        cardObject.transform.SetParent(transform, false);

        var card = cardObject.AddComponent<CardView>();
        card.Initialize(data.Id, data.Text, _cardMaterial, _cardFont, _cardTextColor, _cardSize, _cardTextSize, 10);

        _cards[data.Id] = card;
        return card;
    }

    void ClearCards()
    {
        foreach (KeyValuePair<string, CardView> pair in _cards)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }

        _cards.Clear();
        _cellCards.Clear();
    }

    public void HandleDrop(CardView card, Vector3 world)
    {
        if (card == null) return;

        if (_grid == null || !_grid.TryGetCell(world, out int row, out int col))
        {
            card.SetHome(HandPosition(HandIndexOf(card), HandCount()));
            card.ReturnHome();
            return;
        }

        int key = row * _level.Cols + col;

        if (_cellCards.TryGetValue(key, out string occupant) && occupant != card.CardId)
        {
            card.SetHome(HandPosition(HandIndexOf(card), HandCount()));
            card.ReturnHome();
            return;
        }

        if (card.Row >= 0) _cellCards.Remove(card.Row * _level.Cols + card.Col);

        _cellCards[key] = card.CardId;
        card.PlaceAt(row, col, _grid.CellCenter(row, col));
    }

    int HandCount()
    {
        int count = 0;

        foreach (KeyValuePair<string, CardView> pair in _cards)
        {
            if (pair.Value != null && !pair.Value.IsPlaced) count++;
        }

        return Mathf.Max(1, count);
    }

    int HandIndexOf(CardView card)
    {
        int index = 0;

        foreach (KeyValuePair<string, CardView> pair in _cards)
        {
            if (pair.Value == null || pair.Value.IsPlaced) continue;
            if (pair.Value == card) return index;
            index++;
        }

        return 0;
    }
}
