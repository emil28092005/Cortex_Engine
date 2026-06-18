using System.Numerics;
using Engine.Core;
using Engine.Graphics;

namespace Engine.Tests;

public class MeshMathTests
{
    [Fact]
    public void Computes_Normal_For_Triangle()
    {
        var n = MeshMath.ComputeFaceNormal(
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0));

        Assert.Equal(0f, n.X, 0.001f);
        Assert.Equal(0f, n.Y, 0.001f);
        Assert.Equal(1f, n.Z, 0.001f);
    }

    [Fact]
    public void Normal_Is_Unit_Length()
    {
        var n = MeshMath.ComputeFaceNormal(
            new Vector3(0, 0, 0),
            new Vector3(3, 0, 0),
            new Vector3(0, 4, 0));

        Assert.Equal(1f, n.Length(), 0.001f);
    }

    [Fact]
    public void Degenerate_Triangle_Falls_Back_To_UnitY()
    {
        var n = MeshMath.ComputeFaceNormal(
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(2, 0, 0));

        Assert.Equal(Vector3.UnitY, n);
    }
}

public class ProceduralMeshTests
{
    [Fact]
    public void Sphere_Has_Correct_Vertex_Count()
    {
        var mesh = ProceduralMesh.CreateSphere(1f, 16, 8, Vector3.One);

        Assert.Equal((8 + 1) * (16 + 1), mesh.Vertices.Length);
    }

    [Fact]
    public void Sphere_Has_Correct_Index_Count()
    {
        var mesh = ProceduralMesh.CreateSphere(1f, 16, 8, Vector3.One);

        Assert.Equal(7 * 16 * 6, mesh.Indices.Length);
    }

    [Fact]
    public void Sphere_Vertices_Lie_On_Surface()
    {
        const float radius = 2.5f;
        var mesh = ProceduralMesh.CreateSphere(radius, 8, 4, Vector3.One);

        foreach (var v in mesh.Vertices)
            Assert.Equal(radius, v.Position.Length(), 0.001f);
    }

    [Fact]
    public void Sphere_Normals_Are_Unit_Length()
    {
        var mesh = ProceduralMesh.CreateSphere(1f, 8, 4, Vector3.One);

        foreach (var v in mesh.Vertices)
            Assert.Equal(1f, v.Normal.Length(), 0.001f);
    }

    [Fact]
    public void Sphere_Top_Vertex_At_Positive_Z()
    {
        var mesh = ProceduralMesh.CreateSphere(1f, 8, 4, Vector3.One);

        Assert.Equal(1f, mesh.Vertices[0].Position.Z, 0.001f);
        Assert.Equal(0f, mesh.Vertices[0].Position.X, 0.001f);
        Assert.Equal(0f, mesh.Vertices[0].Position.Y, 0.001f);
    }

    [Fact]
    public void Grid_Has_Correct_Vertex_Count()
    {
        var mesh = ProceduralMesh.CreateGrid(5, 1f, Vector3.One);

        var expectedLines = 2 * 5 + 1;
        Assert.Equal(expectedLines * 4, mesh.Vertices.Length);
    }

    [Fact]
    public void Grid_All_Normals_Point_Up()
    {
        var mesh = ProceduralMesh.CreateGrid(3, 1f, Vector3.One);

        foreach (var v in mesh.Vertices)
            Assert.Equal(Vector3.UnitY, v.Normal);
    }

    [Fact]
    public void Grid_Extent_Matches_Lines_And_Spacing()
    {
        var mesh = ProceduralMesh.CreateGrid(10, 2f, Vector3.One);

        var maxPos = 10f * 2f;
        Assert.True(mesh.Vertices.Any(v => v.Position.X <= -maxPos));
        Assert.True(mesh.Vertices.Any(v => v.Position.X >= maxPos));
    }
}
