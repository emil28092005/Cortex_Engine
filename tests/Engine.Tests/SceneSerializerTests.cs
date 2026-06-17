using System.IO;
using System.Numerics;
using Engine.Core.Components;
using Engine.Graphics;
using Flecs.NET.Core;

namespace Engine.Tests;

public class SceneSerializerTests
{
    private static World CreateWorldWithEntities()
    {
        var world = World.Create();
        world.Entity("CubeA")
            .Set(new Transform(new Vector3(1, 2, 3), Quaternion.Identity, Vector3.One))
            .Set(new Material(new Vector3(0.9f, 0.2f, 0.2f), 0.4f, 0.1f));

        world.Entity("CubeB")
            .Set(new Transform(new Vector3(-1, 0, 5), Quaternion.Identity, new Vector3(2, 2, 2)))
            .Set(new Material(new Vector3(0.2f, 0.8f, 0.3f), 0.7f, 0.0f));

        return world;
    }

    [Fact]
    public void SaveToString_Produces_NonEmpty_Json()
    {
        using var world = CreateWorldWithEntities();

        var json = SceneSerializer.SaveToString(world);

        Assert.False(string.IsNullOrEmpty(json));
        Assert.Contains("CubeA", json);
        Assert.Contains("CubeB", json);
    }

    [Fact]
    public void SaveToFile_Creates_File()
    {
        using var world = CreateWorldWithEntities();
        var path = Path.Combine(Path.GetTempPath(), $"scene_{Guid.NewGuid():N}.json");

        try
        {
            SceneSerializer.SaveToFile(world, path);

            Assert.True(File.Exists(path));
            var content = File.ReadAllText(path);
            Assert.Contains("CubeA", content);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromString_Adds_Entities()
    {
        using var sourceWorld = CreateWorldWithEntities();
        var json = SceneSerializer.SaveToString(sourceWorld);

        using var targetWorld = World.Create();
        var loaded = SceneSerializer.LoadFromString(targetWorld, json);

        Assert.True(loaded > 0);
        var entity = targetWorld.Lookup("CubeA");
        Assert.True((ulong)entity.Id != 0);
    }

    [Fact]
    public void LoadFromFile_Restores_Entities()
    {
        using var sourceWorld = CreateWorldWithEntities();
        var path = Path.Combine(Path.GetTempPath(), $"scene_{Guid.NewGuid():N}.json");

        try
        {
            SceneSerializer.SaveToFile(sourceWorld, path);

            using var targetWorld = World.Create();
            SceneSerializer.LoadFromFile(targetWorld, path);

            var entity = targetWorld.Lookup("CubeB");
            Assert.True((ulong)entity.Id != 0);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_Throws_For_Missing_File()
    {
        using var world = World.Create();

        Assert.Throws<FileNotFoundException>(() =>
            SceneSerializer.LoadFromFile(world, "/nonexistent/scene.json"));
    }

    [Fact]
    public void Roundtrip_Preserves_Transform_Position()
    {
        using var sourceWorld = World.Create();
        sourceWorld.Entity("TestEntity")
            .Set(new Transform(new Vector3(5, 10, 15), Quaternion.Identity, Vector3.One));

        var json = SceneSerializer.SaveToString(sourceWorld);

        using var targetWorld = World.Create();
        SceneSerializer.LoadFromString(targetWorld, json);

        var entity = targetWorld.Lookup("TestEntity");
        Assert.True((ulong)entity.Id != 0);
        var transform = entity.Get<Transform>();
        Assert.Equal(5f, transform.Position.X, 0.001f);
        Assert.Equal(10f, transform.Position.Y, 0.001f);
        Assert.Equal(15f, transform.Position.Z, 0.001f);
    }
}
