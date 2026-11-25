using System.Collections.Generic;
using UnityEngine;

/// <summary>世界空间的网格：一个网格里画出所有格子与格线，不生成格子对象。</summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class GridMesh : MonoBehaviour
{
    [SerializeField, Tooltip("格线颜色。")]
    Color _lineColor;

    [SerializeField, Tooltip("格子底色。")]
    Color _cellColor;

    [SerializeField, Tooltip("格线宽度（世界单位）。")]
    float _lineWidth;

    [SerializeField, Tooltip("格子的世界尺寸。")]
    Vector2 _cellSize;

    Mesh _mesh;
    int _rows;
    int _cols;

    public int Rows => _rows;
    public int Cols => _cols;
    public Vector2 CellSize => _cellSize;

    void Awake()
    {
        EnsureMesh();
    }

    void EnsureMesh()
    {
        if (_mesh != null) return;

        _mesh = new Mesh { name = "Grid" };
        _mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        var renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 0;
    }

    public void Build(int rows, int cols)
    {
        EnsureMesh();

        _rows = Mathf.Max(1, rows);
        _cols = Mathf.Max(1, cols);

        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        float width = _cols * _cellSize.x;
        float height = _rows * _cellSize.y;
        float xMin = -width * 0.5f;
        float yMax = height * 0.5f;

        AddQuad(vertices, colors, triangles, xMin, yMax, width, height, _cellColor);

        for (int row = 0; row <= _rows; row++)
        {
            float y = yMax - row * _cellSize.y;
            AddQuad(vertices, colors, triangles, xMin, y, width, _lineWidth, _lineColor);
        }

        for (int col = 0; col <= _cols; col++)
        {
            float x = xMin + col * _cellSize.x;
            AddQuad(vertices, colors, triangles, x, yMax, _lineWidth, height, _lineColor);
        }

        _mesh.Clear();
        _mesh.SetVertices(vertices);
        _mesh.SetColors(colors);
        _mesh.SetTriangles(triangles, 0);
        _mesh.RecalculateBounds();
    }

    static void AddQuad(List<Vector3> vertices, List<Color> colors, List<int> triangles,
        float x, float yTop, float width, float height, Color color)
    {
        int start = vertices.Count;

        vertices.Add(new Vector3(x, yTop - height, 0f));
        vertices.Add(new Vector3(x, yTop, 0f));
        vertices.Add(new Vector3(x + width, yTop, 0f));
        vertices.Add(new Vector3(x + width, yTop - height, 0f));

        for (int i = 0; i < 4; i++) colors.Add(color);

        triangles.Add(start);
        triangles.Add(start + 1);
        triangles.Add(start + 2);
        triangles.Add(start);
        triangles.Add(start + 2);
        triangles.Add(start + 3);
    }

    /// <summary>格中心的世界坐标。</summary>
    public Vector3 CellCenter(int row, int col)
    {
        float width = _cols * _cellSize.x;
        float height = _rows * _cellSize.y;

        float x = -width * 0.5f + (col + 0.5f) * _cellSize.x;
        float y = height * 0.5f - (row + 0.5f) * _cellSize.y;

        return transform.TransformPoint(new Vector3(x, y, 0f));
    }

    /// <summary>世界坐标落在哪一格；在网格外返回 false。</summary>
    public bool TryGetCell(Vector3 worldPoint, out int row, out int col)
    {
        row = -1;
        col = -1;

        Vector3 local = transform.InverseTransformPoint(worldPoint);

        float width = _cols * _cellSize.x;
        float height = _rows * _cellSize.y;

        if (local.x < -width * 0.5f || local.x > width * 0.5f) return false;
        if (local.y < -height * 0.5f || local.y > height * 0.5f) return false;

        col = Mathf.Clamp(Mathf.FloorToInt((local.x + width * 0.5f) / _cellSize.x), 0, _cols - 1);
        row = Mathf.Clamp(Mathf.FloorToInt((height * 0.5f - local.y) / _cellSize.y), 0, _rows - 1);
        return true;
    }
}
