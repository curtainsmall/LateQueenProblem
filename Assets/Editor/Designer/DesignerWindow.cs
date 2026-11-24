using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>设计器窗口：左-情况、中-可平移缩放的网格、右-属性与统计。</summary>
public class DesignerWindow : EditorWindow
{
    const float BaseCellSize = 96f;


    DesignerWorkspace _workspace;
    string _workspacePath = DesignerIO.DefaultWorkspacePath;
    string _exportPath = DesignerIO.DefaultExportPath;

    int _selectedIndex = -1;
    string _selectedCardId;
    Vector2 _listScroll;
    Vector2 _rightScroll;
    Vector2 _resultScroll;

    float _zoom = 1f;
    Vector2 _pan;
    float _lastFitCell;
    DesignerSituation _fallbackSituation;
    bool _viewFocused;
    bool _panning;

    string _dragCardId;
    int _dragRow = -1;
    int _dragCol = -1;
    Vector2 _dragOffset;
    Vector2 _dragLocal;
    int _hoverRow = -1;
    int _hoverCol = -1;

    string _mergeSourceClassId;
    string _mergeTargetClassId;

    DesignerSolver.Result _result;
    string _status = "就绪";
    bool _showTopologies;

    int _randomMinCards = 3;
    int _randomMaxCards = 6;
    int _randomFixedCount = 1;

    [MenuItem("LateQueenProblem/Designer")]
    static void Open()
    {
        DesignerWindow window = GetWindow<DesignerWindow>("Designer");
        window.minSize = new Vector2(1200f, 700f);
    }

    void OnEnable()
    {
        if (_workspace == null)
        {
            DesignerWorkspace loaded = DesignerIO.Load(_workspacePath, out string error);
            _workspace = loaded ?? new DesignerWorkspace();
            if (loaded == null && !string.IsNullOrEmpty(error)) _status = "未找到工作区，已新建";
        }
    }

    DesignerSituation Selected =>
        _workspace != null && _selectedIndex >= 0 && _selectedIndex < _workspace.situations.Count
            ? _workspace.situations[_selectedIndex]
            : null;

    DesignerCard SelectedCard => Selected != null && !string.IsNullOrEmpty(_selectedCardId) ? Selected.FindCard(_selectedCardId) : null;

    void OnGUI()
    {
        _workspace ??= new DesignerWorkspace();

        DrawToolbar();

        DesignerSituation situation = Selected;

        EditorGUILayout.BeginHorizontal();

        DesignerSituation display = situation ?? EnsureFallbackSituation();
        if (situation != null) situation.EnsureShape();

        // 画布（视图）尺寸 = 内容区高度；宽度按行列比换算。
        float canvasAspect = display.cols / (float)Mathf.Max(1, display.rows);
        float canvasHeight = Mathf.Max(160f, position.height - 44f);
        float canvasWidth = canvasHeight * canvasAspect;

        float sideWidth = Mathf.Max(140f, (position.width - canvasWidth) * 0.5f - 10f);

        DrawLeftColumn(sideWidth);

        EditorGUILayout.BeginVertical(GUILayout.Width(canvasWidth), GUILayout.Height(canvasHeight + 22f));

        DrawGridViewToolbar(situation);

        Rect view = GUILayoutUtility.GetRect(canvasWidth, canvasHeight, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
        DrawCanvas(display, view);

        EditorGUILayout.EndVertical();

        DrawRightColumn(situation, sideWidth);

        EditorGUILayout.EndHorizontal();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60f))) SaveWorkspace();
        if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(60f))) LoadWorkspace();
        if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(70f))) ExportLevels();
        GUILayout.FlexibleSpace();
        GUILayout.Label(_status, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }

    void DrawLeftColumn(float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.ExpandHeight(true));
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

        EditorGUILayout.LabelField("情况", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新建")) AddSituation();
        if (GUILayout.Button("复制") && Selected != null)
        {
            DesignerSituation copy = Selected.Clone(NextId());
            _workspace.situations.Add(copy);
            _selectedIndex = _workspace.situations.Count - 1;
            _viewFocused = false;
        }
        if (GUILayout.Button("删除") && Selected != null)
        {
            _workspace.levelOrder.Remove(Selected.id);
            _workspace.situations.RemoveAt(_selectedIndex);
            _selectedIndex = Mathf.Clamp(_selectedIndex - 1, -1, _workspace.situations.Count - 1);
            _result = null;
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < _workspace.situations.Count; i++)
        {
            DesignerSituation situation = _workspace.situations[i];
            bool isLevel = _workspace.levelOrder.Contains(situation.id);
            string label = (isLevel ? "[L] " : "    ") + situation.id + "  " + situation.cards.Count + "卡";
            if (GUILayout.Toggle(_selectedIndex == i, label, "Button"))
            {
                if (_selectedIndex != i) _viewFocused = false;
                _selectedIndex = i;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("关卡表", EditorStyles.boldLabel);
        for (int i = 0; i < _workspace.levelOrder.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField((i + 1) + ". " + _workspace.levelOrder[i]);
            if (GUILayout.Button("↑", GUILayout.Width(22f)) && i > 0) SwapLevels(i, i - 1);
            if (GUILayout.Button("↓", GUILayout.Width(22f)) && i < _workspace.levelOrder.Count - 1) SwapLevels(i, i + 1);
            if (GUILayout.Button("×", GUILayout.Width(22f))) _workspace.levelOrder.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("随机生成", EditorStyles.boldLabel);
        _randomMinCards = EditorGUILayout.IntField("最少卡数", _randomMinCards);
        _randomMaxCards = EditorGUILayout.IntField("最多卡数", _randomMaxCards);
        _randomFixedCount = EditorGUILayout.IntField("固定卡数", _randomFixedCount);

        if (GUILayout.Button("生成新情况"))
        {
            _workspace.situations.Add(RandomSituation());
            _selectedIndex = _workspace.situations.Count - 1;
            _selectedCardId = null;
            _result = null;
            _viewFocused = false;
            _status = "已随机生成";
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("路径", EditorStyles.boldLabel);
        _workspacePath = EditorGUILayout.TextField("工作区", _workspacePath);
        _exportPath = EditorGUILayout.TextField("导出", _exportPath);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void SwapLevels(int a, int b)
    {
        (_workspace.levelOrder[a], _workspace.levelOrder[b]) = (_workspace.levelOrder[b], _workspace.levelOrder[a]);
    }

    void DrawGridViewToolbar(DesignerSituation situation)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("居中", EditorStyles.toolbarButton, GUILayout.Width(50f))) _viewFocused = false;
        GUI.enabled = situation != null;
        if (GUILayout.Button("添加卡", EditorStyles.toolbarButton, GUILayout.Width(60f))) AddCard(situation);
        GUI.enabled = true;

        GUILayout.Label("缩放 " + _zoom.ToString("F2"), EditorStyles.miniLabel, GUILayout.Width(80f));
        GUILayout.Label("左键选中 / 右键拖卡 / 右键空白处平移 / 滚轮缩放", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    // 画布：高度等于内容区高度，宽度按网格行列比换算，放不下时按内容区宽度收缩，始终居中。
    static Rect CanvasRect(DesignerSituation situation, Rect area)
    {
        float aspect = situation.cols / (float)Mathf.Max(1, situation.rows);

        float height = area.height;
        float width = height * aspect;

        if (width > area.width)
        {
            width = area.width;
            height = width / aspect;
        }

        return new Rect(
            area.x + (area.width - width) * 0.5f,
            area.y + (area.height - height) * 0.5f,
            width,
            height);
    }

    void DrawCanvas(DesignerSituation display, Rect view)
    {
        EditorGUI.DrawRect(view, new Color(0.16f, 0.16f, 0.17f, 1f));

        float fit = FitZoom(display, view);
        if (fit <= 0f)
        {
            GUI.Label(view, "画布过小", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        float fitCell = BaseCellSize * fit;
        float maxRelative = Mathf.Max(1f, Mathf.Min(view.width, view.height) / (3f * BaseCellSize) / fit);

        if (!_viewFocused)
        {
            _zoom = 1f;
            _pan = CenterPan(display, view, fitCell);
            _lastFitCell = fitCell;
            _viewFocused = true;
        }

        _zoom = Mathf.Clamp(_zoom, 1f, maxRelative);

        // 画布尺寸变化时，格子像素按同一比例变化；平移量同比例缩放，画面内容按比例保持一致。
        if (_lastFitCell > 0f) _pan *= fitCell / _lastFitCell;
        _lastFitCell = fitCell;

        float cell = fitCell * _zoom;
        _pan = ClampPan(display, view, cell, _pan);

        HandleViewEvents(display, view, fitCell, maxRelative);

        GUI.BeginGroup(view);
        DrawGridCells(display, view, cell);
        GUI.EndGroup();
    }    DesignerSituation EnsureFallbackSituation()
    {
        if (_fallbackSituation == null)
        {
            _fallbackSituation = new DesignerSituation { id = "(empty)", rows = 12, cols = 12 };
            _fallbackSituation.EnsureShape();
        }

        return _fallbackSituation;
    }

    static float FitZoom(DesignerSituation situation, Rect view)
    {
        if (view.width <= 1f || view.height <= 1f) return 0f;

        // 网格四周各留 1/4 格边距：视图 = (格数 + 0.5) × 格子尺寸 × 缩放
        float widthZoom = view.width / (Mathf.Max(1, situation.cols) + 0.5f) / BaseCellSize;
        float heightZoom = view.height / (Mathf.Max(1, situation.rows) + 0.5f) / BaseCellSize;
        return Mathf.Max(0.01f, Mathf.Min(widthZoom, heightZoom));
    }

    static float MaxZoom(Rect view)
    {
        return Mathf.Max(0.01f, Mathf.Min(view.width, view.height) / (3f * BaseCellSize));
    }

    static Vector2 CenterPan(DesignerSituation situation, Rect view, float cell)
    {
        return new Vector2(
            (view.width - situation.cols * cell) * 0.5f,
            (view.height - situation.rows * cell) * 0.5f);
    }
    Vector2 ClampPan(DesignerSituation situation, Rect view, float cell, Vector2 pan)
    {
        float gridWidth = situation.cols * cell;
        float gridHeight = situation.rows * cell;

        return new Vector2(
            ClampAxis(pan.x, view.width, gridWidth, cell),
            ClampAxis(pan.y, view.height, gridHeight, cell));
    }

    // 网格小于视图时四周各留 1/4 格；网格大于视图时网格始终盖住视图。
    static float ClampAxis(float pan, float viewSize, float gridSize, float cell)
    {
        float margin = cell * 0.25f;
        float lower = margin;
        float upper = viewSize - gridSize - margin;

        if (lower <= upper) return Mathf.Clamp(pan, lower, upper);

        lower = viewSize - gridSize;
        upper = 0f;
        return Mathf.Clamp(pan, Mathf.Min(lower, upper), Mathf.Max(lower, upper));
    }

    void DrawGridCells(DesignerSituation situation, Rect view, float cell)
    {
        Color border = new Color(0.42f, 0.42f, 0.45f, 1f);
        Color empty = new Color(0.26f, 0.26f, 0.28f, 1f);
        Color occupied = new Color(0.34f, 0.44f, 0.60f, 1f);
        Color fixedColor = new Color(0.62f, 0.42f, 0.22f, 1f);
        Color selected = new Color(0.85f, 0.75f, 0.20f, 1f);
        Color target = new Color(0.40f, 0.85f, 0.55f, 1f);

        float originX = _pan.x;
        float originY = _pan.y;

        float gridWidth = situation.cols * cell;
        float gridHeight = situation.rows * cell;

        bool dragging = !string.IsNullOrEmpty(_dragCardId);
        bool hasTarget = dragging && _hoverRow >= 0 && _hoverCol >= 0 && !situation.IsOccupied(_hoverRow, _hoverCol);

        EditorGUI.DrawRect(new Rect(originX, originY, gridWidth, gridHeight), empty);

        GUIStyle textStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize = Mathf.RoundToInt(Mathf.Clamp(cell * 0.14f, 8f, 16f)),
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };

        GUIStyle idStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.LowerRight };

        for (int row = 0; row < situation.rows; row++)
        {
            for (int col = 0; col < situation.cols; col++)
            {
                float x = originX + col * cell;
                float y = originY + row * cell;

                if (x + cell < 0f || y + cell < 0f || x > view.width || y > view.height) continue;

                DesignerCard card = situation.CardAt(row, col);
                bool isDraggedCard = card != null && dragging && card.id == _dragCardId;

                if (card != null && !isDraggedCard)
                {
                    EditorGUI.DrawRect(new Rect(x + 1f, y + 1f, cell - 2f, cell - 2f), card.isFixed ? fixedColor : occupied);

                    if (card.id == _selectedCardId)
                    {
                        EditorGUI.DrawRect(new Rect(x + 1f, y + 1f, cell - 2f, 2f), selected);
                        EditorGUI.DrawRect(new Rect(x + 1f, y + cell - 3f, cell - 2f, 2f), selected);
                        EditorGUI.DrawRect(new Rect(x + 1f, y + 1f, 2f, cell - 2f), selected);
                        EditorGUI.DrawRect(new Rect(x + cell - 3f, y + 1f, 2f, cell - 2f), selected);
                    }

                    GUI.Label(new Rect(x + 4f, y + 4f, cell - 8f, cell - 20f), card.text, textStyle);

                    if (card.isFixed) GUI.Label(new Rect(x + 3f, y + cell - 18f, 18f, 16f), "F", EditorStyles.boldLabel);

                    GUI.Label(new Rect(x, y + cell - 16f, cell - 4f, 14f), card.id, idStyle);
                }

                if (hasTarget && row == _hoverRow && col == _hoverCol)
                {
                    EditorGUI.DrawRect(new Rect(x, y, cell, 3f), target);
                    EditorGUI.DrawRect(new Rect(x, y + cell - 3f, cell, 3f), target);
                    EditorGUI.DrawRect(new Rect(x, y, 3f, cell), target);
                    EditorGUI.DrawRect(new Rect(x + cell - 3f, y, 3f, cell), target);
                }

                EditorGUI.DrawRect(new Rect(x, y, cell, 1f), border);
                EditorGUI.DrawRect(new Rect(x, y, 1f, cell), border);
            }
        }

        EditorGUI.DrawRect(new Rect(originX, originY + gridHeight - 1f, gridWidth, 1f), border);
        EditorGUI.DrawRect(new Rect(originX + gridWidth - 1f, originY, 1f, gridHeight), border);

        if (!dragging) return;

        DesignerCard dragged = situation.FindCard(_dragCardId);
        if (dragged == null) return;

        Rect floating = new Rect(_dragLocal.x - _dragOffset.x, _dragLocal.y - _dragOffset.y, cell, cell);
        EditorGUI.DrawRect(floating, new Color(0.36f, 0.48f, 0.68f, 0.92f));
        EditorGUI.DrawRect(new Rect(floating.x, floating.y, floating.width, 2f), selected);
        EditorGUI.DrawRect(new Rect(floating.x, floating.yMax - 2f, floating.width, 2f), selected);
        EditorGUI.DrawRect(new Rect(floating.x, floating.y, 2f, floating.height), selected);
        EditorGUI.DrawRect(new Rect(floating.xMax - 2f, floating.y, 2f, floating.height), selected);
        GUI.Label(new Rect(floating.x + 4f, floating.y + 4f, floating.width - 8f, floating.height - 20f), dragged.text, textStyle);
        GUI.Label(new Rect(floating.x, floating.yMax - 16f, floating.width - 4f, 14f), dragged.id, idStyle);
    }
    void HandleViewEvents(DesignerSituation situation, Rect view, float fitCell, float maxRelative)
    {
        Event current = Event.current;
        Vector2 local = current.mousePosition - new Vector2(view.x, view.y);
        bool inside = local.x >= 0f && local.y >= 0f && local.x < view.width && local.y < view.height;

        float cell = fitCell * _zoom;
        int col = Mathf.FloorToInt((local.x - _pan.x) / cell);
        int row = Mathf.FloorToInt((local.y - _pan.y) / cell);
        bool onGrid = inside && row >= 0 && row < situation.rows && col >= 0 && col < situation.cols;

        _hoverRow = onGrid ? row : -1;
        _hoverCol = onGrid ? col : -1;

        if (current.type == EventType.ScrollWheel && inside)
        {
            float previousCell = cell;
            _zoom = Mathf.Clamp(_zoom * (1f - current.delta.y * 0.03f), 1f, maxRelative);
            cell = fitCell * _zoom;

            Vector2 world = (local - _pan) / previousCell;
            _pan = ClampPan(situation, view, cell, local - world * cell);

            current.Use();
            Repaint();
            return;
        }

        if (current.type == EventType.MouseDown && current.button == 1 && inside)
        {
            DesignerCard pressed = onGrid ? situation.CardAt(row, col) : null;

            if (pressed != null && !pressed.isFixed)
            {
                _dragCardId = pressed.id;
                _dragRow = row;
                _dragCol = col;
                _dragOffset = local - new Vector2(_pan.x + col * cell, _pan.y + row * cell);
                _dragLocal = local;
            }
            else
            {
                _panning = pressed == null;
            }

            current.Use();
            return;
        }

        if (current.type == EventType.MouseDown && current.button == 2 && inside)
        {
            _panning = true;
            current.Use();
            return;
        }

        if (current.type == EventType.MouseDrag && _panning)
        {
            _pan = ClampPan(situation, view, cell, _pan + current.delta);
            current.Use();
            Repaint();
            return;
        }

        if (current.type == EventType.MouseUp && _panning)
        {
            _panning = false;
            current.Use();
            return;
        }

        if (current.type == EventType.MouseDrag && !string.IsNullOrEmpty(_dragCardId))
        {
            _dragLocal = local;
            current.Use();
            Repaint();
            return;
        }

        if (current.type == EventType.MouseUp && !string.IsNullOrEmpty(_dragCardId))
        {
            DesignerCard card = situation.FindCard(_dragCardId);

            if (card != null && onGrid && !situation.IsOccupied(row, col) && (row != card.row || col != card.col))
            {
                card.row = row;
                card.col = col;
                situation.RebuildEdges();
                _status = "已移动 " + card.id + " → (" + row + "," + col + ")";
            }

            _dragCardId = null;
            _dragRow = -1;
            _dragCol = -1;
            _dragLocal = Vector2.zero;
            _dragOffset = Vector2.zero;
            current.Use();
            Repaint();
            return;
        }

        if (current.type == EventType.MouseDown && current.button == 0 && onGrid)
        {
            DesignerCard card = situation.CardAt(row, col);
            _selectedCardId = card == null ? null : card.id;
            current.Use();
            Repaint();
        }
    }    void DrawRightColumn(DesignerSituation situation, float width)
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(width), GUILayout.ExpandHeight(true));
        _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

        if (situation == null)
        {
            EditorGUILayout.HelpBox("没有选中的情况。", MessageType.Info);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            return;
        }

        DrawSituationHeader(situation);
        DrawCardProperties(situation);
        DrawClassTable(situation);
        DrawResultPanel(situation);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawSituationHeader(DesignerSituation situation)
    {
        EditorGUILayout.LabelField("情况", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        situation.id = EditorGUILayout.TextField("ID", situation.id);
        if (GUILayout.Button("加入", GUILayout.Width(50f)))
        {
            if (!_workspace.levelOrder.Contains(situation.id)) _workspace.levelOrder.Add(situation.id);
        }
        EditorGUILayout.EndHorizontal();

        bool inLevels = _workspace.levelOrder.Contains(situation.id);
        if (GUILayout.Button(inLevels ? "从关卡表移除" : "加入关卡表"))
        {
            if (inLevels) _workspace.levelOrder.Remove(situation.id);
            else _workspace.levelOrder.Add(situation.id);
        }

        situation.rows = Mathf.Max(1, EditorGUILayout.IntField("行", situation.rows));
        situation.cols = Mathf.Max(1, EditorGUILayout.IntField("列", situation.cols));
        situation.EnsureShape();

        situation.rules.flipAllowed = EditorGUILayout.ToggleLeft("可翻转方向", situation.rules.flipAllowed);
        situation.rules.requiredTopologies = Mathf.Max(1, EditorGUILayout.IntField("要求拓扑数", situation.rules.requiredTopologies));
    }

    void DrawCardProperties(DesignerSituation situation)
    {
        DesignerCard card = SelectedCard;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("卡属性", EditorStyles.boldLabel);

        if (card == null)
        {
            EditorGUILayout.HelpBox("左键点击网格中的卡。", MessageType.None);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        card.id = EditorGUILayout.TextField("ID", card.id, GUILayout.Width(180f));
        card.isFixed = EditorGUILayout.ToggleLeft("固定", card.isFixed, GUILayout.Width(56f));
        EditorGUILayout.EndHorizontal();

        card.text = EditorGUILayout.TextField("文字", card.text);

        string follow = string.Join(",", situation.FollowOf(card.id));
        string lead = string.Join(",", situation.LeadOf(card.id));
        EditorGUILayout.LabelField("跟随集", string.IsNullOrEmpty(follow) ? "（空）" : follow);
        EditorGUILayout.LabelField("引出集", string.IsNullOrEmpty(lead) ? "（空）" : lead);

        if (GUILayout.Button("删除卡"))
        {
            situation.cards.Remove(card);
            situation.RebuildEdges();
            _selectedCardId = null;
        }
    }

    void DrawClassTable(DesignerSituation situation)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("类（自动生成，只能合并）", EditorStyles.boldLabel);

        if (situation.classes.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有相邻卡，因此没有类。", MessageType.Info);
            return;
        }

        for (int i = 0; i < situation.classes.Count; i++)
        {
            DesignerClass cls = situation.classes[i];
            string label = cls.id + "（" + situation.EdgeCountOf(cls.id) + " 边）";

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_mergeSourceClassId == cls.id, "A", GUILayout.Width(24f))) _mergeSourceClassId = cls.id;
            if (GUILayout.Toggle(_mergeTargetClassId == cls.id, "B", GUILayout.Width(24f))) _mergeTargetClassId = cls.id;
            EditorGUILayout.LabelField(label);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !string.IsNullOrEmpty(_mergeSourceClassId) && !string.IsNullOrEmpty(_mergeTargetClassId) &&
                      _mergeSourceClassId != _mergeTargetClassId;

        if (GUILayout.Button("合并 B → A"))
        {
            situation.MergeClasses(_mergeTargetClassId, _mergeSourceClassId);
            _mergeTargetClassId = null;
            _status = "已合并类";
        }

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
    }

    void DrawResultPanel(DesignerSituation situation)
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Test", GUILayout.Height(24f))) RunTest(situation);
        _showTopologies = EditorGUILayout.ToggleLeft("显示拓扑", _showTopologies, GUILayout.Width(90f));
        EditorGUILayout.EndHorizontal();

        if (_result == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("状态", _result.message);
        EditorGUILayout.LabelField("合法摆法", _result.placementCount.ToString());
        EditorGUILayout.LabelField("不同拓扑", _result.topologyCount + (_result.topologyCountCapped ? "（超过上限）" : string.Empty));
        EditorGUILayout.LabelField("搜索节点", _result.nodes.ToString());
        EditorGUILayout.LabelField("耗时", _result.milliseconds.ToString("F0") + " ms");


        if (_showTopologies && _result.topologies.Count > 0)
        {
            _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll, GUILayout.Height(120f));
            for (int i = 0; i < _result.topologies.Count; i++)
            {
                EditorGUILayout.LabelField((i + 1) + ". " + (_result.topologies[i].Length == 0 ? "（无边）" : _result.topologies[i]));
            }
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    void RunTest(DesignerSituation situation)
    {
        situation.RebuildEdges();
        _result = DesignerSolver.Test(situation.BuildSolverSituation());
        _status = "统计完成";
        Repaint();
    }

    void AddCard(DesignerSituation situation)
    {
        for (int row = 0; row < situation.rows; row++)
        {
            for (int col = 0; col < situation.cols; col++)
            {
                if (situation.IsOccupied(row, col)) continue;

                var card = new DesignerCard { id = NextCardId(situation), text = "text", row = row, col = col };
                situation.cards.Add(card);
                situation.RebuildEdges();
                _selectedCardId = card.id;
                return;
            }
        }

        _status = "没有空位";
    }

    static string NextCardId(DesignerSituation situation)
    {
        int index = situation.cards.Count;
        string id = "c" + index;

        while (situation.FindCard(id) != null)
        {
            index++;
            id = "c" + index;
        }

        return id;
    }

    void AddSituation()
    {
        var situation = new DesignerSituation { id = NextId(), name = string.Empty };
        situation.EnsureShape();
        _workspace.situations.Add(situation);
        _selectedIndex = _workspace.situations.Count - 1;
        _selectedCardId = null;
        _result = null;
        _viewFocused = false;
    }

    string NextId()
    {
        int index = _workspace.situations.Count;
        string id = "s" + index;

        while (_workspace.FindSituation(id) != null)
        {
            index++;
            id = "s" + index;
        }

        return id;
    }

    DesignerSituation RandomSituation()
    {
        var situation = new DesignerSituation { id = NextId() };
        situation.EnsureShape();

        int maxCards = Mathf.Min(Mathf.Max(1, _randomMaxCards), situation.rows * situation.cols);
        int minCards = Mathf.Clamp(_randomMinCards, 1, maxCards);
        int cardCount = Random.Range(minCards, maxCards + 1);

        int row = Random.Range(0, situation.rows);
        int col = Random.Range(0, situation.cols);
        situation.cards.Add(new DesignerCard { id = "c0", text = "text_0", row = row, col = col });

        var frontier = new List<Vector2Int>();

        for (int i = 1; i < cardCount; i++)
        {
            frontier.Clear();

            for (int c = 0; c < situation.cards.Count; c++)
            {
                DesignerCard card = situation.cards[c];
                AddFrontier(situation, frontier, card.row - 1, card.col);
                AddFrontier(situation, frontier, card.row + 1, card.col);
                AddFrontier(situation, frontier, card.row, card.col - 1);
                AddFrontier(situation, frontier, card.row, card.col + 1);
            }

            if (frontier.Count == 0) break;

            Vector2Int cell = frontier[Random.Range(0, frontier.Count)];
            situation.cards.Add(new DesignerCard { id = "c" + i, text = "text_" + i, row = cell.x, col = cell.y });
        }

        for (int i = 0; i < situation.cards.Count && i < _randomFixedCount; i++)
        {
            situation.cards[i].isFixed = true;
        }

        situation.RebuildEdges();
        return situation;
    }

    static void AddFrontier(DesignerSituation situation, List<Vector2Int> frontier, int row, int col)
    {
        if (row < 0 || row >= situation.rows || col < 0 || col >= situation.cols) return;
        if (situation.IsOccupied(row, col)) return;

        var candidate = new Vector2Int(row, col);
        if (!frontier.Contains(candidate)) frontier.Add(candidate);
    }
    void SaveWorkspace()
    {
        DesignerIO.Save(_workspace, _workspacePath);
        _status = "已保存：" + _workspacePath;
        Repaint();
    }

    void LoadWorkspace()
    {
        DesignerWorkspace loaded = DesignerIO.Load(_workspacePath, out string error);

        if (loaded == null)
        {
            _status = "加载失败：" + error;
            return;
        }

        _workspace = loaded;
        _selectedIndex = -1;
        _selectedCardId = null;
        _result = null;
        _viewFocused = false;
        _status = "已加载：" + _workspacePath;
        Repaint();
    }

    void ExportLevels()
    {
        bool ok = DesignerIO.ExportAssets(_workspace, _exportPath, out string error);

        _status = ok
            ? "已导出 " + _workspace.levelOrder.Count + " 个关卡：" + _exportPath
            : "导出失败：" + error;

        Repaint();
    }
}
