using System.Collections.Generic;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Graphics;

/// <summary>
/// Procedural mesh generators for common primitive shapes.
/// All methods are pure CPU — no GPU/display dependencies.
/// </summary>
public static class ProceduralMesh
{
    /// <summary>
    /// Generate a UV sphere mesh.
    /// </summary>
    /// <param name="radius">Sphere radius.</param>
    /// <param name="segments">Longitude segments (around the equator).</param>
    /// <param name="rings">Latitude rings (from pole to pole).</param>
    /// <param name="color">Vertex color applied to all vertices.</param>
    public static Mesh CreateSphere(float radius, int segments, int rings, Vector3 color)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        for (var ring = 0; ring <= rings; ring++)
        {
            var phi = MathF.PI * ring / rings;
            var sinPhi = MathF.Sin(phi);
            var cosPhi = MathF.Cos(phi);

            for (var seg = 0; seg <= segments; seg++)
            {
                var theta = 2.0f * MathF.PI * seg / segments;
                var sinTheta = MathF.Sin(theta);
                var cosTheta = MathF.Cos(theta);

                var x = radius * sinPhi * cosTheta;
                var y = radius * cosPhi;
                var z = radius * sinPhi * sinTheta;
                var normal = Vector3.Normalize(new Vector3(x, y, z));

                vertices.Add(new Vertex(new Vector3(x, y, z), color, normal));
            }
        }

        for (var ring = 0; ring < rings; ring++)
        {
            for (var seg = 0; seg < segments; seg++)
            {
                var i0 = (uint)(ring * (segments + 1) + seg);
                var i1 = i0 + 1;
                var i2 = i0 + (uint)(segments + 1);
                var i3 = i2 + 1;

                indices.Add(i0); indices.Add(i1); indices.Add(i2);
                indices.Add(i1); indices.Add(i3); indices.Add(i2);
            }
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Generate a ground grid mesh at Y=0, consisting of thin quads.
    /// </summary>
    /// <param name="lines">Number of grid lines on each side of the origin.</param>
    /// <param name="spacing">Distance between grid lines.</param>
    /// <param name="color">Vertex color applied to all vertices.</param>
    public static Mesh CreateGrid(int lines, float spacing, Vector3 color)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        var extent = lines * spacing;
        var normal = Vector3.UnitY;
        var halfWidth = 0.02f;

        for (var i = -lines; i <= lines; i++)
        {
            var offset = i * spacing;

            var baseIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(-extent, 0, offset - halfWidth), color, normal));
            vertices.Add(new Vertex(new Vector3(extent, 0, offset - halfWidth), color, normal));
            vertices.Add(new Vertex(new Vector3(extent, 0, offset + halfWidth), color, normal));
            vertices.Add(new Vertex(new Vector3(-extent, 0, offset + halfWidth), color, normal));
            indices.Add(baseIndex); indices.Add(baseIndex + 1); indices.Add(baseIndex + 2);
            indices.Add(baseIndex); indices.Add(baseIndex + 2); indices.Add(baseIndex + 3);

            baseIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(offset - halfWidth, 0, -extent), color, normal));
            vertices.Add(new Vertex(new Vector3(offset + halfWidth, 0, -extent), color, normal));
            vertices.Add(new Vertex(new Vector3(offset + halfWidth, 0, extent), color, normal));
            vertices.Add(new Vertex(new Vector3(offset - halfWidth, 0, extent), color, normal));
            indices.Add(baseIndex); indices.Add(baseIndex + 1); indices.Add(baseIndex + 2);
            indices.Add(baseIndex); indices.Add(baseIndex + 2); indices.Add(baseIndex + 3);
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }
}
