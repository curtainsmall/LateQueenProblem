using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>唯一的界面管理者：按 GameManager 的请求，用元素预制体搭出当前要显示的界面。</summary>
public sealed class ViewManager : MonoBehaviour
{
    [Header("元素预制体")]
    [SerializeField, Tooltip("桌面背景。")]
    GameObject _backgroundPrefab;

    [SerializeField, Tooltip("网格。")]
    GameObject _gridPrefab;

    [SerializeField, Tooltip("卡牌容器。")]
    GameObject _cardsPrefab;

    [SerializeField, Tooltip("手牌抽屉。")]
    GameObject _drawerPrefab;

    [SerializeField, Tooltip("Esc 按钮。")]
    GameObject _escapePrefab;

    [SerializeField, Tooltip("文字底板。")]
    GameObject _panelPrefab;

    [SerializeField, Tooltip("文字。")]
    GameObject _textPrefab;

    [SerializeField, Tooltip("继续按钮。")]
    GameObject _continuePrefab;

    [Header("场景")]
    [SerializeField, Tooltip("元素实例的父节点。")]
    Transform _viewRoot;

    [SerializeField, Tooltip("渲染用的相机。")]
    Camera _camera;

    [Header("卡牌")]
    [SerializeField, Tooltip("卡的材质。")]
    Material _cardMaterial;

    [SerializeField, Tooltip("卡的字体。")]
    Font _cardFont;

    [SerializeField, Tooltip("卡的世界尺寸。")]
    Vector2 _cardSize;

    [SerializeField, Tooltip("卡文字颜色。")]
    Color _cardTextColor;

    [SerializeField, Tooltip("卡文字大小。")]
    float _cardTextSize;

    [SerializeField, Tooltip("格子的世界尺寸。")]
    Vector2 _cellSize;

    [SerializeField, Tooltip("正文每行字数。")]
    int _lineLength;

    public event Action ContinueClicked;
    public event Action EscapeClicked;

    GameObject _background;
    GameObject _grid;
    GameObject _cards;
    GameObject _drawer;
    GameObject _escape;
    GameObject _panel;
    GameObject _text;
    GameObject _continue;

    GridMesh _gridMesh;
    HandDrawer _drawerComponent;
    EscButton _escapeComponent;
    TextMesh _textMesh;
    Renderer _continueRenderer;

    LevelData _level;
    bool _interludeVisible;

    readonly Dictionary<string, CardView> _cardsById = new Dictionary<string, CardView>();
    readonly Dictionary<int, string> _cellCards = new Dictionary<int, string>();

    CardView _dragging;
    Vector3 _dragOffset;
    int _dragOrder;

    // ---------- 对外的请求 ----------

    public void ShowLevel(LevelData level)
    {
        HideAll();

        _level = level;
        _interludeVisible = false;

        EnsureLevelViews();

        SetActive(_background, true);
        SetActive(_grid, true);
        SetActive(_cards, true);
        SetActive(_drawer, true);
        SetActive(_escape, true);

        ClearCards();

        if (_gridMesh != null) _gridMesh.Build(level.Rows, level.Cols, _cellSize);

        int count = level.Cards.Count;

        for (int i = 0; i < count; i++)
        {
            CardData data = level.Cards[i];
            CardView card = CreateCard(data);
            card.SetHome(HandPosition(i, count));
        }
    }

    public void ShowInterlude(InterludeData interlude)
    {
        HideAll();

        _level = null;
        _interludeVisible = true;

        EnsureTextView();

        SetActive(_panel, true);
        SetActive(_text, true);
        SetActive(_continue, true);

        if (_textMesh != null) _textMesh.text = Wrap(interlude.JoinTexts("\n"));
    }

    public void HideAll()
    {
        _dragging = null;
        _level = null;
        _interludeVisible = false;

        SetActive(_background, false);
        SetActive(_grid, false);
        SetActive(_cards, false);
        SetActive(_drawer, false);
        SetActive(_escape, false);
        SetActive(_panel, false);
        SetActive(_text, false);
        SetActive(_continue, false);

        ClearCards();
    }

    // ---------- 输入 ----------

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 screen = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame) OnPressed(screen);
        else if (Mouse.current.leftButton.wasReleasedThisFrame) OnReleased(screen);
        else if (Mouse.current.leftButton.isPressed && _dragging != null) OnDragged(screen);
    }

    void OnPressed(Vector2 screen)
    {
        Vector3 world = ScreenToWorld(screen);

        if (_level != null)
        {
            if (_escapeComponent != null && _escapeComponent.Contains(world))
            {
                EscapeClicked?.Invoke();
                return;
            }

            if (_drawerComponent != null && _drawerComponent.Contains(world))
            {
                _drawerComponent.Toggle();
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
            return;
        }

        if (_interludeVisible && _continueRenderer != null && Inside(_continueRenderer.bounds, world))
        {
            ContinueClicked?.Invoke();
        }
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

    // ---------- 元素实例 ----------

    void EnsureLevelViews()
    {
        if (_background == null) _background = InstantiateElement(_backgroundPrefab);
        if (_grid == null) { _grid = InstantiateElement(_gridPrefab); _gridMesh = _grid != null ? _grid.GetComponent<GridMesh>() : null; }
        if (_cards == null) _cards = InstantiateElement(_cardsPrefab);
        if (_drawer == null) { _drawer = InstantiateElement(_drawerPrefab); _drawerComponent = _drawer != null ? _drawer.GetComponent<HandDrawer>() : null; if (_drawerComponent != null) _drawerComponent.Initialize(); }
        if (_escape == null) { _escape = InstantiateElement(_escapePrefab); _escapeComponent = _escape != null ? _escape.GetComponent<EscButton>() : null; }
    }

    void EnsureTextView()
    {
        if (_panel == null) _panel = InstantiateElement(_panelPrefab);
        if (_text == null) { _text = InstantiateElement(_textPrefab); _textMesh = _text != null ? _text.GetComponent<TextMesh>() : null; }
        if (_continue == null) { _continue = InstantiateElement(_continuePrefab); _continueRenderer = _continue != null ? _continue.GetComponent<Renderer>() : null; }
    }

    GameObject InstantiateElement(GameObject prefab)
    {
        if (prefab == null) return null;

        GameObject instance = Instantiate(prefab, _viewRoot != null ? _viewRoot : transform);
        instance.name = prefab.name;
        return instance;
    }

    // ---------- 内部实现 ----------

    void ReturnToHand(CardView card)
    {
        card.SetHome(HandPosition(HandIndexOf(card), HandCount()));
        card.ReturnHome();
    }

    Vector3 HandPosition(int index, int count)
    {
        if (_drawerComponent == null) return Vector3.zero;
        if (!_drawerComponent.IsOpen) return new Vector3(0f, _drawerComponent.HiddenPosition, 0f);

        return _drawerComponent.Slot(index, count);
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
        var cardObject = new GameObject("Card_" + data.Id);
        cardObject.transform.SetParent(_cards != null ? _cards.transform : transform, false);

        var card = cardObject.AddComponent<CardView>();
        card.Initialize(data.Id, data.Text, _cardMaterial, _cardFont, _cardTextColor, _cardSize, _cardTextSize, 10);

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

    static bool Inside(Bounds bounds, Vector3 point)
    {
        return point.x >= bounds.min.x && point.x <= bounds.max.x
            && point.y >= bounds.min.y && point.y <= bounds.max.y;
    }

    string Wrap(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        int limit = Mathf.Max(1, _lineLength);
        var builder = new System.Text.StringBuilder(text.Length + text.Length / limit);
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

            if (count >= limit)
            {
                builder.Append('\n');
                count = 0;
            }
        }

        return builder.ToString();
    }
}
