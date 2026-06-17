using System.IO;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics.Loaders;

namespace Engine.Tests;

public class ObjLoaderTests
{
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "CortexEngineTests");
    private static string WriteTempObj(string content)
    {
        Directory.CreateDirectory(TempDir);
        var path = Path.Combine(TempDir, $"test_{Guid.NewGuid():N}.obj");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Loads_Single_Triangle()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """);

        var mesh = ObjLoader.Load(path, new Vector3(1, 1, 1));

        Assert.Equal(3, mesh.Vertices.Length);
        Assert.Equal(3, mesh.Indices.Length);
    }

    [Fact]
    public void Triangulates_Quad_As_Fan()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 1 1 0
            v 0 1 0
            f 1 2 3 4
            """);

        var mesh = ObjLoader.Load(path);

        Assert.Equal(6, mesh.Vertices.Length);
        Assert.Equal(6, mesh.Indices.Length);
    }

    [Fact]
    public void Parses_Face_With_Texcoord_Format()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vt 0 0
            vt 1 0
            vt 0 1
            f 1/1 2/2 3/3
            """);

        var mesh = ObjLoader.Load(path);

        Assert.Equal(3, mesh.Vertices.Length);
    }

    [Fact]
    public void Parses_Face_With_Normal_Format()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            f 1//1 2//1 3//1
            """);

        var mesh = ObjLoader.Load(path);

        Assert.Equal(3, mesh.Vertices.Length);
    }

    [Fact]
    public void Computes_Face_Normal_For_Triangle()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """);

        var mesh = ObjLoader.Load(path);

        var normal = mesh.Vertices[0].Normal;
        Assert.Equal(0f, normal.X, 0.001f);
        Assert.Equal(0f, normal.Y, 0.001f);
        Assert.Equal(1f, normal.Z, 0.001f);
    }

    [Fact]
    public void Skips_Comments_And_Blank_Lines()
    {
        var path = WriteTempObj("""
            # This is a comment

            v 0 0 0
            # Another comment
            v 1 0 0
            v 0 1 0

            f 1 2 3
            """);

        var mesh = ObjLoader.Load(path);

        Assert.Equal(3, mesh.Vertices.Length);
    }

    [Fact]
    public void Throws_On_Empty_File()
    {
        var path = WriteTempObj("# just a comment\n");

        Assert.Throws<InvalidOperationException>(() => ObjLoader.Load(path));
    }

    [Fact]
    public void Default_Color_When_Not_Specified()
    {
        var path = WriteTempObj("""
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """);

        var mesh = ObjLoader.Load(path);

        Assert.Equal(0.7f, mesh.Vertices[0].Color.X, 0.001f);
    }
}
