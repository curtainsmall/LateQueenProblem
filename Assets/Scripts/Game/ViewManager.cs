using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>唯一的界面管理者：按 GameManager 的请求，用通用元素预制体搭出当前界面。</summary>
public sealed class ViewManager : MonoBehaviour
{
    [Header("元素预制体")]
    [SerializeField, Tooltip("底板。")]
    GameObject _panelPrefab;

    [SerializeField, Tooltip("按钮。")]
    GameObject _buttonPrefab;

    [SerializeField, Tooltip("文字。")]
    GameObject _textPrefab;

    [SerializeField, Tooltip("网格。")]
    GameObject _gridPrefab;

    [SerializeField, Tooltip("手牌抽屉。")]
    GameObject _drawerPrefab;

    [SerializeField, Tooltip("卡牌。")]
    GameObject _cardPrefab;

    [Header("场景")]
    [SerializeField, Tooltip("元素实例的父节点。")]
    Transform _viewRoot;

    [SerializeField, Tooltip("渲染用的相机。")]
    Camera _camera;

    [Header("材质")]
    [SerializeField, Tooltip("桌面背景的材质。")]
    Material _backgroundMaterial;

    [SerializeField, Tooltip("文字底板的材质。")]
    Material _panelMaterial;

    [SerializeField, Tooltip("Esc 按钮的材质。")]
    Material _escMaterial;

    [SerializeField, Tooltip("继续按钮的材质。")]
    Material _continueMaterial;

    [SerializeField, Tooltip("卡牌的材质。")]
    Material _cardMaterial;

    [Header("布局")]
    [SerializeField, Tooltip("桌面的位置。")]
    Vector3 _backgroundPosition;

    [SerializeField, Tooltip("桌面的尺寸。")]
    Vector2 _backgroundSize;

    [SerializeField, Tooltip("文字底板的位置。")]
    Vector3 _panelPosition;

    [SerializeField, Tooltip("文字底板的尺寸。")]
    Vector2 _panelSize;

    [SerializeField, Tooltip("Esc 按钮的位置。")]
    Vector3 _escPosition;

    [SerializeField, Tooltip("Esc 按钮的尺寸。")]
    Vector2 _escSize;

    [SerializeField, Tooltip("Esc 按钮的文字。")]
    string _escLabel;

    [SerializeField, Tooltip("继续按钮的位置。")]
    Vector3 _continuePosition;

    [SerializeField, Tooltip("继续按钮的尺寸。")]
    Vector2 _continueSize;

    [SerializeField, Tooltip("继续按钮的文字。")]
    string _continueLabel;

    [SerializeField, Tooltip("文字的位置。")]
    Vector3 _textPosition;

    [SerializeField, Tooltip("文字的颜色。")]
    Color _textColor;

    [Header("卡牌")]
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

    PanelElement _background;
    GameObject _grid;
    GameObject _cards;
    GameObject _drawer;
    ButtonElement _escape;
    PanelElement _textPanel;
    TextElement _text;
    ButtonElement _continueButton;

    GridMesh _gridMesh;
    HandDrawer _drawerComponent;

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

        SetActive(_textPanel, true);
        SetActive(_text, true);
        SetActive(_continueButton, true);

        if (_text != null) _text.Configure(Wrap(interlude.JoinTexts("\n")), _textColor);
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
        SetActive(_textPanel, false);
        SetActive(_text, false);
        SetActive(_continueButton, false);

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
            if (_escape != null && _escape.Contains(world))
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

        if (_interludeVisible && _continueButton != null && _continueButton.Contains(world))
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
        if (_background == null)
        {
            _background = InstantiateElement(_panelPrefab).GetComponent<PanelElement>();
            _background.transform.position = _backgroundPosition;
            _background.Configure(_backgroundMaterial, _backgroundSize);
        }

        if (_grid == null)
        {
            _grid = InstantiateElement(_gridPrefab);
            _gridMesh = _grid != null ? _grid.GetComponent<GridMesh>() : null;
        }

        if (_cards == null)
        {
            _cards = new GameObject("Cards");
            _cards.transform.SetParent(_viewRoot != null ? _viewRoot : transform, false);
        }

        if (_drawer == null)
        {
            _drawer = InstantiateElement(_drawerPrefab);
            _drawerComponent = _drawer != null ? _drawer.GetComponent<HandDrawer>() : null;
            if (_drawerComponent != null) _drawerComponent.Initialize();
        }

        if (_escape == null)
        {
            _escape = InstantiateElement(_buttonPrefab).GetComponent<ButtonElement>();
            _escape.transform.position = _escPosition;
            _escape.Configure(_escLabel, _escMaterial, _escSize);
        }
    }

    void EnsureTextView()
    {
        if (_textPanel == null)
        {
            _textPanel = InstantiateElement(_panelPrefab).GetComponent<PanelElement>();
            _textPanel.transform.position = _panelPosition;
            _textPanel.Configure(_panelMaterial, _panelSize);
        }

        if (_text == null)
        {
            _text = InstantiateElement(_textPrefab).GetComponent<TextElement>();
            _text.transform.position = _textPosition;
        }

        if (_continueButton == null)
        {
            _continueButton = InstantiateElement(_buttonPrefab).GetComponent<ButtonElement>();
            _continueButton.transform.position = _continuePosition;
            _continueButton.Configure(_continueLabel, _continueMaterial, _continueSize);
        }
    }

    GameObject InstantiateElement(GameObject prefab)
    {
        if (prefab == null) return new GameObject("MissingElement");

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
        GameObject cardObject = Instantiate(_cardPrefab, _cards != null ? _cards.transform : transform);
        cardObject.name = "Card_" + data.Id;

        var card = cardObject.GetComponent<CardView>();
        card.Configure(data.Id, data.Text, _cardMaterial, _cardFont, _cardTextColor, _cardSize, _cardTextSize);

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

    static void SetActive(Component target, bool active)
    {
        if (target != null) target.gameObject.SetActive(active);
    }

    static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
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
