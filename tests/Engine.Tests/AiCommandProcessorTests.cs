using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.AI;
using Flecs.NET.Core;

namespace Engine.Tests;

public class AiCommandProcessorTests
{
    private static (AiCommandProcessor, World) CreateProcessor()
    {
        var world = World.Create();
        var dummyMesh = new Mesh(
            new[] { new Vertex(new Vector3(0, 0, 0), Vector3.One, Vector3.UnitY) },
            new uint[] { 0 });
        var processor = new AiCommandProcessor(
            world,
            _ => dummyMesh,
            _ => { });
        return (processor, world);
    }

    [Fact]
    public void SpawnModel_Creates_Entity_With_Transform()
    {
        var (processor, world) = CreateProcessor();

        var result = processor.Process("""
            { "type": "spawn_model", "name": "TestCube", "modelPath": "fake.obj", "position": [1, 2, 3] }
            """);

        Assert.True(result.Success);
        var entity = world.Lookup("TestCube");
        Assert.True((ulong)entity.Id != 0);
        var transform = entity.Get<Transform>();
        Assert.Equal(new Vector3(1, 2, 3), transform.Position);
    }

    [Fact]
    public void SetTransform_Updates_Position()
    {
        var (processor, world) = CreateProcessor();
        processor.Process("""{ "type": "spawn_model", "name": "Test", "modelPath": "x.obj" }""");

        var result = processor.Process("""
            { "type": "set_transform", "name": "Test", "position": [5, 5, 5] }
            """);

        Assert.True(result.Success);
        var transform = world.Lookup("Test").Get<Transform>();
        Assert.Equal(new Vector3(5, 5, 5), transform.Position);
    }

    [Fact]
    public void SetTransform_Partial_Update_Keeps_Other_Fields()
    {
        var (processor, world) = CreateProcessor();
        processor.Process("""{ "type": "spawn_model", "name": "Test", "modelPath": "x.obj", "position": [1, 1, 1], "scale": [2, 2, 2] }""");

        processor.Process("""{ "type": "set_transform", "name": "Test", "position": [9, 9, 9] }""");

        var transform = world.Lookup("Test").Get<Transform>();
        Assert.Equal(new Vector3(9, 9, 9), transform.Position);
        Assert.Equal(new Vector3(2, 2, 2), transform.Scale);
    }

    [Fact]
    public void SetMaterial_Updates_Albedo_And_Roughness()
    {
        var (processor, world) = CreateProcessor();
        processor.Process("""{ "type": "spawn_model", "name": "Test", "modelPath": "x.obj" }""");

        var result = processor.Process("""
            { "type": "set_material", "name": "Test", "albedo": [1, 0, 0], "roughness": 0.8 }
            """);

        Assert.True(result.Success);
        var mat = world.Lookup("Test").Get<Material>();
        Assert.Equal(new Vector3(1, 0, 0), mat.Albedo);
        Assert.Equal(0.8f, mat.Roughness);
    }

    [Fact]
    public void DeleteEntity_Removes_Entity()
    {
        var (processor, world) = CreateProcessor();
        processor.Process("""{ "type": "spawn_model", "name": "ToDelete", "modelPath": "x.obj" }""");

        var result = processor.Process("""{ "type": "delete_entity", "name": "ToDelete" }""");

        Assert.True(result.Success);
        Assert.True((ulong)world.Lookup("ToDelete").Id == 0);
    }

    [Fact]
    public void ListEntities_Returns_Names()
    {
        var (processor, world) = CreateProcessor();
        processor.Process("""{ "type": "spawn_model", "name": "Alpha", "modelPath": "x.obj" }""");
        processor.Process("""{ "type": "spawn_model", "name": "Beta", "modelPath": "x.obj" }""");

        var result = processor.Process("""{ "type": "list_entities" }""");

        Assert.True(result.Success);
        Assert.Contains("Alpha", result.Message);
        Assert.Contains("Beta", result.Message);
    }

    [Fact]
    public void CaptureScreenshot_Calls_Callback()
    {
        var world = World.Create();
        var capturedPath = "";
        var processor = new AiCommandProcessor(
            world,
            _ => new Mesh(new[] { new Vertex(Vector3.Zero, Vector3.One, Vector3.UnitY) }, new uint[] { 0 }),
            path => capturedPath = path);

        var result = processor.Process("""{ "type": "capture_screenshot", "outputPath": "test.png" }""");

        Assert.True(result.Success);
        Assert.Equal("test.png", capturedPath);
    }

    [Fact]
    public void GetWorldState_Returns_Json()
    {
        var (processor, world) = CreateProcessor();
        processor.Process("""{ "type": "spawn_model", "name": "StateTest", "modelPath": "x.obj", "position": [1, 2, 3] }""");

        var result = processor.Process("""{ "type": "get_world_state" }""");

        Assert.True(result.Success);
        Assert.Contains("StateTest", result.Message);
        Assert.Contains("position", result.Message);
    }

    [Fact]
    public void SetTransform_On_Nonexistent_Entity_Returns_Error()
    {
        var (processor, world) = CreateProcessor();

        var result = processor.Process("""{ "type": "set_transform", "name": "Ghost", "position": [0, 0, 0] }""");

        Assert.False(result.Success);
    }

    [Fact]
    public void Invalid_JSON_Returns_Error()
    {
        var (processor, world) = CreateProcessor();

        var result = processor.Process("not valid json");

        Assert.False(result.Success);
    }

    [Fact]
    public void ProcessBatch_Handles_Multiple_Commands()
    {
        var (processor, world) = CreateProcessor();

        var results = processor.ProcessBatch("""
            { "type": "spawn_model", "name": "A", "modelPath": "x.obj" }
            { "type": "spawn_model", "name": "B", "modelPath": "x.obj" }
            """);

        Assert.Equal(2, results.Length);
        Assert.True(results[0].Success);
        Assert.True(results[1].Success);
    }
}
