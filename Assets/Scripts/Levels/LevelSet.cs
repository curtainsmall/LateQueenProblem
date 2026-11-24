using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>运行期的关卡卡：文字、隐藏集合、以及（若固定）所在格。</summary>
[Serializable]
public class LevelCard
{
    public string id;
    public string text;
    public List<string> follow = new List<string>();
    public List<string> lead = new List<string>();
    public int row;
    public int col;
    public bool isFixed;
}

/// <summary>运行期的一关。</summary>
[Serializable]
public class LevelEntry
{
    public string id;
    public int rows = 12;
    public int cols = 12;
    public List<LevelCard> cards = new List<LevelCard>();
    public List<int> rowDirections = new List<int>();
    public List<int> colDirections = new List<int>();
    public List<int> rowLocked = new List<int>();
    public List<int> colLocked = new List<int>();
    public bool flipAllowed = true;
    public int requiredTopologies = 1;
}

/// <summary>全部关卡，按顺序排列；由设计器导出到 Assets 下的资产。</summary>
[CreateAssetMenu(menuName = "LateQueenProblem/LevelSet", fileName = "LevelSet")]
public class LevelSet : ScriptableObject
{
    [SerializeField, Tooltip("按顺序排列的关卡。")]
    List<LevelEntry> _levels = new List<LevelEntry>();

    public IReadOnlyList<LevelEntry> Levels => _levels;

    public void SetLevels(List<LevelEntry> levels)
    {
        _levels = levels ?? new List<LevelEntry>();
    }
}
