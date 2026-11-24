using System;
using System.Collections.Generic;

/// <summary>网格上的一张卡。</summary>
[Serializable]
public class DesignerCard
{
    public string id;
    public string text;
    public int row;
    public int col;
    public bool isFixed;
}

/// <summary>自动生成的类；只能合并，不能新建。</summary>
[Serializable]
public class DesignerClass
{
    public string id;
}

/// <summary>一条邻接：from 在时间线上排在 to 之前；classId 决定它属于哪个类。</summary>
[Serializable]
public class DesignerEdge
{
    public string fromId;
    public string toId;
    public string classId;
}

/// <summary>关卡规则开关。</summary>
[Serializable]
public class DesignerRules
{
    public bool flipAllowed = true;
    public bool hasFixedCards = true;
    public int requiredTopologies = 1;
}

/// <summary>一个情况：网格、放在网格上的卡、自动生成的类与边、规则。</summary>
[Serializable]
public class DesignerSituation
{
    public string id;
    public string name;
    public int rows = 12;
    public int cols = 12;
    public List<DesignerCard> cards = new List<DesignerCard>();
    public List<DesignerClass> classes = new List<DesignerClass>();
    public List<DesignerEdge> edges = new List<DesignerEdge>();
    public List<int> rowDirections = new List<int>();
    public List<int> colDirections = new List<int>();
    public List<int> rowLocked = new List<int>();
    public List<int> colLocked = new List<int>();
    public DesignerRules rules = new DesignerRules();

    public void EnsureShape()
    {
        if (rows <= 0) rows = 12;
        if (cols <= 0) cols = 12;

        Normalize(rowDirections, rows, 1);
        Normalize(colDirections, cols, 1);
        Normalize(rowLocked, rows, 0);
        Normalize(colLocked, cols, 0);

        cards ??= new List<DesignerCard>();
        classes ??= new List<DesignerClass>();
        edges ??= new List<DesignerEdge>();
        rules ??= new DesignerRules();
    }

    static void Normalize(List<int> values, int count, int fill)
    {
        if (values == null)
        {
            values = new List<int>();
        }

        while (values.Count < count) values.Add(fill);
        while (values.Count > count) values.RemoveAt(values.Count - 1);
    }

    public DesignerCard CardAt(int row, int col)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].row == row && cards[i].col == col) return cards[i];
        }

        return null;
    }

    public DesignerCard FindCard(string cardId)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].id == cardId) return cards[i];
        }

        return null;
    }

    public bool IsOccupied(int row, int col)
    {
        return CardAt(row, col) != null;
    }

    /// <summary>按当前布局重建边表：已有的边保留其类，新的边分配新类，消失的边被移除。</summary>
    public void RebuildEdges()
    {
        EnsureShape();

        var wanted = new List<DesignerEdge>();
        var keep = new Dictionary<string, string>();

        for (int i = 0; i < edges.Count; i++)
        {
            DesignerEdge edge = edges[i];
            if (edge == null) continue;
            keep[Key(edge.fromId, edge.toId)] = edge.classId;
        }

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                DesignerCard card = CardAt(row, col);
                if (card == null) continue;

                if (col + 1 < cols)
                {
                    DesignerCard right = CardAt(row, col + 1);
                    if (right != null) AddEdge(wanted, keep, card, right, rowDirections[row] >= 0);
                }

                if (row + 1 < rows)
                {
                    DesignerCard below = CardAt(row + 1, col);
                    if (below != null) AddEdge(wanted, keep, card, below, colDirections[col] >= 0);
                }
            }
        }

        edges = wanted;
        PruneClasses();
    }

    void AddEdge(List<DesignerEdge> target, Dictionary<string, string> keep, DesignerCard a, DesignerCard b, bool forward)
    {
        string from = forward ? a.id : b.id;
        string to = forward ? b.id : a.id;
        string key = Key(from, to);

        if (!keep.TryGetValue(key, out string classId) || string.IsNullOrEmpty(classId))
        {
            classId = NewClass();
        }

        target.Add(new DesignerEdge { fromId = from, toId = to, classId = classId });
    }

    string NewClass()
    {
        string id = "k" + (classes.Count + 1);
        int suffix = classes.Count + 1;

        while (FindClass(id) != null)
        {
            suffix++;
            id = "k" + suffix;
        }

        classes.Add(new DesignerClass { id = id });
        return id;
    }

    public DesignerClass FindClass(string classId)
    {
        for (int i = 0; i < classes.Count; i++)
        {
            if (classes[i].id == classId) return classes[i];
        }

        return null;
    }

    public void PruneClasses()
    {
        var used = new HashSet<string>();
        for (int i = 0; i < edges.Count; i++) used.Add(edges[i].classId);

        for (int i = classes.Count - 1; i >= 0; i--)
        {
            if (!used.Contains(classes[i].id)) classes.RemoveAt(i);
        }
    }

    public void MergeClasses(string sourceId, string targetId)
    {
        if (sourceId == targetId) return;

        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].classId == sourceId) edges[i].classId = targetId;
        }

        DesignerClass source = FindClass(sourceId);
        if (source != null) classes.Remove(source);
    }

    public int EdgeCountOf(string classId)
    {
        int count = 0;
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].classId == classId) count++;
        }

        return count;
    }

    public HashSet<string> FollowOf(string cardId)
    {
        var set = new HashSet<string>();
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].toId == cardId) set.Add(edges[i].classId);
        }

        return set;
    }

    public HashSet<string> LeadOf(string cardId)
    {
        var set = new HashSet<string>();
        for (int i = 0; i < edges.Count; i++)
        {
            if (edges[i].fromId == cardId) set.Add(edges[i].classId);
        }

        return set;
    }

    static string Key(string from, string to)
    {
        return from + ">" + to;
    }

    /// <summary>转成求解器视图：集合由边表导出，固定卡取其在网格上的位置。</summary>
    public SolverSituation BuildSolverSituation()
    {
        EnsureShape();

        var solver = new SolverSituation
        {
            rows = rows,
            cols = cols
        };

        solver.rowDirections.AddRange(rowDirections);
        solver.colDirections.AddRange(colDirections);

        var indexOf = new Dictionary<string, int>();

        for (int i = 0; i < cards.Count; i++)
        {
            DesignerCard card = cards[i];
            indexOf[card.id] = i;
            solver.cards.Add(new SolverCard
            {
                id = card.id,
                follow = new List<string>(FollowOf(card.id)),
                lead = new List<string>(LeadOf(card.id))
            });
        }

        for (int i = 0; i < cards.Count; i++)
        {
            DesignerCard card = cards[i];
            if (!card.isFixed) continue;

            solver.fixedCards.Add(new SolverPlacement
            {
                cardIndex = indexOf[card.id],
                row = card.row,
                col = card.col
            });
        }

        return solver;
    }

    public DesignerSituation Clone(string newId)
    {
        string json = UnityEngine.JsonUtility.ToJson(this);
        DesignerSituation copy = UnityEngine.JsonUtility.FromJson<DesignerSituation>(json);
        copy.id = newId;
        copy.name = newId;
        copy.EnsureShape();
        return copy;
    }
}

/// <summary>设计器的工作区：全部情况与关卡表。</summary>
[Serializable]
public class DesignerWorkspace
{
    public List<DesignerSituation> situations = new List<DesignerSituation>();
    public List<string> levelOrder = new List<string>();

    public DesignerSituation FindSituation(string situationId)
    {
        if (situations == null) return null;

        for (int i = 0; i < situations.Count; i++)
        {
            if (situations[i].id == situationId) return situations[i];
        }

        return null;
    }
}

/// <summary>交给求解器的只读视图：卡、导出后的集合、固定位置、规则与方向。</summary>
[Serializable]
public class SolverCard
{
    public string id;
    public List<string> follow = new List<string>();
    public List<string> lead = new List<string>();
}

[Serializable]
public class SolverPlacement
{
    public int cardIndex;
    public int row;
    public int col;
}

[Serializable]
public class SolverSituation
{
    public int rows = 12;
    public int cols = 12;
    public List<SolverCard> cards = new List<SolverCard>();
    public List<SolverPlacement> fixedCards = new List<SolverPlacement>();
    public List<int> rowDirections = new List<int>();
    public List<int> colDirections = new List<int>();
}
