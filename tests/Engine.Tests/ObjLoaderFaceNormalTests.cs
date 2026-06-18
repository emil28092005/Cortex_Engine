using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Engine.Graphics.Loaders;

namespace Engine.Tests;

/// <summary>
/// Tests OBJ loading with face normal computation.
/// When OBJ files don't contain 'vn' lines, the loader should compute
/// face normals from the triangle positions.
/// </summary>
public class ObjLoaderFaceNormalTests
{
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "CortexEngineTests");
    private static string WriteTempObj(string content)
    {
        Directory.CreateDirectory(TempDir);
        var path = Path.Combine(TempDir, $"face_{Guid.NewGuid():N}.obj");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void No_VN_Lines_Computes_Face_Normal()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """);

        var mesh = ObjLoader.Load(path);

        var normal = mesh.Vertices[0].Normal;
        Assert.NotEqual(Vector3.UnitY, normal);
        Assert.Equal(1f, normal.Length(), 0.001f);
    }

    [Fact]
    public void No_VN_Lines_All_Face_Vertices_Get_Same_Normal()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """);

        var mesh = ObjLoader.Load(path);

        Assert.Equal(mesh.Vertices[0].Normal, mesh.Vertices[1].Normal);
        Assert.Equal(mesh.Vertices[0].Normal, mesh.Vertices[2].Normal);
    }

    [Fact]
    public void With_VN_Lines_Uses_Provided_Normals()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            f 1//1 2//1 3//1
            """);

        var mesh = ObjLoader.Load(path);

        Assert.Equal(Vector3.UnitZ, mesh.Vertices[0].Normal);
    }

    [Fact]
    public void Cube_Without_VN_Has_Six_Different_Normals()
    {
        var path = WriteTempObj("""
            v -0.5 -0.5 -0.5
            v  0.5 -0.5 -0.5
            v  0.5  0.5 -0.5
            v -0.5  0.5 -0.5
            v -0.5 -0.5  0.5
            v  0.5 -0.5  0.5
            v  0.5  0.5  0.5
            v -0.5  0.5  0.5
            f 1 2 3
            f 1 3 4
            f 5 7 6
            f 5 8 7
            f 1 5 6
            f 1 6 2
            f 3 7 8
            f 3 8 4
            f 1 4 8
            f 1 8 5
            f 2 6 7
            f 2 7 3
            """);

        var mesh = ObjLoader.Load(path);

        var distinctNormals = mesh.Vertices
            .Select(v => (Math.Round(v.Normal.X, 2), Math.Round(v.Normal.Y, 2), Math.Round(v.Normal.Z, 2)))
            .Distinct()
            .Count();

        Assert.True(distinctNormals >= 3, $"Expected at least 3 distinct normals for cube, got {distinctNormals}");
    }

    [Fact]
    public void Mixed_Faces_With_And_Without_VN()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            v -1 0 0
            vn 0 0 1
            f 1//1 2//1 3//1
            f 1 3 4
            """);

        var mesh = ObjLoader.Load(path);

        Assert.Equal(Vector3.UnitZ, mesh.Vertices[0].Normal);
        var computedNormal = mesh.Vertices[3].Normal;
        Assert.NotEqual(Vector3.UnitY, computedNormal);
        Assert.Equal(1f, computedNormal.Length(), 0.001f);
    }
}
