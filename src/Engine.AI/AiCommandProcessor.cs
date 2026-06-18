using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.AI.Commands;
using Engine.AI.Serialization;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Flecs.NET.Core;

namespace Engine.AI;

/// <summary>
/// Parses and executes JSON commands from an AI agent.
/// </summary>
public sealed class AiCommandProcessor
{
    private readonly World _world;
    private readonly Func<string, Mesh> _modelLoader;
    private readonly Action<string> _requestScreenshot;

    public JsonSerializerOptions JsonOptions { get; }

    public AiCommandProcessor(World world, Func<string, Mesh> modelLoader, Action<string> requestScreenshot)
    {
        _world = world;
        _modelLoader = modelLoader;
        _requestScreenshot = requestScreenshot;
        JsonOptions = new JsonSerializerOptions
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
            var command = JsonSerializer.Deserialize<AiCommand>(json, JsonOptions);
            if (command == null)
                return AiCommandResult.Error("Failed to parse command.");

            return command switch
            {
                SpawnModelCommand c => SpawnModel(c),
                SetTransformCommand c => SetTransform(c),
                DeleteEntityCommand c => DeleteEntity(c),
                ListEntitiesCommand => ListEntities(),
                CaptureScreenshotCommand c => CaptureScreenshot(c),
                GetWorldStateCommand => GetWorldState(),
                SetMaterialCommand c => SetMaterial(c),
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
        Mesh mesh;
        if (command.Shape == "sphere")
        {
            var r = MathF.Max(MathF.Max(command.Scale.X, command.Scale.Y), command.Scale.Z) * 0.5f;
            mesh = ProceduralMesh.CreateSphere(r, 24, 12, new Vector3(0.5f, 0.6f, 0.9f));
        }
        else
        {
            mesh = _modelLoader(command.ModelPath);
        }

        var entity = _world.Entity(command.Name)
            .Set(new Transform(command.Position, command.Rotation, command.Scale))
            .Set(mesh);

        if (command.Physics)
        {
            var maxScale = MathF.Max(MathF.Max(command.Scale.X, command.Scale.Y), command.Scale.Z);
            if (command.Shape == "sphere")
            {
                entity.Set(RigidBody.DynamicSphere(maxScale * 0.5f, mass: maxScale * 2f));
            }
            else
            {
                entity.Set(RigidBody.DynamicBox(new Vector3(maxScale * 0.5f), mass: maxScale * 2f));
            }
        }

        return AiCommandResult.Ok($"Spawned entity '{command.Name}' with shape '{command.Shape}' (id {(ulong)entity.Id}).");
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

        var json = JsonSerializer.Serialize(names, JsonOptions);
        return AiCommandResult.Ok(json);
    }

    private AiCommandResult CaptureScreenshot(CaptureScreenshotCommand command)
    {
        var path = command.OutputPath ?? $"screenshot_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.png";
        _requestScreenshot(path);
        return AiCommandResult.Ok($"Screenshot requested: {path}");
    }

    private AiCommandResult SetMaterial(SetMaterialCommand command)
    {
        var entity = _world.Lookup(command.Name);
        if ((ulong)entity.Id == 0)
            return AiCommandResult.Error($"Entity '{command.Name}' not found.");

        ref var material = ref entity.Ensure<Material>();

        if (command.Albedo.HasValue)
            material.Albedo = command.Albedo.Value;
        if (command.Roughness.HasValue)
            material.Roughness = command.Roughness.Value;
        if (command.Metallic.HasValue)
            material.Metallic = command.Metallic.Value;
        if (command.TexturePath is not null)
            material.TexturePath = command.TexturePath;

        return AiCommandResult.Ok($"Updated material for entity '{command.Name}'.");
    }

    private AiCommandResult GetWorldState()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartArray();

            _world.Each((Entity e, ref Transform _) =>
            {
                var name = e.Name();
                if (string.IsNullOrEmpty(name))
                    return;

                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WriteNumber("id", (ulong)e.Id);

                writer.WriteStartObject("components");

                if (e.Has<Transform>())
                {
                    ref var transform = ref e.Ensure<Transform>();
                    writer.WriteStartObject("Transform");
                    WriteVector3(writer, "position", transform.Position);
                    WriteQuaternion(writer, "rotation", transform.Rotation);
                    WriteVector3(writer, "scale", transform.Scale);
                    writer.WriteEndObject();
                }

                if (e.Has<Camera>())
                {
                    ref var camera = ref e.Ensure<Camera>();
                    writer.WriteStartObject("Camera");
                    writer.WriteNumber("fieldOfView", camera.FieldOfView);
                    writer.WriteNumber("aspectRatio", camera.AspectRatio);
                    writer.WriteNumber("nearPlane", camera.NearPlane);
                    writer.WriteNumber("farPlane", camera.FarPlane);
                    WriteVector3(writer, "position", camera.Position);
                    WriteVector3(writer, "target", camera.Target);
                    WriteVector3(writer, "up", camera.Up);
                    writer.WriteEndObject();
                }

                if (e.Has<Material>())
                {
                    ref var material = ref e.Ensure<Material>();
                    writer.WriteStartObject("Material");
                    WriteVector3(writer, "albedo", material.Albedo);
                    writer.WriteNumber("roughness", material.Roughness);
                    writer.WriteNumber("metallic", material.Metallic);
                    if (material.HasTexture)
                        writer.WriteString("texturePath", material.TexturePath);
                    writer.WriteEndObject();
                }

                if (e.Has<Mesh>())
                {
                    ref var mesh = ref e.Ensure<Mesh>();
                    writer.WriteStartObject("Mesh");
                    writer.WriteNumber("vertexCount", mesh.Vertices.Length);
                    writer.WriteNumber("indexCount", mesh.Indices.Length);
                    writer.WriteEndObject();
                }

                if (e.Has<Light>())
                {
                    ref var light = ref e.Ensure<Light>();
                    writer.WriteStartObject("Light");
                    writer.WriteString("type", light.Type.ToString());
                    if (light.IsPoint)
                    {
                        WriteVector3(writer, "position", light.Position);
                        writer.WriteNumber("range", light.Range);
                    }
                    else
                    {
                        WriteVector3(writer, "direction", light.Direction);
                    }
                    WriteVector3(writer, "color", light.Color);
                    writer.WriteNumber("intensity", light.Intensity);
                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            });

            writer.WriteEndArray();
        }

        var json = System.Text.Encoding.UTF8.GetString(stream.ToArray());
        return AiCommandResult.Ok(json);
    }

    private static void WriteVector3(Utf8JsonWriter writer, string propertyName, Vector3 value)
    {
        writer.WriteStartArray(propertyName);
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }

    private static void WriteQuaternion(Utf8JsonWriter writer, string propertyName, Quaternion value)
    {
        writer.WriteStartArray(propertyName);
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteNumberValue(value.W);
        writer.WriteEndArray();
    }
}
