using System;
using UnityEngine;

/// <summary>插曲：若干段彼此独立的文字，玩家读完并确认即完成。</summary>
[CreateAssetMenu(menuName = "LateQueenProblem/Interlude", fileName = "Interlude")]
public sealed class InterludeData : StageData
{
    [SerializeField, Tooltip("逐段显示的文字。")]
    string[] _texts;

    public string[] Texts => _texts ?? Array.Empty<string>();

    public override bool IsCompleted(GameContext context)
    {
        return context.InterludeAcknowledged;
    }

    public string JoinTexts(string separator)
    {
        if (_texts == null || _texts.Length == 0) return string.Empty;

        return string.Join(separator, _texts);
    }
}
