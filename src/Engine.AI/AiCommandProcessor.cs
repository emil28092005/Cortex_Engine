using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.AI.Commands;
using Engine.AI.Serialization;
using Engine.Core;
using Engine.Core.Components;
using Flecs.NET.Core;

namespace Engine.AI;

/// <summary>
/// Parses and executes JSON commands from an AI agent.
/// </summary>
public sealed class AiCommandProcessor
{
    private readonly World _world;
    private readonly Func<string, Mesh> _modelLoader;
    private readonly JsonSerializerOptions _jsonOptions;

    public AiCommandProcessor(World world, Func<string, Mesh> modelLoader)
    {
        _world = world;
        _modelLoader = modelLoader;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter(),
                new Vector3JsonConverter(),
                new QuaternionJsonConverter()
            }
        };
    }

    /// <summary>
    /// Process a single JSON command string.
    /// </summary>
    public AiCommandResult Process(string json)
    {
        try
        {
            var command = JsonSerializer.Deserialize<AiCommand>(json, _jsonOptions);
            if (command == null)
                return AiCommandResult.Error("Failed to parse command.");

            return command switch
            {
                SpawnModelCommand c => SpawnModel(c),
                SetTransformCommand c => SetTransform(c),
                DeleteEntityCommand c => DeleteEntity(c),
                ListEntitiesCommand => ListEntities(),
                _ => AiCommandResult.Error($"Unknown command type: {command.Type}")
            };
        }
        catch (Exception ex)
        {
            return AiCommandResult.Error($"Command execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Process a batch of JSON commands separated by newlines.
    /// </summary>
    public AiCommandResult[] ProcessBatch(string jsonBatch)
    {
        var lines = jsonBatch.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return lines.Select(Process).ToArray();
    }

    private AiCommandResult SpawnModel(SpawnModelCommand command)
    {
        var mesh = _modelLoader(command.ModelPath);
        var entity = _world.Entity(command.Name)
            .Set(new Transform(command.Position, command.Rotation, command.Scale))
            .Set(mesh);

        return AiCommandResult.Ok($"Spawned entity '{command.Name}' with model '{command.ModelPath}' (id {(ulong)entity.Id}).");
    }

    private AiCommandResult SetTransform(SetTransformCommand command)
    {
        var entity = _world.Lookup(command.Name);
        if ((ulong)entity.Id == 0)
            return AiCommandResult.Error($"Entity '{command.Name}' not found.");

        ref var transform = ref entity.Ensure<Transform>();

        if (command.Position.HasValue)
            transform.Position = command.Position.Value;
        if (command.Rotation.HasValue)
            transform.Rotation = command.Rotation.Value;
        if (command.Scale.HasValue)
            transform.Scale = command.Scale.Value;

        return AiCommandResult.Ok($"Updated transform for entity '{command.Name}'.");
    }

    private AiCommandResult DeleteEntity(DeleteEntityCommand command)
    {
        var entity = _world.Lookup(command.Name);
        if ((ulong)entity.Id == 0)
            return AiCommandResult.Error($"Entity '{command.Name}' not found.");

        entity.Destruct();

        return AiCommandResult.Ok($"Deleted entity '{command.Name}'.");
    }

    private AiCommandResult ListEntities()
    {
        var names = new List<string>();
        _world.Each((Entity e, ref Transform t) =>
        {
            var name = e.Name();
            if (!string.IsNullOrEmpty(name))
                names.Add(name);
        });

        var json = JsonSerializer.Serialize(names, _jsonOptions);
        return AiCommandResult.Ok(json);
    }
}
