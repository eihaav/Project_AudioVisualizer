using System.Collections.Generic;
using UnityEngine;

public class HexGridMeshGenerator : MonoBehaviour
{
    public Vector2 GridDimensions;
    public float HexRadius, HexHeight, HexOffset;
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = GenerateHexGrid(Mathf.RoundToInt(GridDimensions.x), Mathf.RoundToInt(GridDimensions.y), HexRadius, HexHeight);
        _meshFilter.mesh = _mesh;
    }
    private Vector3 GetHexWorldPos(int q, int r, float radius, float offset)
    {
        float x = radius * (1.5f + HexOffset) * q;
        float z = radius * Mathf.Sqrt(3f) * (r + r * HexOffset + q % 2 * 0.5f);
        return new Vector3(x, 0f, z);
    }
    private void AddHex(Vector3 center, float radius, float height, List<Vector3> vertices, List<int> triangles, List<Vector2> uv2, List<Color> vertexColors)
    {
        int start = vertices.Count;
        Vector2 uvCenter = new Vector2(center.x / GridDimensions.x, center.z / GridDimensions.y);
        Color vertexColor = new Color(uvCenter.x, uvCenter.y, 1f);
        // Top center
        vertices.Add(center + Vector3.up * height);
        uv2.Add(uvCenter);
        vertexColors.Add(vertexColor);


        // Bottom center
        vertices.Add(center);
        uv2.Add(uvCenter);
        vertexColors.Add(vertexColor);

        // Ring vertices
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60 * i);
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            vertices.Add(center + offset + Vector3.up * height);
            uv2.Add(uvCenter);
            vertexColors.Add(vertexColor);

            vertices.Add(center + offset);
            uv2.Add(uvCenter);
            vertexColors.Add(vertexColor);
        }

        // Top
        for (int i = 0; i < 6; i++)
        {
            triangles.Add(start);
            triangles.Add(start + 2 + ((i + 1) % 6) * 2);
            triangles.Add(start + 2 + i * 2);

        }
        for (int i = 0; i < 6; i++)
        {
            triangles.Add(start + 1);
            triangles.Add(start + 2 + i * 2 + 1);
            triangles.Add(start + 2 + ((i + 1) % 6) * 2 + 1);
        }

        // Sides
        for (int i = 0; i < 6; i++)
        {
            int t0 = start + 2 + i * 2;
            int b0 = t0 + 1;
            int t1 = start + 2 + ((i + 1) % 6) * 2;
            int b1 = t1 + 1;

            triangles.Add(t0);
            triangles.Add(t1);
            triangles.Add(b0);


            triangles.Add(t1);
            triangles.Add(b1);
            triangles.Add(b0);

        }
    }
    private Mesh GenerateHexGrid(int width, int height, float radius, float hexHeight)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var uv2 = new List<Vector2>();
        var vertexColors = new List<Color>();

        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                Vector3 center = GetHexWorldPos(q, r, radius, 0f);
                AddHex(center, radius, hexHeight, vertices, triangles, uv2, vertexColors);
            }
        }
        Debug.Log($"Generated hex grid with {vertices.Count} vertices, {vertexColors.Count} vertex colors and {triangles.Count} triangles.");

        Mesh mesh = new Mesh();
        mesh.indexFormat = vertices.Count > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(2, uv2);
        //mesh.SetNormals(CalculateFlatNormals(vertices.ToArray(), triangles.ToArray()));
        //mesh.RecalculateNormals();
        mesh.SetColors(vertexColors);
        //NormalSolver.RecalculateNormals(mesh, 10f);
        mesh.RecalculateBounds();
        mesh.UploadMeshData(false);

        return mesh;
    }
}
