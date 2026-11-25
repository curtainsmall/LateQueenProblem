using UnityEngine;

/// <summary>一步流程共享的运行时状态：由表现层写入，由数据层的 IsCompleted 读取。</summary>
public sealed class GameContext
{
    public bool PuzzleSolved { get; set; }

    public bool InterludeAcknowledged { get; set; }

    public void Reset()
    {
        PuzzleSolved = false;
        InterludeAcknowledged = false;
    }
}

/// <summary>流程中的一步：关卡或插曲。完成条件由数据自己回答。</summary>
public abstract class StageData : ScriptableObject
{
    [SerializeField, Tooltip("稳定标识，便于调试与存档。")]
    string _id;

    [SerializeField, Tooltip("显示用标题。")]
    string _title;

    public string Id => _id;
    public string Title => _title;

    public abstract bool IsCompleted(GameContext context);
}
