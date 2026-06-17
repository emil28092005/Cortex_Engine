using System.Numerics;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Graphics;

/// <summary>
/// Procedural mesh generators.
/// </summary>
public static class ProceduralMesh
{
    public static Mesh CreateCube(float size, Vector3 color)
    {
        var s = size * 0.5f;
        var vertices = new[]
        {
            // Front
            new Vertex(new Vector3(-s, -s,  s), color, new Vector3(0, 0, 1)),
            new Vertex(new Vector3( s, -s,  s), color, new Vector3(0, 0, 1)),
            new Vertex(new Vector3( s,  s,  s), color, new Vector3(0, 0, 1)),
            new Vertex(new Vector3(-s,  s,  s), color, new Vector3(0, 0, 1)),
            // Back
            new Vertex(new Vector3( s, -s, -s), color, new Vector3(0, 0, -1)),
            new Vertex(new Vector3(-s, -s, -s), color, new Vector3(0, 0, -1)),
            new Vertex(new Vector3(-s,  s, -s), color, new Vector3(0, 0, -1)),
            new Vertex(new Vector3( s,  s, -s), color, new Vector3(0, 0, -1)),
            // Top
            new Vertex(new Vector3(-s,  s,  s), color, new Vector3(0, 1, 0)),
            new Vertex(new Vector3( s,  s,  s), color, new Vector3(0, 1, 0)),
            new Vertex(new Vector3( s,  s, -s), color, new Vector3(0, 1, 0)),
            new Vertex(new Vector3(-s,  s, -s), color, new Vector3(0, 1, 0)),
            // Bottom
            new Vertex(new Vector3(-s, -s, -s), color, new Vector3(0, -1, 0)),
            new Vertex(new Vector3( s, -s, -s), color, new Vector3(0, -1, 0)),
            new Vertex(new Vector3( s, -s,  s), color, new Vector3(0, -1, 0)),
            new Vertex(new Vector3(-s, -s,  s), color, new Vector3(0, -1, 0)),
            // Right
            new Vertex(new Vector3( s, -s,  s), color, new Vector3(1, 0, 0)),
            new Vertex(new Vector3( s, -s, -s), color, new Vector3(1, 0, 0)),
            new Vertex(new Vector3( s,  s, -s), color, new Vector3(1, 0, 0)),
            new Vertex(new Vector3( s,  s,  s), color, new Vector3(1, 0, 0)),
            // Left
            new Vertex(new Vector3(-s, -s, -s), color, new Vector3(-1, 0, 0)),
            new Vertex(new Vector3(-s, -s,  s), color, new Vector3(-1, 0, 0)),
            new Vertex(new Vector3(-s,  s,  s), color, new Vector3(-1, 0, 0)),
            new Vertex(new Vector3(-s,  s, -s), color, new Vector3(-1, 0, 0)),
        };

        var indices = new uint[]
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23,
        };

        return new Mesh(vertices, indices);
    }

    public static Mesh CreateSphere(float radius, int sectors, int stacks, Vector3 color)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        for (int i = 0; i <= stacks; i++)
        {
            var stackAngle = MathF.PI / 2 - i * MathF.PI / stacks;
            var xy = radius * MathF.Cos(stackAngle);
            var z = radius * MathF.Sin(stackAngle);

            for (int j = 0; j <= sectors; j++)
            {
                var sectorAngle = j * 2 * MathF.PI / sectors;
                var x = xy * MathF.Cos(sectorAngle);
                var y = xy * MathF.Sin(sectorAngle);
                var pos = new Vector3(x, y, z);
                var normal = Vector3.Normalize(pos);
                vertices.Add(new Vertex(pos, color, normal));
            }
        }

        for (int i = 0; i < stacks; i++)
        {
            var k1 = (uint)(i * (sectors + 1));
            var k2 = (uint)(k1 + sectors + 1);

            for (int j = 0; j < sectors; j++, k1++, k2++)
            {
                if (i != 0)
                {
                    indices.Add(k1);
                    indices.Add(k2);
                    indices.Add(k1 + 1);
                }

                if (i != stacks - 1)
                {
                    indices.Add(k1 + 1);
                    indices.Add(k2);
                    indices.Add(k2 + 1);
                }
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    public static Mesh CreateGrid(int lines, float spacing, Vector3 color)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        var max = lines * spacing;
        var normal = Vector3.UnitY;

        for (int i = -lines; i <= lines; i++)
        {
            var pos = i * spacing;

            vertices.Add(new Vertex(new Vector3(pos, 0, -max), color, normal));
            vertices.Add(new Vertex(new Vector3(pos, 0,  max), color, normal));
            indices.Add((uint)(vertices.Count - 2));
            indices.Add((uint)(vertices.Count - 1));

            vertices.Add(new Vertex(new Vector3(-max, 0, pos), color, normal));
            vertices.Add(new Vertex(new Vector3( max, 0, pos), color, normal));
            indices.Add((uint)(vertices.Count - 2));
            indices.Add((uint)(vertices.Count - 1));
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }
}
