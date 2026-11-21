using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TableGrid : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField, Tooltip("一行的槽位数。")]
    int _columns = 8;

    [SerializeField, Tooltip("槽位的行数。")]
    int _rows = 8;

    [SerializeField, Tooltip("槽位外框的水平尺寸，与帧在桌面上的占地一致。")]
    Vector2 _slotSize = new Vector2(0.64f, 0.36f);

    [SerializeField, Tooltip("相邻槽位之间的留白。")]
    float _padding = 0.06f;

    [SerializeField, Tooltip("外框的线宽。")]
    float _lineWidth = 0.008f;

    [SerializeField, Tooltip("画高亮外框的子物体，需带网格过滤器与渲染器。")]
    MeshFilter _highlightMeshFilter;

    readonly List<Vector3> _vertices = new List<Vector3>();
    readonly List<int> _triangles = new List<int>();
    readonly List<Vector3> _highlightVertices = new List<Vector3>();
    readonly List<int> _highlightTriangles = new List<int>();
    readonly List<int> _highlightedSlots = new List<int>();

    MaterialPropertyBlock _propertyBlock;
    Mesh _mesh;
    Mesh _highlightMesh;
    Color _highlightColor = Color.white;

    public int SlotCount => Mathf.Max(1, _columns) * Mathf.Max(1, _rows);

    private void OnEnable()
    {
        Rebuild();
        RebuildHighlight();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        Rebuild();
        RebuildHighlight();
    }

    private void OnDisable()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter != null) filter.sharedMesh = null;

        if (_highlightMeshFilter != null) _highlightMeshFilter.sharedMesh = null;

        Release(ref _mesh);
        Release(ref _highlightMesh);
    }

    /// <summary>
    /// 把一个槽位加进高亮状态，不动网格。行 0 是最靠后的一排。
    /// 高亮只有一种颜色，后设的颜色会应用到所有高亮槽位；改完调用 RebuildHighlight。
    /// </summary>
    public void AddHighlightCandidate(int row, int col, Color color)
    {
        _highlightColor = color;

        int slot = row * Mathf.Max(1, _columns) + col;
        if (!_highlightedSlots.Contains(slot)) _highlightedSlots.Add(slot);
    }

    /// <summary>只保留一个高亮槽位（先清空再加）并立即重建。</summary>
    public void SetHighlight(int row, int col, Color color)
    {
        ClearHighlights();
        AddHighlightCandidate(row, col, color);
        RebuildHighlight();
    }

    /// <summary>清掉所有高亮状态，不动网格。</summary>
    public void ClearHighlights()
    {
        _highlightedSlots.Clear();
    }

    /// <summary>按槽位参数重建全部外框。</summary>
    void Rebuild()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        if (filter == null) return;

        _vertices.Clear();
        _triangles.Clear();

        SlotLayout layout = BuildLayout();
        for (int slot = 0; slot < layout.SlotCount; slot++)
        {
            AppendSlot(slot, layout, _vertices, _triangles);
        }

        _mesh = Write(_mesh, "TableGrid", _vertices, _triangles);
        filter.sharedMesh = _mesh;
    }

    /// <summary>按当前高亮状态重建高亮网格并写入高亮色。</summary>
    public void RebuildHighlight()
    {
        if (_highlightMeshFilter == null) return;

        _highlightVertices.Clear();
        _highlightTriangles.Clear();

        SlotLayout layout = BuildLayout();
        for (int i = 0; i < _highlightedSlots.Count; i++)
        {
            AppendSlot(_highlightedSlots[i], layout, _highlightVertices, _highlightTriangles);
        }

        _highlightMesh = Write(_highlightMesh, "TableGridHighlight", _highlightVertices, _highlightTriangles);
        _highlightMeshFilter.sharedMesh = _highlightMesh;

        MeshRenderer highlightRenderer = _highlightMeshFilter.GetComponent<MeshRenderer>();
        if (highlightRenderer == null) return;

        if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();
        _propertyBlock.SetColor(BaseColorId, _highlightColor);
        highlightRenderer.SetPropertyBlock(_propertyBlock);
    }

    SlotLayout BuildLayout()
    {
        SlotLayout layout = new SlotLayout
        {
            ColumnCount = Mathf.Max(1, _columns),
            RowCount = Mathf.Max(1, _rows),
            SlotWidth = Mathf.Max(0f, _slotSize.x),
            SlotDepth = Mathf.Max(0f, _slotSize.y)
        };

        layout.LineWidth = Mathf.Clamp(_lineWidth, 0f, Mathf.Min(layout.SlotWidth, layout.SlotDepth) * 0.5f);
        layout.StepX = layout.SlotWidth + _padding;
        layout.StepZ = layout.SlotDepth + _padding;
        return layout;
    }

    static void AppendSlot(int slot, SlotLayout layout, List<Vector3> vertices, List<int> triangles)
    {
        if (slot < 0 || slot >= layout.SlotCount || layout.LineWidth <= 0f) return;

        int column = slot % layout.ColumnCount;
        int row = slot / layout.ColumnCount;
        float x = (column - (layout.ColumnCount - 1) * 0.5f) * layout.StepX;
        float z = ((layout.RowCount - 1) * 0.5f - row) * layout.StepZ;
        float x0 = x - layout.SlotWidth * 0.5f;
        float x1 = x + layout.SlotWidth * 0.5f;
        float z0 = z - layout.SlotDepth * 0.5f;
        float z1 = z + layout.SlotDepth * 0.5f;
        float line = layout.LineWidth;

        AddQuad(x0, x1, z0, z0 + line, vertices, triangles);
        AddQuad(x0, x1, z1 - line, z1, vertices, triangles);
        AddQuad(x0, x0 + line, z0 + line, z1 - line, vertices, triangles);
        AddQuad(x1 - line, x1, z0 + line, z1 - line, vertices, triangles);
    }

    static void AddQuad(float x0, float x1, float z0, float z1, List<Vector3> vertices, List<int> triangles)
    {
        int index = vertices.Count;
        vertices.Add(new Vector3(x0, 0f, z0));
        vertices.Add(new Vector3(x0, 0f, z1));
        vertices.Add(new Vector3(x1, 0f, z1));
        vertices.Add(new Vector3(x1, 0f, z0));
        triangles.Add(index);
        triangles.Add(index + 1);
        triangles.Add(index + 2);
        triangles.Add(index);
        triangles.Add(index + 2);
        triangles.Add(index + 3);
    }

    static Mesh Write(Mesh mesh, string name, List<Vector3> vertices, List<int> triangles)
    {
        if (mesh == null) mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void Release(ref Mesh mesh)
    {
        if (mesh == null) return;

        if (Application.isPlaying) Destroy(mesh);
        else DestroyImmediate(mesh);

        mesh = null;
    }

    struct SlotLayout
    {
        public int ColumnCount;
        public int RowCount;
        public float SlotWidth;
        public float SlotDepth;
        public float StepX;
        public float StepZ;
        public float LineWidth;

        public readonly int SlotCount => ColumnCount * RowCount;
    }
}
