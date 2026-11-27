using UnityEngine;

/// <summary>按顺序推进流程：每一步的完成条件由该步的数据通过 IsCompleted 回答。</summary>
public sealed class GameManager : MonoBehaviour
{
    [SerializeField, Tooltip("按顺序排列的 stage。")]
    StageData[] _flow;

    readonly GameContext _context = new GameContext();

    int _index = -1;
    bool _started;

    public GameContext Context => _context;
    public StageData CurrentStage => _index >= 0 && _index < _flow.Length ? _flow[_index] : null;

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
            return;
        }

        PresentCurrent();
    }

    void PresentCurrent()
    {
        StageData stage = _flow[_index];

        Debug.LogWarning("没有可用于呈现的 stage 类型：" + stage.name);
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
