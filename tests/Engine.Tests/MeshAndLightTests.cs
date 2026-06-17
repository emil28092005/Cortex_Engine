using System.Numerics;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Tests;

public class MeshAndLightTests
{
    [Fact]
    public void Mesh_Stores_Vertices_And_Indices()
    {
        var vertices = new[]
        {
            new Vertex(new Vector3(0, 0, 0), Vector3.One, Vector3.UnitY),
            new Vertex(new Vector3(1, 0, 0), Vector3.One, Vector3.UnitY),
            new Vertex(new Vector3(1, 1, 0), Vector3.One, Vector3.UnitY),
        };
        var indices = new uint[] { 0, 1, 2 };
        var mesh = new Mesh(vertices, indices);

        Assert.Equal(3, mesh.Vertices.Length);
        Assert.Equal(3, mesh.Indices.Length);
    }

    [Fact]
    public void Light_Direction_Is_Normalized()
    {
        var light = Light.Directional(new Vector3(0, 2, 0), Vector3.One, 1.0f);

        Assert.Equal(1f, light.Direction.Length(), 0.001f);
    }

    [Fact]
    public void Light_With_Zero_Direction_Defaults_To_UnitY()
    {
        var light = Light.Directional(Vector3.Zero, Vector3.One, 1.0f);

        Assert.Equal(Vector3.UnitY, light.Direction);
    }
}
