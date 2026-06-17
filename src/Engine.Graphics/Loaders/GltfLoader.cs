using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
namespace Engine.Graphics.Loaders;

public static class GltfLoader
{
    public static Mesh Load(string path, Vector3? color = null)
    {
        var tint = color ?? new Vector3(0.7f, 0.6f, 0.5f);

        var modelRoot = SharpGLTF.Schema2.ModelRoot.Load(path);

        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        foreach (var scene in modelRoot.LogicalScenes)
        {
            foreach (var node in scene.VisualChildren)
            {
                var mesh = node.Mesh;
                if (mesh == null) continue;

                foreach (var primitive in mesh.Primitives)
                {
                    var posAccess = primitive.GetVertexAccessor("POSITION");
                    var normAccess = primitive.GetVertexAccessor("NORMAL");
                    if (posAccess == null) continue;

                    var indexAccess = primitive.IndexAccessor;
                    var baseVertex = (uint)vertices.Count;

                    for (var i = 0; i < posAccess.Count; i++)
                    {
                        var pos = posAccess.AsVector3Array()[i];
                        var normal = normAccess != null
                            ? normAccess.AsVector3Array()[i]
                            : Vector3.UnitY;

                        vertices.Add(new Vertex(pos, tint, normal));
                    }

                    if (indexAccess != null)
                    {
                        var indexArray = indexAccess.AsIndicesArray();
                        foreach (var idx in indexArray)
                        {
                            indices.Add((uint)idx + baseVertex);
                        }
                    }
                    else
                    {
                        for (uint i = 0; i < posAccess.Count; i++)
                        {
                            indices.Add(baseVertex + i);
                        }
                    }
                }
            }
        }

        if (vertices.Count == 0)
            throw new InvalidOperationException($"GLTF file '{path}' contains no meshes.");

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }
}
