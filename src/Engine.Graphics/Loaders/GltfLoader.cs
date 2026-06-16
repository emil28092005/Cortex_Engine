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
        var vertices = new Vertex[positions.Count];
        for (var i = 0; i < positions.Count; i++)
        {
            vertices[i] = new Vertex(positions[i], color);
        }

        uint[] indices;
        if (primitive.IndexAccessor != null)
        {
            var idx = primitive.IndexAccessor.AsIndexArray();
            indices = new uint[idx.Count];
            for (var i = 0; i < idx.Count; i++)
                indices[i] = idx[i];
        }
        else
        {
            // Non-indexed primitive
            indices = new uint[positions.Count];
            for (var i = 0; i < positions.Count; i++)
                indices[i] = (uint)i;
        }

        return new Engine.Core.Components.Mesh(vertices, indices);
    }
}
