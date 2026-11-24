using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/// <summary>
/// 情况统计：枚举满足固定卡、方向与接缝约束的全部摆法，按拓扑去重后计数。
/// 坐标只用于推导邻接，判定不依赖坐标。
/// </summary>
public static class DesignerSolver
{
    public sealed class Result
    {
        public bool ok;
        public string message = string.Empty;
        public bool truncated;
        public int nodes;
        public double milliseconds;
        public int placementCount;
        public int topologyCount;
        public bool topologyCountCapped;
        public List<string> topologies = new List<string>();
        public List<int> cardDomains = new List<int>();
    }

    const int MaxTopologiesKept = 200;

    public static Result Test(SolverSituation situation, int maxNodes = 2000000, double maxSeconds = 5.0)
    {
        Result result = new Result();

        if (situation == null)
        {
            result.message = "情况为空";
            return result;
        }

        int rows = situation.rows;
        int cols = situation.cols;
        int cellCount = rows * cols;
        int cardCount = situation.cards.Count;

        if (cardCount == 0)
        {
            result.message = "没有卡";
            return result;
        }

        if (cardCount > cellCount)
        {
            result.message = "卡数超过格子数";
            return result;
        }

        var ids = new Dictionary<string, int>();
        for (int i = 0; i < cardCount; i++)
        {
            string cardId = string.IsNullOrEmpty(situation.cards[i].id) ? "card" + i : situation.cards[i].id;

            if (ids.ContainsKey(cardId))
            {
                result.message = "卡 ID 重复：" + cardId;
                return result;
            }

            ids.Add(cardId, i);
        }

        int[] cellCard = new int[cellCount];
        for (int i = 0; i < cellCount; i++) cellCard[i] = -1;

        bool[] placed = new bool[cardCount];

        for (int i = 0; i < situation.fixedCards.Count; i++)
        {
            SolverPlacement fixedCard = situation.fixedCards[i];
            if (fixedCard == null) continue;

            int cardIndex = fixedCard.cardIndex;
            if (cardIndex < 0 || cardIndex >= cardCount)
            {
                result.message = "固定卡索引非法";
                return result;
            }

            if (fixedCard.row < 0 || fixedCard.row >= rows || fixedCard.col < 0 || fixedCard.col >= cols)
            {
                result.message = "固定卡坐标越界";
                return result;
            }

            int cell = fixedCard.row * cols + fixedCard.col;
            if (cellCard[cell] != -1 || placed[cardIndex])
            {
                result.message = "固定卡冲突：" + situation.cards[cardIndex].id;
                return result;
            }

            cellCard[cell] = cardIndex;
            placed[cardIndex] = true;
        }

        for (int card = 0; card < cardCount; card++)
        {
            result.cardDomains.Add(placed[card] ? 1 : LegalCells(card, cellCard, situation).Count);
        }

        Stopwatch watch = Stopwatch.StartNew();
        var topologies = new HashSet<string>();
        int[] nodeCount = { 0 };
        bool[] truncated = { false };
        bool[] capped = { false };
        int[] placements = { 0 };

        Search(situation, cellCard, placed, watch, maxNodes, maxSeconds,
            nodeCount, truncated, topologies, capped, placements);

        result.ok = true;
        result.nodes = nodeCount[0];
        result.milliseconds = watch.Elapsed.TotalMilliseconds;
        result.truncated = truncated[0];
        result.placementCount = placements[0];
        result.topologyCount = topologies.Count;
        result.topologyCountCapped = capped[0];
        result.topologies.AddRange(topologies);
        result.message = truncated[0] ? "已达预算上限，结果不完整" : "枚举完成";

        return result;
    }

    static void Search(SolverSituation situation, int[] cellCard, bool[] placed,
        Stopwatch watch, int maxNodes, double maxSeconds, int[] nodeCount, bool[] truncated,
        HashSet<string> topologies, bool[] capped, int[] placements)
    {
        if (truncated[0]) return;

        nodeCount[0]++;
        if (nodeCount[0] > maxNodes || watch.Elapsed.TotalSeconds > maxSeconds)
        {
            truncated[0] = true;
            return;
        }

        int bestCard = -1;
        List<int> bestCells = null;

        for (int card = 0; card < placed.Length; card++)
        {
            if (placed[card]) continue;

            List<int> cells = LegalCells(card, cellCard, situation);

            // 暂时无处可放的卡先跳过：放别的卡会扩展可放置区域，它稍后可能变得可放。
            if (cells.Count == 0) continue;

            if (bestCells == null || cells.Count < bestCells.Count)
            {
                bestCard = card;
                bestCells = cells;
            }
        }

        if (bestCard < 0)
        {
            // 还有卡没放下（只是暂时无处可放）时不能算完成。
            for (int card = 0; card < placed.Length; card++)
            {
                if (!placed[card]) return;
            }

            if (!IsConnected(cellCard, situation.rows, situation.cols)) return;

            placements[0]++;

            string key = TopologyKey(cellCard, situation);
            if (topologies.Add(key) && topologies.Count > MaxTopologiesKept) capped[0] = true;
            return;
        }

        if (bestCells == null || bestCells.Count == 0) return;

        for (int i = 0; i < bestCells.Count; i++)
        {
            int cell = bestCells[i];
            cellCard[cell] = bestCard;
            placed[bestCard] = true;

            Search(situation, cellCard, placed, watch, maxNodes, maxSeconds,
                nodeCount, truncated, topologies, capped, placements);

            placed[bestCard] = false;
            cellCard[cell] = -1;

            if (truncated[0]) return;
        }
    }

    // 只在"已放卡的相邻空格"里找候选，保证每一步都沿连通方向生长，避免枚举大量散落摆法。
    static List<int> LegalCells(int card, int[] cellCard, SolverSituation situation)
    {
        List<int> candidates = Candidates(cellCard, situation);
        var cells = new List<int>();

        for (int i = 0; i < candidates.Count; i++)
        {
            int cell = candidates[i];
            int row = cell / situation.cols;
            int col = cell % situation.cols;

            if (IsLegalAt(card, row, col, cellCard, situation)) cells.Add(cell);
        }

        return cells;
    }

    static List<int> Candidates(int[] cellCard, SolverSituation situation)
    {
        int rows = situation.rows;
        int cols = situation.cols;
        bool anyPlaced = false;

        for (int cell = 0; cell < cellCard.Length; cell++)
        {
            if (cellCard[cell] < 0) continue;
            anyPlaced = true;
            break;
        }

        if (!anyPlaced)
        {
            var all = new List<int>(cellCard.Length);
            for (int cell = 0; cell < cellCard.Length; cell++) all.Add(cell);
            return all;
        }

        var frontier = new List<int>();
        var seen = new bool[cellCard.Length];

        for (int cell = 0; cell < cellCard.Length; cell++)
        {
            if (cellCard[cell] < 0) continue;

            int row = cell / cols;
            int col = cell % cols;

            PushFrontier(cellCard, seen, frontier, rows, cols, row - 1, col);
            PushFrontier(cellCard, seen, frontier, rows, cols, row + 1, col);
            PushFrontier(cellCard, seen, frontier, rows, cols, row, col - 1);
            PushFrontier(cellCard, seen, frontier, rows, cols, row, col + 1);
        }

        return frontier;
    }

    static void PushFrontier(int[] cellCard, bool[] seen, List<int> frontier, int rows, int cols, int row, int col)
    {
        if (row < 0 || row >= rows || col < 0 || col >= cols) return;

        int cell = row * cols + col;
        if (cellCard[cell] >= 0 || seen[cell]) return;

        seen[cell] = true;
        frontier.Add(cell);
    }
    static bool IsLegalAt(int card, int row, int col, int[] cellCard, SolverSituation situation)
    {
        int rows = situation.rows;
        int cols = situation.cols;
        bool rowForward = situation.rowDirections[row] >= 0;
        bool colForward = situation.colDirections[col] >= 0;

        if (col - 1 >= 0)
        {
            int other = cellCard[row * cols + col - 1];
            if (other >= 0 && !OrderedAdjacency(card, other, rowForward, situation)) return false;
        }

        if (col + 1 < cols)
        {
            int other = cellCard[row * cols + col + 1];
            if (other >= 0 && !OrderedAdjacency(card, other, !rowForward, situation)) return false;
        }

        if (row - 1 >= 0)
        {
            int other = cellCard[(row - 1) * cols + col];
            if (other >= 0 && !OrderedAdjacency(card, other, colForward, situation)) return false;
        }

        if (row + 1 < rows)
        {
            int other = cellCard[(row + 1) * cols + col];
            if (other >= 0 && !OrderedAdjacency(card, other, !colForward, situation)) return false;
        }

        return true;
    }

    /// <param name="cardIsAfter">true 表示 card 排在 other 之后（other 是前驱）。</param>
    static bool OrderedAdjacency(int card, int other, bool cardIsAfter, SolverSituation situation)
    {
        int before = cardIsAfter ? other : card;
        int after = cardIsAfter ? card : other;

        SolverCard beforeCard = situation.cards[before];
        SolverCard afterCard = situation.cards[after];

        if (beforeCard.lead == null || afterCard.follow == null) return false;

        for (int i = 0; i < beforeCard.lead.Count; i++)
        {
            for (int j = 0; j < afterCard.follow.Count; j++)
            {
                if (beforeCard.lead[i] == afterCard.follow[j]) return true;
            }
        }

        return false;
    }

    static bool IsConnected(int[] cellCard, int rows, int cols)
    {
        int total = 0;
        int first = -1;

        for (int cell = 0; cell < cellCard.Length; cell++)
        {
            if (cellCard[cell] < 0) continue;

            total++;
            if (first < 0) first = cellCard[cell];
        }

        if (total <= 1) return true;

        var visited = new HashSet<int> { first };
        var queue = new Queue<int>();
        queue.Enqueue(first);

        while (queue.Count > 0)
        {
            int card = queue.Dequeue();

            for (int cell = 0; cell < cellCard.Length; cell++)
            {
                if (cellCard[cell] != card) continue;

                int row = cell / cols;
                int col = cell % cols;

                PushCell(cellCard, rows, cols, row, col - 1, visited, queue);
                PushCell(cellCard, rows, cols, row, col + 1, visited, queue);
                PushCell(cellCard, rows, cols, row - 1, col, visited, queue);
                PushCell(cellCard, rows, cols, row + 1, col, visited, queue);
            }
        }

        return visited.Count == total;
    }

    static void PushCell(int[] cellCard, int rows, int cols, int row, int col, HashSet<int> visited, Queue<int> queue)
    {
        if (row < 0 || row >= rows || col < 0 || col >= cols) return;

        int card = cellCard[row * cols + col];
        if (card < 0 || visited.Contains(card)) return;

        visited.Add(card);
        queue.Enqueue(card);
    }

    static string TopologyKey(int[] cellCard, SolverSituation situation)
    {
        int rows = situation.rows;
        int cols = situation.cols;
        var pairs = new List<string>();

        for (int row = 0; row < rows; row++)
        {
            bool rowForward = situation.rowDirections[row] >= 0;

            for (int col = 0; col < cols; col++)
            {
                int card = cellCard[row * cols + col];
                if (card < 0) continue;

                if (col + 1 < cols)
                {
                    int right = cellCard[row * cols + col + 1];
                    if (right >= 0) AddPair(pairs, card, right, rowForward, situation);
                }

                if (row + 1 < rows)
                {
                    int below = cellCard[(row + 1) * cols + col];
                    if (below >= 0)
                    {
                        bool colForward = situation.colDirections[col] >= 0;
                        AddPair(pairs, card, below, colForward, situation);
                    }
                }
            }
        }

        pairs.Sort();
        var builder = new StringBuilder();
        for (int i = 0; i < pairs.Count; i++) builder.Append(pairs[i]).Append(';');
        return builder.ToString();
    }

    static void AddPair(List<string> pairs, int card, int other, bool forward, SolverSituation situation)
    {
        string before = forward ? situation.cards[card].id : situation.cards[other].id;
        string after = forward ? situation.cards[other].id : situation.cards[card].id;
        pairs.Add(before + ">" + after);
    }
}
