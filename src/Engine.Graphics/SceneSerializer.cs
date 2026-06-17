using System.Numerics;
using System.Text.Json;
using Engine.Core.Components;
using Flecs.NET.Core;

namespace Engine.Graphics;

/// <summary>
/// Serializes and deserializes entity scenes to JSON.
/// Minimal version: handles Transform, Material, Light, Camera, Mesh.
/// </summary>
public static class SceneSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public static string SaveToString(World world)
    {
        var entities = new List<SceneEntity>();
        world.Each((Entity e, ref Transform t) =>
        {
            var name = e.Name();
            var entity = new SceneEntity { Name = name };

            entity.Transform = t;

            if (e.Has<Material>())
                entity.Material = e.Get<Material>();

            if (e.Has<Light>())
                entity.Light = e.Get<Light>();

            if (e.Has<Camera>())
                entity.Camera = e.Get<Camera>();

            entities.Add(entity);
        });

        return JsonSerializer.Serialize(entities, Options);
    }

    public static void SaveToFile(World world, string path)
    {
        var json = SaveToString(world);
        File.WriteAllText(path, json);
    }

    public static int LoadFromString(World world, string json)
    {
        var entities = JsonSerializer.Deserialize<List<SceneEntity>>(json, Options);
        if (entities == null) return 0;

        foreach (var e in entities)
        {
            var entity = world.Entity(e.Name);
            entity.Set(e.Transform);

            if (e.Material != null)
                entity.Set(e.Material.Value);

            if (e.Light != null)
                entity.Set(e.Light.Value);

            if (e.Camera != null)
                entity.Set(e.Camera.Value);
        }

        return entities.Count;
    }

    public static void LoadFromFile(World world, string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Scene file not found: {path}", path);

        var json = File.ReadAllText(path);
        LoadFromString(world, json);
    }

    private class SceneEntity
    {
        public string Name { get; set; } = string.Empty;
        public Transform Transform { get; set; }
        public Material? Material { get; set; }
        public Light? Light { get; set; }
        public Camera? Camera { get; set; }
    }
}
