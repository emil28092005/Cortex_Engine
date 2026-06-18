using System.Numerics;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Graphics.Loaders;
/// <summary>
/// Minimal OBJ loader.
/// </summary>
public static class ObjLoader
{
    public static Mesh Load(string path, Vector3? defaultColor = null)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"OBJ file not found: {path}", path);

        var color = defaultColor ?? new Vector3(0.7f, 0.6f, 0.5f);
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var texcoords = new List<Vector2>();
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        var faceNormals = new List<Vector3>();

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            switch (parts[0])
            {
                case "v":
                    positions.Add(ParseVector3(parts));
                    break;
                case "vn":
                    normals.Add(ParseVector3(parts));
                    break;
                case "vt":
                    texcoords.Add(ParseVector2(parts));
                    break;
                case "f":
                    ParseFace(parts, positions, normals, texcoords, color, vertices, indices, faceNormals);
                    break;
            }
        }

        if (vertices.Count == 0)
            throw new InvalidOperationException($"OBJ file contains no geometry: {path}");

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    private static void ParseFace(
        string[] parts,
        List<Vector3> positions,
        List<Vector3> normals,
        List<Vector2> texcoords,
        Vector3 color,
        List<Vertex> vertices,
        List<uint> indices,
        List<Vector3> faceNormals)
    {
        var faceIndices = new List<uint>();
        faceNormals.Clear();

        var hasNormals = false;

        for (int i = 1; i < parts.Length; i++)
        {
            var sub = parts[i].Split('/');
            var posIndex = int.Parse(sub[0]) - 1;
            var pos = positions[posIndex];

            Vector3 normal = Vector3.UnitY;
            if (sub.Length > 2 && !string.IsNullOrEmpty(sub[2]))
            {
                normal = normals[int.Parse(sub[2]) - 1];
                hasNormals = true;
            }

            vertices.Add(new Vertex(pos, color, normal));
            faceIndices.Add((uint)(vertices.Count - 1));
        }

        if (!hasNormals && faceIndices.Count >= 3)
        {
            var a = vertices[(int)faceIndices[0]].Position;
            var b = vertices[(int)faceIndices[1]].Position;
            var c = vertices[(int)faceIndices[2]].Position;
            var faceNormal = MeshMath.ComputeFaceNormal(a, b, c);
            for (int i = 0; i < faceIndices.Count; i++)
            {
                var v = vertices[(int)faceIndices[i]];
                v.Normal = faceNormal;
                vertices[(int)faceIndices[i]] = v;
            }
        }

        for (int i = 2; i < faceIndices.Count; i++)
        {
            indices.Add(faceIndices[0]);
            indices.Add(faceIndices[i - 1]);
            indices.Add(faceIndices[i]);
        }
    }

    private static Vector3 ParseVector3(string[] parts)
    {
        return new Vector3(
            float.Parse(parts[1]),
            float.Parse(parts[2]),
            float.Parse(parts[3]));
    }

    private static Vector2 ParseVector2(string[] parts)
    {
        return new Vector2(
            float.Parse(parts[1]),
            parts.Length > 2 ? float.Parse(parts[2]) : 0);
    }
}
