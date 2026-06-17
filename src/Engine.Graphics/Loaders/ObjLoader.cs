using System.Globalization;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Graphics.Loaders;

public static class ObjLoader
{
    private static readonly Vector3 DefaultColor = new(0.7f, 0.6f, 0.5f);

    public static Mesh Load(string path, Vector3? color = null)
    {
        var tint = color ?? DefaultColor;
        var lines = File.ReadAllLines(path);

        var positions = new List<Vector3>();
        var normals = new List<Vector3>();

        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            switch (parts[0])
            {
                case "v":
                    positions.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)));
                    break;

                case "vn":
                    normals.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)));
                    break;

                case "f":
                    ParseFace(parts, positions, normals, vertices, indices, tint);
                    break;
            }
        }

        if (vertices.Count == 0)
            throw new InvalidOperationException($"OBJ file '{path}' contains no faces.");

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    private static void ParseFace(
        string[] parts,
        List<Vector3> positions,
        List<Vector3> normals,
        List<Vertex> vertices,
        List<uint> indices,
        Vector3 tint)
    {
        var faceData = new List<(int posIdx, int normIdx)>();

        for (var i = 1; i < parts.Length; i++)
        {
            var vertexData = parts[i].Split('/');
            var posIdx = int.Parse(vertexData[0]) - 1;
            var normIdx = vertexData.Length > 2 && !string.IsNullOrEmpty(vertexData[2])
                ? int.Parse(vertexData[2]) - 1
                : -1;

            faceData.Add((posIdx, normIdx));
        }

        if (faceData.Count < 3) return;

        for (var i = 1; i < faceData.Count - 1; i++)
        {
            var d0 = faceData[0];
            var d1 = faceData[i];
            var d2 = faceData[i + 1];

            var p0 = positions[d0.posIdx];
            var p1 = positions[d1.posIdx];
            var p2 = positions[d2.posIdx];

            var normal = d0.normIdx >= 0 && d0.normIdx < normals.Count
                ? normals[d0.normIdx]
                : MeshMath.ComputeFaceNormal(p0, p1, p2);

            var i0 = (uint)vertices.Count;
            vertices.Add(new Vertex(p0, tint, normal));
            var i1 = (uint)vertices.Count;
            vertices.Add(new Vertex(p1, tint, normal));
            var i2 = (uint)vertices.Count;
            vertices.Add(new Vertex(p2, tint, normal));

            indices.Add(i0); indices.Add(i1); indices.Add(i2);
        }
    }
}
