using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core.Components;
using Flecs.NET.Core;

namespace Engine.Graphics;

/// <summary>
/// Scene serialization — saves and loads the ECS world to/from JSON.
/// Uses manual serialization for named entities with Transform, Material, Light, Camera components.
/// </summary>
public static class SceneSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters =
        {
            new Vector3JsonConverter(),
            new QuaternionJsonConverter()
        }
    };

    /// <summary>
    /// Serialize all named entities with their components to a JSON string.
    /// </summary>
    public static string SaveToString(World world)
    {
        var entities = new List<SceneEntity>();
        var processedNames = new HashSet<string>();

        world.Each((Entity e, ref Transform _) =>
        {
            var name = e.Name();
            if (string.IsNullOrEmpty(name))
                return;

            if (processedNames.Contains(name))
                return;
            processedNames.Add(name);

            var entry = new SceneEntity { Name = name };

            if (e.Has<Transform>())
            {
                var t = e.Get<Transform>();
                entry.Transform = new SceneTransform
                {
                    Position = t.Position,
                    Rotation = t.Rotation,
                    Scale = t.Scale
                };
            }

            if (e.Has<Material>())
            {
                var m = e.Get<Material>();
                entry.Material = new SceneMaterial
                {
                    Albedo = m.Albedo,
                    Roughness = m.Roughness,
                    Metallic = m.Metallic,
                    TexturePath = m.TexturePath
                };
            }

            if (e.Has<Light>())
            {
                var l = e.Get<Light>();
                entry.Light = new SceneLight
                {
                    Direction = l.Direction,
                    Color = l.Color,
                    Intensity = l.Intensity
                };
            }

            if (e.Has<Camera>())
            {
                var c = e.Get<Camera>();
                entry.Camera = new SceneCamera
                {
                    Position = c.Position,
                    Target = c.Target,
                    Up = c.Up,
                    FieldOfView = c.FieldOfView,
                    NearPlane = c.NearPlane,
                    FarPlane = c.FarPlane
                };
            }

            entities.Add(entry);
        });

        return JsonSerializer.Serialize(entities, JsonOptions);
    }

    /// <summary>
    /// Save the world to a JSON file.
    /// </summary>
    public static void SaveToFile(World world, string path)
    {
        var json = SaveToString(world);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Load entities from a JSON string into the world.
    /// Returns the number of entities loaded.
    /// </summary>
    public static int LoadFromString(World world, string json)
    {
        var entities = JsonSerializer.Deserialize<List<SceneEntity>>(json, JsonOptions);
        if (entities == null) return 0;

        foreach (var entry in entities)
        {
            var entity = world.Entity(entry.Name);

            if (entry.Transform != null)
            {
                entity.Set(new Transform(
                    entry.Transform.Position,
                    entry.Transform.Rotation,
                    entry.Transform.Scale));
            }

            if (entry.Material != null)
            {
                entity.Set(new Material(
                    entry.Material.Albedo,
                    entry.Material.Roughness,
                    entry.Material.Metallic,
                    entry.Material.TexturePath));
            }

            if (entry.Light != null)
            {
                entity.Set(new Light(
                    entry.Light.Direction,
                    entry.Light.Color,
                    entry.Light.Intensity));
            }

            if (entry.Camera != null)
            {
                entity.Set(new Camera(
                    entry.Camera.Position,
                    entry.Camera.Target,
                    entry.Camera.Up,
                    entry.Camera.FieldOfView,
                    16f / 9f,
                    entry.Camera.NearPlane,
                    entry.Camera.FarPlane));
            }
        }

        return entities.Count;
    }

    /// <summary>
    /// Load entities from a JSON file into the world.
    /// Returns the number of entities loaded.
    /// </summary>
    public static int LoadFromFile(World world, string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Scene file not found: {path}");

        var json = File.ReadAllText(path);
        return LoadFromString(world, json);
    }
}

// Serialization DTOs

internal sealed class SceneEntity
{
    public string Name { get; set; } = "";
    public SceneTransform? Transform { get; set; }
    public SceneMaterial? Material { get; set; }
    public SceneLight? Light { get; set; }
    public SceneCamera? Camera { get; set; }
}

internal sealed class SceneTransform
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }
    public Vector3 Scale { get; set; }
}

internal sealed class SceneMaterial
{
    public Vector3 Albedo { get; set; }
    public float Roughness { get; set; }
    public float Metallic { get; set; }
    public string? TexturePath { get; set; }
}

internal sealed class SceneLight
{
    public Vector3 Direction { get; set; }
    public Vector3 Color { get; set; }
    public float Intensity { get; set; }
}

internal sealed class SceneCamera
{
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public Vector3 Up { get; set; }
    public float FieldOfView { get; set; }
    public float NearPlane { get; set; }
    public float FarPlane { get; set; }
}

internal sealed class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for Vector3");
        reader.Read();
        var x = reader.GetSingle();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();
        var z = reader.GetSingle();
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected 3 elements for Vector3");
        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}

internal sealed class QuaternionJsonConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for Quaternion");
        reader.Read();
        var x = reader.GetSingle();
        reader.Read();
        var y = reader.GetSingle();
        reader.Read();
        var z = reader.GetSingle();
        reader.Read();
        var w = reader.GetSingle();
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException("Expected 4 elements for Quaternion");
        return new Quaternion(x, y, z, w);
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteNumberValue(value.W);
        writer.WriteEndArray();
    }
}
