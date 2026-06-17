using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Graphics.Loaders;

/// <summary>
/// Minimal .obj loader.
/// Supports vertices (v) and faces (f). Creates per-face normals for flat shading.
/// Produces a colored Mesh component.
/// </summary>
public static class ObjLoader
{
    public static Mesh Load(string path, Vector3? defaultColor = null)
    {
        var color = defaultColor ?? new Vector3(0.7f, 0.7f, 0.7f);

        var positions = new List<Vector3>();
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                continue;

            switch (parts[0])
            {
                case "v" when parts.Length >= 4:
                    positions.Add(new Vector3(
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])));
                    break;

                case "f" when parts.Length >= 4:
                    // Triangulate the face as a fan.
                    var baseIndex = ParseFaceIndex(parts[1]);
                    for (var i = 2; i < parts.Length - 1; i++)
                    {
                        var i0 = baseIndex;
                        var i1 = ParseFaceIndex(parts[i]);
                        var i2 = ParseFaceIndex(parts[i + 1]);

                        var v0 = positions[(int)i0];
                        var v1 = positions[(int)i1];
                        var v2 = positions[(int)i2];
                        var normal = ComputeFaceNormal(v0, v1, v2);

                        var vertexBase = (uint)vertices.Count;
                        indices.Add(vertexBase);
                        indices.Add(vertexBase + 1);
                        indices.Add(vertexBase + 2);

                        vertices.Add(new Vertex(v0, color, normal));
                        vertices.Add(new Vertex(v1, color, normal));
                        vertices.Add(new Vertex(v2, color, normal));
                    }
                    break;
            }
        }

        if (vertices.Count == 0)
            throw new InvalidOperationException($"OBJ file has no vertices: {path}");

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    private static uint ParseFaceIndex(string part)
    {
        // Formats: v, v/vt, v/vt/vn, v//vn
        var slashIndex = part.IndexOf('/');
        var indexStr = slashIndex == -1 ? part : part.Substring(0, slashIndex);
        var index = int.Parse(indexStr);
        return (uint)(index - 1); // OBJ indices are 1-based
    }

    private static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
        => MeshMath.ComputeFaceNormal(a, b, c);
}
