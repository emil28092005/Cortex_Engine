using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Graphics.Loaders;

/// <summary>
/// Minimal .obj loader.
/// Supports vertices (v) and faces (f). Ignores normals/UVs for now.
/// Produces a colored Mesh component.
/// </summary>
public static class ObjLoader
{
    public static Mesh Load(string path, Vector3? defaultColor = null)
    {
        var color = defaultColor ?? new Vector3(0.7f, 0.7f, 0.7f);

        var positions = new List<Vector3>();
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
                    // Triangulate the face as a fan. Only the position index is used.
                    var baseIndex = ParseFaceIndex(parts[1]);
                    for (var i = 2; i < parts.Length - 1; i++)
                    {
                        indices.Add(baseIndex);
                        indices.Add(ParseFaceIndex(parts[i]));
                        indices.Add(ParseFaceIndex(parts[i + 1]));
                    }
                    break;
            }
        }

        if (positions.Count == 0)
            throw new InvalidOperationException($"OBJ file has no vertices: {path}");

        var vertices = new Vertex[positions.Count];
        for (var i = 0; i < positions.Count; i++)
        {
            vertices[i] = new Vertex(positions[i], color);
        }

        return new Mesh(vertices, indices.ToArray());
    }

    private static uint ParseFaceIndex(string part)
    {
        // Formats: v, v/vt, v/vt/vn, v//vn
        var slashIndex = part.IndexOf('/');
        var indexStr = slashIndex == -1 ? part : part.Substring(0, slashIndex);
        var index = int.Parse(indexStr);
        return (uint)(index - 1); // OBJ indices are 1-based
    }
}
