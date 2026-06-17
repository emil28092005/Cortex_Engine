using System.Numerics;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Graphics;

public static class ProceduralMesh
{
    public static Mesh CreateSphere(float radius, int slices, int stacks, Vector3 color)
    {
        var vertices = new Vertex[(stacks + 1) * (slices + 1)];
        var indices = new uint[stacks * slices * 6];

        var vi = 0;
        for (var i = 0; i <= stacks; i++)
        {
            var phi = MathF.PI * i / stacks;
            var y = radius * MathF.Cos(phi);
            var r = radius * MathF.Sin(phi);

            for (var j = 0; j <= slices; j++)
            {
                var theta = 2.0f * MathF.PI * j / slices;
                var x = r * MathF.Cos(theta);
                var z = r * MathF.Sin(theta);

                var pos = new Vector3(x, y, z);
                var normal = Vector3.Normalize(pos);

                vertices[vi++] = new Vertex(pos, color, normal);
            }
        }

        var ii = 0;
        for (var i = 0; i < stacks; i++)
        {
            for (var j = 0; j < slices; j++)
            {
                var a = (uint)(i * (slices + 1) + j);
                var b = a + 1;
                var c = a + (uint)(slices + 1);
                var d = c + 1;

                indices[ii++] = a; indices[ii++] = c; indices[ii++] = b;
                indices[ii++] = b; indices[ii++] = c; indices[ii++] = d;
            }
        }

        return new Mesh(vertices, indices);
    }

    public static Mesh CreateGrid(int halfSize, float spacing, Vector3 color)
    {
        var lines = 2 * halfSize + 1;
        var vertices = new List<Vertex>(lines * 4 * 2);
        var indices = new List<uint>(lines * 4 * 2);
        var extent = halfSize * spacing;

        for (var i = -halfSize; i <= halfSize; i++)
        {
            var pos = i * spacing;

            var i0 = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(pos, 0, -extent), color, Vector3.UnitY));
            var i1 = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(pos, 0, extent), color, Vector3.UnitY));
            indices.Add(i0); indices.Add(i1);

            var i2 = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(pos, 0, -extent), color, Vector3.UnitY));
            var i3 = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(pos, 0, extent), color, Vector3.UnitY));
            indices.Add(i2); indices.Add(i3);

            var i4 = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(-extent, 0, pos), color, Vector3.UnitY));
            var i5 = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(extent, 0, pos), color, Vector3.UnitY));
            indices.Add(i4); indices.Add(i5);

            var i6 = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(-extent, 0, pos), color, Vector3.UnitY));
            var i7 = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(extent, 0, pos), color, Vector3.UnitY));
            indices.Add(i6); indices.Add(i7);
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }
}
