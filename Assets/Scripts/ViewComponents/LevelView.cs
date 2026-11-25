using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>关卡用途：用网格、卡牌、手牌区与 Esc 按钮组装出桌子，并处理拖拽与吸附。</summary>
public sealed class LevelView : MonoBehaviour
{
    [Header("元素预制体")]
    [SerializeField, Tooltip("网格。")]
    GameObject _gridPrefab;

    [SerializeField, Tooltip("卡牌。")]
    GameObject _cardPrefab;

    [SerializeField, Tooltip("手牌区。")]
    GameObject _handZonePrefab;

    [SerializeField, Tooltip("按钮。")]
    GameObject _buttonPrefab;

    [Header("配置")]
    [SerializeField, Tooltip("Esc 按钮的布局。")]
    ButtonSpec _escapeSpec;

    [SerializeField, Tooltip("世界元素实例的父节点。")]
    Transform _viewRoot;

    [SerializeField, Tooltip("UI 元素的父节点。")]
    Transform _uiRoot;

    [SerializeField, Tooltip("渲染用的相机。")]
    Camera _camera;

    public event Action EscapeClicked;

    GameObject _grid;
    GameObject _cards;
    GameObject _handZoneObject;
    GameObject _escapeObject;

    GridMesh _gridMesh;
    HandZone _handZone;

    LevelData _level;

    readonly Dictionary<string, CardView> _cardsById = new Dictionary<string, CardView>();
    readonly Dictionary<int, string> _cellCards = new Dictionary<int, string>();

    CardView _dragging;
    Vector3 _dragOffset;
    int _dragOrder;

    public bool IsShown => gameObject.activeSelf && _level != null;

    public void Show(LevelData level)
    {
        ClearCards();

        _level = level;

        EnsureElements();

        SetActive(_grid, true);
        SetActive(_cards, true);
        SetActive(_handZoneObject, true);
        SetActive(_escapeObject, true);

        if (_gridMesh != null) _gridMesh.Build(level.Rows, level.Cols);

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
        _level = null;

        SetActive(_grid, false);
        SetActive(_cards, false);
        SetActive(_handZoneObject, false);
        SetActive(_escapeObject, false);

        ClearCards();
    }

    void Update()
    {
        if (_level == null) return;
        if (Mouse.current == null) return;

        Vector2 screen = Mouse.current.position.ReadValue();

        UpdateHandZoneHover(ScreenToWorld(screen));

        if (Mouse.current.leftButton.wasPressedThisFrame) OnPressed(screen);
        else if (Mouse.current.leftButton.wasReleasedThisFrame) OnReleased(screen);
        else if (Mouse.current.leftButton.isPressed && _dragging != null) OnDragged(screen);
    }

    void EnsureElements()
    {
        if (_grid == null)
        {
            _grid = Instantiate(_gridPrefab, _viewRoot != null ? _viewRoot : transform);
            _grid.name = "Grid";
            _gridMesh = _grid.GetComponent<GridMesh>();
        }

        if (_cards == null)
        {
            _cards = new GameObject("Cards");
            _cards.transform.SetParent(_viewRoot != null ? _viewRoot : transform, false);
        }

        if (_handZoneObject == null)
        {
            _handZoneObject = Instantiate(_handZonePrefab, _viewRoot != null ? _viewRoot : transform);
            _handZoneObject.name = "HandZone";
            _handZone = _handZoneObject.GetComponent<HandZone>();
        }

        if (_escapeObject == null)
        {
            _escapeObject = Instantiate(_buttonPrefab, _uiRoot != null ? _uiRoot : transform);
            _escapeObject.name = "EscButton";

            var button = _escapeObject.GetComponent<UIButton>();
            if (button != null && _escapeSpec != null)
            {
                button.Setup(_escapeSpec.Label, _escapeSpec.Anchor, _escapeSpec.Position, _escapeSpec.Size, _escapeSpec.HierarchyIndex);
                button.Clicked += () => EscapeClicked?.Invoke();
            }
        }
    }

    // ---------- 交互 ----------

    void OnPressed(Vector2 screen)
    {
        Vector3 world = ScreenToWorld(screen);
        CardView card = FindCard(world);

        if (card == null && _handZone != null && _handZone.Contains(world))
        {
            _handZone.TogglePinned();
            LayoutHand();
            return;
        }

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

    void UpdateHandZoneHover(Vector3 world)
    {
        if (_handZone == null) return;
        if (_dragging != null) return;

        bool before = _handZone.IsOpen;

        if (!_handZone.IsPinned)
        {
            if (_handZone.IsInTrigger(world)) _handZone.SetHovered(true);
            else if (!_handZone.Contains(world)) _handZone.SetHovered(false);
        }

        if (before != _handZone.IsOpen) LayoutHand();
    }

    public void HandleDrop(CardView card, Vector3 world)
    {
        if (card == null || _level == null) return;

        if (_gridMesh == null || !_gridMesh.TryGetCell(world, out int row, out int col))
        {
            ReturnToHand(card);
            return;
        }

        int key = row * _level.Cols + col;

        if (_cellCards.TryGetValue(key, out string occupant) && occupant != card.CardId)
        {
            ReturnToHand(card);
            return;
        }

        if (card.Row >= 0) _cellCards.Remove(card.Row * _level.Cols + card.Col);

        _cellCards[key] = card.CardId;
        card.PlaceAt(row, col, _gridMesh.CellCenter(row, col));
    }

    // ---------- 内部 ----------

    void ReturnToHand(CardView card)
    {
        card.SetHome(HandPosition(HandIndexOf(card), HandCount()));
        card.ReturnHome();
    }

    Vector3 HandPosition(int index, int count)
    {
        if (_handZone == null) return Vector3.zero;

        return _handZone.Slot(index, count);
    }

    void LayoutHand()
    {
        if (_level == null) return;

        int count = HandCount();
        int index = 0;

        foreach (KeyValuePair<string, CardView> pair in _cardsById)
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
        GameObject cardObject = Instantiate(_cardPrefab, _cards != null ? _cards.transform : transform);
        cardObject.name = "Card_" + data.Id;

        var card = cardObject.GetComponent<CardView>();
        card.Setup(data.Id, data.Text);

        _cardsById[data.Id] = card;
        return card;
    }

    void ClearCards()
    {
        foreach (KeyValuePair<string, CardView> pair in _cardsById)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }

        _cardsById.Clear();
        _cellCards.Clear();
    }

    CardView FindCard(Vector3 world)
    {
        CardView found = null;
        int order = int.MinValue;

        foreach (KeyValuePair<string, CardView> pair in _cardsById)
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

    Vector3 ScreenToWorld(Vector2 screen)
    {
        Vector3 point = new Vector3(screen.x, screen.y, -_camera.transform.position.z);
        return _camera.ScreenToWorldPoint(point);
    }

    int HandCount()
    {
        int count = 0;

        foreach (KeyValuePair<string, CardView> pair in _cardsById)
        {
            if (pair.Value != null && !pair.Value.IsPlaced) count++;
        }

        return Mathf.Max(1, count);
    }

    int HandIndexOf(CardView card)
    {
        int index = 0;

        foreach (KeyValuePair<string, CardView> pair in _cardsById)
        {
            if (pair.Value == null || pair.Value.IsPlaced) continue;
            if (pair.Value == card) return index;
            index++;
        }

        return 0;
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}
