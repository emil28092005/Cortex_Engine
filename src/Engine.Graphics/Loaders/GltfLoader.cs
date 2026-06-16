using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using SharpGLTF.Schema2;

namespace Engine.Graphics.Loaders;

/// <summary>
/// glTF/glTF binary loader using SharpGLTF.Core.
/// Loads the first primitive of the first mesh and converts it to a colored Mesh component.
/// Creates per-face normals for flat shading if the glTF does not provide normals.
/// </summary>
public static class GltfLoader
{
    public static Engine.Core.Components.Mesh Load(string path, Vector3? defaultColor = null)
    {
        var color = defaultColor ?? new Vector3(0.7f, 0.7f, 0.7f);

        var model = ModelRoot.Load(path);
        if (model.LogicalMeshes.Count == 0)
            throw new InvalidOperationException($"glTF file has no meshes: {path}");

        var mesh = model.LogicalMeshes[0];
        if (mesh.Primitives.Count == 0)
            throw new InvalidOperationException($"glTF mesh has no primitives: {path}");

        var primitive = mesh.Primitives[0];

        if (!primitive.VertexAccessors.TryGetValue("POSITION", out var positionAccessor))
            throw new InvalidOperationException($"glTF primitive has no POSITION accessor: {path}");

        var positions = positionAccessor.AsVector3Array();

        var indices = GetIndices(primitive, positions.Count);
        var vertices = new List<Vertex>();
        var newIndices = new List<uint>();

        for (var i = 0; i < indices.Length; i += 3)
        {
            var i0 = (int)indices[i];
            var i1 = (int)indices[i + 1];
            var i2 = (int)indices[i + 2];

            var v0 = new Vector3(positions[i0].X, positions[i0].Y, positions[i0].Z);
            var v1 = new Vector3(positions[i1].X, positions[i1].Y, positions[i1].Z);
            var v2 = new Vector3(positions[i2].X, positions[i2].Y, positions[i2].Z);

            var normal = ComputeFaceNormal(v0, v1, v2);

            var vertexBase = (uint)vertices.Count;
            newIndices.Add(vertexBase);
            newIndices.Add(vertexBase + 1);
            newIndices.Add(vertexBase + 2);

            vertices.Add(new Vertex(v0, color, normal));
            vertices.Add(new Vertex(v1, color, normal));
            vertices.Add(new Vertex(v2, color, normal));
        }

        return new Engine.Core.Components.Mesh(vertices.ToArray(), newIndices.ToArray());
    }

    private static uint[] GetIndices(MeshPrimitive primitive, int positionCount)
    {
        if (primitive.IndexAccessor != null)
        {
            var idx = primitive.IndexAccessor.AsIndexArray();
            var indices = new uint[idx.Count];
            for (var i = 0; i < idx.Count; i++)
                indices[i] = idx[i];
            return indices;
        }

        // Non-indexed primitive
        var auto = new uint[positionCount];
        for (var i = 0; i < positionCount; i++)
            auto[i] = (uint)i;
        return auto;
    }

    private static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var ab = b - a;
        var ac = c - a;
        var normal = Vector3.Cross(ab, ac);
        if (normal.LengthSquared() > 0.00001f)
            normal = Vector3.Normalize(normal);
        else
            normal = Vector3.UnitY;
        return normal;
    }
}
