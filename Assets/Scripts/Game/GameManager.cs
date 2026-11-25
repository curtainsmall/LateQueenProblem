using UnityEngine;

/// <summary>按顺序推进流程：每一步的完成条件由该步的数据通过 IsCompleted 回答。</summary>
public sealed class GameManager : MonoBehaviour
{
    [SerializeField, Tooltip("按顺序排列的关卡与插曲。")]
    StageData[] _flow;

    [SerializeField, Tooltip("界面管理者。")]
    ViewManager _view;

    readonly GameContext _context = new GameContext();

    int _index = -1;
    bool _started;

    public GameContext Context => _context;
    public StageData CurrentStage => _index >= 0 && _index < _flow.Length ? _flow[_index] : null;

    void Awake()
    {
        if (_view != null)
        {
            _view.ContinueClicked += MarkInterludeAcknowledged;
            _view.EscapeClicked += OnEscapeClicked;
        }
    }

    void OnDestroy()
    {
        if (_view != null)
        {
            _view.ContinueClicked -= MarkInterludeAcknowledged;
            _view.EscapeClicked -= OnEscapeClicked;
        }
    }

    void Start()
    {
        Begin();
    }

    void Update()
    {
        if (!_started) return;
        if (_index < 0 || _index >= _flow.Length) return;
        if (!_flow[_index].IsCompleted(_context)) return;

        Advance();
    }

    void Begin()
    {
        if (_flow == null || _flow.Length == 0)
        {
            Debug.LogWarning("流程为空。");
            return;
        }

        _started = true;
        _index = 0;
        PresentCurrent();
    }

    void Advance()
    {
        _context.Reset();
        _index++;

        if (_index >= _flow.Length)
        {
            _index = -1;
            _started = false;
            if (_view != null) _view.HideAll();
            return;
        }

        PresentCurrent();
    }

    void PresentCurrent()
    {
        if (_view == null) return;

        StageData stage = _flow[_index];

        switch (stage)
        {
            case LevelData level:
                _view.ShowLevel(level);
                break;

            case InterludeData interlude:
                _view.ShowInterlude(interlude);
                break;

            default:
                Debug.LogWarning("未知的流程类型：" + stage);
                break;
        }
    }

    void OnEscapeClicked()
    {
        // 暂时没有菜单，留作占位。
    }

    public void MarkInterludeAcknowledged()
    {
        _context.InterludeAcknowledged = true;
    }

    public void MarkPuzzleSolved()
    {
        _context.PuzzleSolved = true;
    }
}
