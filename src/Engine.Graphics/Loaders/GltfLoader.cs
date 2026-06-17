using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Engine.Core;
using EngineCoreMaterial = Engine.Core.Components.Material;
using EngineMesh = Engine.Core.Components.Mesh;
using SharpGLTF.Schema2;

namespace Engine.Graphics.Loaders;

/// <summary>
/// glTF/glTF binary loader using SharpGLTF.Core.
/// Loads all primitives across all meshes, extracting:
/// - Positions, normals (from file or computed), texcoords
/// - PBR material: albedo, roughness, metallic, base color texture
/// </summary>
public static class GltfLoader
{
    /// <summary>
    /// Load a glTF/GLB file and return the combined mesh plus extracted materials.
    /// </summary>
    public static EngineMesh Load(string path, Vector3? defaultColor = null)
    {
        var (mesh, _) = LoadWithMaterials(path, defaultColor);
        return mesh;
    }

    /// <summary>
    /// Load a glTF/GLB file and return the combined mesh plus a list of
    /// (primitive index, material) pairs. Textures are extracted to a
    /// temp directory next to the source file.
    /// </summary>
    public static (EngineMesh Mesh, List<EngineCoreMaterial> Materials) LoadWithMaterials(
        string path, Vector3? defaultColor = null)
    {
        var color = defaultColor ?? new Vector3(0.7f, 0.7f, 0.7f);
        var textureDir = Path.Combine(Path.GetDirectoryName(path) ?? ".", "extracted_textures");
        var model = ModelRoot.Load(path);

        if (model.LogicalMeshes.Count == 0)
            throw new InvalidOperationException($"glTF file has no meshes: {path}");

        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        var materials = new List<EngineCoreMaterial>();

        foreach (var mesh in model.LogicalMeshes)
        {
            foreach (var primitive in mesh.Primitives)
            {
                if (!primitive.VertexAccessors.TryGetValue("POSITION", out var positionAccessor))
                    continue;

                var positions = positionAccessor.AsVector3Array();
                var normals = primitive.VertexAccessors.TryGetValue("NORMAL", out var normalAccessor)
                    ? normalAccessor.AsVector3Array()
                    : null;
                var uvs = primitive.VertexAccessors.TryGetValue("TEXCOORD_0", out var uvAccessor)
                    ? uvAccessor.AsVector2Array()
                    : null;

                var primIndices = GetIndices(primitive, positions.Count);
                var material = ExtractMaterial(primitive, color, textureDir);
                materials.Add(material);

                var vertexBase = (uint)vertices.Count;

                for (var i = 0; i < primIndices.Length; i += 3)
                {
                    var i0 = (int)primIndices[i];
                    var i1 = (int)primIndices[i + 1];
                    var i2 = (int)primIndices[i + 2];

                    var v0 = ToVertex(positions, normals, uvs, i0, color);
                    var v1 = ToVertex(positions, normals, uvs, i1, color);
                    var v2 = ToVertex(positions, normals, uvs, i2, color);

                    if (normals == null)
                    {
                        var n = MeshMath.ComputeFaceNormal(v0.Position, v1.Position, v2.Position);
                        v0.Normal = n;
                        v1.Normal = n;
                        v2.Normal = n;
                    }

                    indices.Add(vertexBase + (uint)i0);
                    indices.Add(vertexBase + (uint)i1);
                    indices.Add(vertexBase + (uint)i2);

                    if (i == 0)
                    {
                        vertices.AddRange(new[] { v0, v1, v2 });
                    }
                }

                if (normals != null || uvs != null)
                {
                    for (var i = 0; i < positions.Count; i++)
                        vertices.Add(ToVertex(positions, normals, uvs, i, color));
                }

                vertexBase = (uint)vertices.Count;
            }
        }

        return (new EngineMesh(vertices.ToArray(), indices.ToArray()), materials);
    }

    private static Vertex ToVertex(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<Vector3>? normals,
        IReadOnlyList<Vector2>? uvs,
        int index,
        Vector3 color)
    {
        var pos = new Vector3(positions[index].X, positions[index].Y, positions[index].Z);
        var normal = normals != null
            ? Vector3.Normalize(new Vector3(normals[index].X, normals[index].Y, normals[index].Z))
            : Vector3.UnitY;

        return new Vertex(pos, color, normal);
    }

    private static EngineCoreMaterial ExtractMaterial(MeshPrimitive primitive, Vector3 defaultColor, string textureDir)
    {
        var albedo = defaultColor;
        var roughness = 0.5f;
        var metallic = 0.0f;
        string? texturePath = null;

        var gltfMat = primitive.Material;
        if (gltfMat == null)
            return new EngineCoreMaterial(albedo, roughness, metallic);

        if (gltfMat.FindChannel("BaseColor") is { } baseColor)
        {
            foreach (var param in baseColor.Parameters)
            {
                if (param.Name == "BaseColorFactor" && param.Value is Vector4 factor)
                {
                    albedo = new Vector3(factor.X, factor.Y, factor.Z);
                }
            }

            if (baseColor.Texture?.PrimaryImage is { } img)
            {
                var mem = img.Content;
                if (!string.IsNullOrEmpty(mem.SourcePath) && File.Exists(mem.SourcePath))
                {
                    texturePath = mem.SourcePath;
                }
                else if (mem.IsValid)
                {
                    Directory.CreateDirectory(textureDir);
                    var ext = string.IsNullOrEmpty(mem.FileExtension) ? ".png" : mem.FileExtension;
                    texturePath = Path.Combine(textureDir, $"tex_{Guid.NewGuid():N}{ext}");
                    mem.SaveToFile(texturePath);
                }
            }
        }

        if (gltfMat.FindChannel("MetallicRoughness") is { } mr)
        {
            foreach (var param in mr.Parameters)
            {
                if (param.Name == "MetallicFactor" && param.Value is float mf)
                    metallic = mf;
                if (param.Name == "RoughnessFactor" && param.Value is float rf)
                    roughness = rf;
            }
        }

        return new EngineCoreMaterial(albedo, roughness, metallic, texturePath);
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

        var auto = new uint[positionCount];
        for (var i = 0; i < positionCount; i++)
            auto[i] = (uint)i;
        return auto;
    }

    private static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
        => MeshMath.ComputeFaceNormal(a, b, c);
}
