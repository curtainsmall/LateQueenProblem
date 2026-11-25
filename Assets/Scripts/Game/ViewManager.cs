using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>唯一的界面管理者：只做抽象层面的控制，元素自身的设置由各组件负责。</summary>
public sealed class ViewManager : MonoBehaviour
{
    [SerializeField, Tooltip("网格。")]
    GameObject _gridPrefab;

    [SerializeField, Tooltip("手牌区。")]
    GameObject _handZonePrefab;

    [SerializeField, Tooltip("卡牌。")]
    GameObject _cardPrefab;

    [SerializeField, Tooltip("插曲底板。")]
    GameObject _uiPanelPrefab;

    [SerializeField, Tooltip("插曲文字。")]
    GameObject _uiTextPrefab;

    [SerializeField, Tooltip("按钮。")]
    GameObject _uiButtonPrefab;

    [SerializeField, Tooltip("Esc 按钮的布局。")]
    ButtonSpec _escSpec;

    [SerializeField, Tooltip("继续按钮的布局。")]
    ButtonSpec _continueSpec;

    [SerializeField, Tooltip("世界元素实例的父节点。")]
    Transform _viewRoot;

    [SerializeField, Tooltip("UI 元素的父节点（Canvas）。")]
    Transform _uiRoot;

    [SerializeField, Tooltip("渲染用的相机。")]
    Camera _camera;

    public event Action ContinueClicked;
    public event Action EscapeClicked;

    GameObject _grid;
    GameObject _cards;
    GameObject _handZoneObject;

    GridMesh _gridMesh;
    HandZone _handZone;

    GameObject _uiPanel;
    GameObject _uiText;
    GameObject _uiContinue;
    GameObject _uiEscape;

    UIText _textElement;

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
        EnsureUi();

        SetActive(_grid, true);
        SetActive(_cards, true);
        SetActive(_handZoneObject, true);
        SetActive(_uiPanel, false);
        SetActive(_uiText, false);
        SetActive(_uiContinue, false);

        if (_gridMesh != null) _gridMesh.Build(level.Rows, level.Cols);

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

        EnsureUi();

        SetActive(_grid, false);
        SetActive(_cards, false);
        SetActive(_handZoneObject, false);
        SetActive(_uiPanel, true);
        SetActive(_uiText, true);
        SetActive(_uiContinue, true);

        if (_textElement != null) _textElement.SetText(interlude.JoinTexts("\n\n"));
    }

    public void HideAll()
    {
        _dragging = null;
        _level = null;
        _interludeVisible = false;

        SetActive(_grid, false);
        SetActive(_cards, false);
        SetActive(_handZoneObject, false);
        SetActive(_uiPanel, false);
        SetActive(_uiText, false);
        SetActive(_uiContinue, false);

        ClearCards();
    }

    // ---------- 输入 ----------

    void Update()
    {
        if (Mouse.current == null) return;

        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        Vector2 screen = Mouse.current.position.ReadValue();

        if (_level != null) UpdateDrawerHover(ScreenToWorld(screen));

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (_dragging == null && overUi) return;
            OnPressed(screen);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame) OnReleased(screen);
        else if (Mouse.current.leftButton.isPressed && _dragging != null) OnDragged(screen);
    }

    void OnPressed(Vector2 screen)
    {
        if (_level == null) return;

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

    void UpdateDrawerHover(Vector3 world)
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

    // ---------- 元素实例 ----------

    void EnsureLevelViews()
    {
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

        if (_handZoneObject == null)
        {
            _handZoneObject = InstantiateElement(_handZonePrefab);
            _handZone = _handZoneObject != null ? _handZoneObject.GetComponent<HandZone>() : null;
        }
    }

    void EnsureUi()
    {
        if (_uiEscape == null)
        {
            _uiEscape = CreateButton(_escSpec);
            var escape = _uiEscape.GetComponent<UIButton>();
            if (escape != null) escape.Clicked += () => EscapeClicked?.Invoke();
        }

        if (_uiPanel == null) _uiPanel = InstantiateUi(_uiPanelPrefab);

        if (_uiText == null)
        {
            _uiText = InstantiateUi(_uiTextPrefab);
            _textElement = _uiText.GetComponent<UIText>();
        }

        if (_uiContinue == null)
        {
            _uiContinue = CreateButton(_continueSpec);
            var button = _uiContinue.GetComponent<UIButton>();
            if (button != null) button.Clicked += () => ContinueClicked?.Invoke();
        }
    }

    GameObject InstantiateElement(GameObject prefab)
    {
        if (prefab == null) return new GameObject("MissingElement");

        GameObject instance = Instantiate(prefab, _viewRoot != null ? _viewRoot : transform);
        instance.name = prefab.name;
        return instance;
    }

    GameObject CreateButton(ButtonSpec spec)
    {
        GameObject instance = InstantiateUi(_uiButtonPrefab);
        var button = instance.GetComponent<UIButton>();

        if (spec != null && button != null)
        {
            button.Setup(spec.Label, spec.Anchor, spec.Position, spec.Size, spec.HierarchyIndex);
        }

        return instance;
    }

    GameObject InstantiateUi(GameObject prefab)
    {
        if (prefab == null) return new GameObject("MissingUiElement");

        GameObject instance = Instantiate(prefab, _uiRoot != null ? _uiRoot : transform);
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
