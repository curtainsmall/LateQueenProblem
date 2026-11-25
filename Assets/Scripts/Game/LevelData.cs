using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>一张卡：数据里的唯一实体，运行时被实例化成 CardView。</summary>
[Serializable]
public sealed class CardData
{
    [SerializeField, Tooltip("稳定标识。")]
    string _id;

    [SerializeField, Tooltip("卡面文字。")]
    string _text;

    [SerializeField, Tooltip("预设行坐标。")]
    int _row;

    [SerializeField, Tooltip("预设列坐标。")]
    int _col;

    public string Id => _id;
    public string Text => _text;
    public int Row => _row;
    public int Col => _col;
}

/// <summary>一个关卡：网格、背景、卡与它们的预设位置。</summary>
[CreateAssetMenu(menuName = "LateQueenProblem/Level", fileName = "Level")]
public sealed class LevelData : StageData
{
    [SerializeField, Tooltip("网格行数。")]
    int _rows;

    [SerializeField, Tooltip("网格列数。")]
    int _cols;

    [SerializeField, Tooltip("背景图，可为空。")]
    Sprite _background;

    [SerializeField, Tooltip("本关的全部卡。")]
    CardData[] _cards;

    public int Rows => _rows;
    public int Cols => _cols;
    public Sprite Background => _background;
    public IReadOnlyList<CardData> Cards => _cards ?? Array.Empty<CardData>();

    public override bool IsCompleted(GameContext context)
    {
        return context.PuzzleSolved;
    }

    public CardData FindCard(string cardId)
    {
        if (_cards == null) return null;

        for (int i = 0; i < _cards.Length; i++)
        {
            if (_cards[i] != null && _cards[i].Id == cardId) return _cards[i];
        }

        return null;
    }

    /// <summary>预设布局的拓扑：每条非空行、每条非空列的有序卡 Id。</summary>
    public List<string> GetTopology()
    {
        return LevelTopology.Build(_rows, _cols, (row, col) => ExpectedCardAt(row, col));
    }

    string ExpectedCardAt(int row, int col)
    {
        if (_cards == null) return null;

        for (int i = 0; i < _cards.Length; i++)
        {
            CardData card = _cards[i];
            if (card != null && card.Row == row && card.Col == col) return card.Id;
        }

        return null;
    }
}

/// <summary>由"格子 → 卡 Id"推导拓扑指纹；行与列同义，因此转置不影响结果。</summary>
public static class LevelTopology
{
    public static List<string> Build(int rows, int cols, Func<int, int, string> cardAt)
    {
        var chains = new List<string>();

        for (int row = 0; row < rows; row++)
        {
            string chain = Chain(0, cols, (i) => cardAt(row, i));
            if (!string.IsNullOrEmpty(chain)) chains.Add(chain);
        }

        for (int col = 0; col < cols; col++)
        {
            string chain = Chain(0, rows, (i) => cardAt(i, col));
            if (!string.IsNullOrEmpty(chain)) chains.Add(chain);
        }

        chains.Sort();
        return chains;
    }

    static string Chain(int from, int count, Func<int, string> cardAt)
    {
        string chain = string.Empty;

        for (int i = from; i < count; i++)
        {
            string cardId = cardAt(i);
            if (string.IsNullOrEmpty(cardId)) continue;

            chain = chain.Length == 0 ? cardId : chain + "|" + cardId;
        }

        return chain;
    }

    public static bool AreEqual(List<string> left, List<string> right)
    {
        if (left == null || right == null) return false;
        if (left.Count != right.Count) return false;

        for (int i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i]) return false;
        }

        return true;
    }
}
