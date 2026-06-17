using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core.Components;
using Flecs.NET.Core;

namespace Engine.Graphics;

public static class SceneSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SaveToString(World world)
    {
        var entities = new List<SceneEntityData>();

        world.Each((Entity e, ref Transform _) =>
        {
            var name = e.Name();
            if (string.IsNullOrEmpty(name)) return;

            var data = new SceneEntityData { Name = name };

            if (e.Has<Transform>())
            {
                var t = e.Get<Transform>();
                data.Transform = new TransformData
                {
                    Position = new float[] { t.Position.X, t.Position.Y, t.Position.Z },
                    Rotation = new float[] { t.Rotation.X, t.Rotation.Y, t.Rotation.Z, t.Rotation.W },
                    Scale = new float[] { t.Scale.X, t.Scale.Y, t.Scale.Z }
                };
            }

            if (e.Has<Material>())
            {
                var m = e.Get<Material>();
                data.Material = new MaterialData
                {
                    Albedo = new float[] { m.Albedo.X, m.Albedo.Y, m.Albedo.Z },
                    Roughness = m.Roughness,
                    Metallic = m.Metallic,
                    TexturePath = m.TexturePath
                };
            }

            if (e.Has<Light>())
            {
                var l = e.Get<Light>();
                data.Light = new LightData
                {
                    Type = l.Type.ToString(),
                    Direction = new float[] { l.Direction.X, l.Direction.Y, l.Direction.Z },
                    Position = new float[] { l.Position.X, l.Position.Y, l.Position.Z },
                    Color = new float[] { l.Color.X, l.Color.Y, l.Color.Z },
                    Intensity = l.Intensity,
                    Range = l.Range
                };
            }

            if (e.Has<Camera>())
            {
                var c = e.Get<Camera>();
                data.Camera = new CameraData
                {
                    Position = new float[] { c.Position.X, c.Position.Y, c.Position.Z },
                    Target = new float[] { c.Target.X, c.Target.Y, c.Target.Z },
                    Up = new float[] { c.Up.X, c.Up.Y, c.Up.Z },
                    FieldOfView = c.FieldOfView,
                    AspectRatio = c.AspectRatio,
                    NearPlane = c.NearPlane,
                    FarPlane = c.FarPlane
                };
            }

            entities.Add(data);
        });

        return JsonSerializer.Serialize(entities, JsonOptions);
    }

    public static void SaveToFile(World world, string path)
    {
        var json = SaveToString(world);
        File.WriteAllText(path, json);
    }

    public static int LoadFromString(World world, string json)
    {
        var entities = JsonSerializer.Deserialize<List<SceneEntityData>>(json, JsonOptions);
        if (entities == null) return 0;

        foreach (var data in entities)
        {
            var entity = world.Entity(data.Name);

            if (data.Transform != null)
            {
                var t = data.Transform;
                entity.Set(new Transform(
                    new Vector3(t.Position[0], t.Position[1], t.Position[2]),
                    new Quaternion(t.Rotation[0], t.Rotation[1], t.Rotation[2], t.Rotation[3]),
                    new Vector3(t.Scale[0], t.Scale[1], t.Scale[2])));
            }

            if (data.Material != null)
            {
                var m = data.Material;
                entity.Set(new Material(
                    new Vector3(m.Albedo[0], m.Albedo[1], m.Albedo[2]),
                    m.Roughness, m.Metallic, m.TexturePath));
            }

            if (data.Light != null)
            {
                var l = data.Light;
                if (l.Type == "Directional")
                {
                    entity.Set(Light.Directional(
                        new Vector3(l.Direction[0], l.Direction[1], l.Direction[2]),
                        new Vector3(l.Color[0], l.Color[1], l.Color[2]),
                        l.Intensity));
                }
                else
                {
                    entity.Set(Light.Point(
                        new Vector3(l.Position[0], l.Position[1], l.Position[2]),
                        new Vector3(l.Color[0], l.Color[1], l.Color[2]),
                        l.Intensity, l.Range));
                }
            }

            if (data.Camera != null)
            {
                var c = data.Camera;
                entity.Set(new Camera(
                    new Vector3(c.Position[0], c.Position[1], c.Position[2]),
                    new Vector3(c.Target[0], c.Target[1], c.Target[2]),
                    new Vector3(c.Up[0], c.Up[1], c.Up[2]),
                    c.FieldOfView, c.AspectRatio, c.NearPlane, c.FarPlane));
            }
        }

        return entities.Count;
    }

    public static int LoadFromFile(World world, string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Scene file not found: {path}", path);

        var json = File.ReadAllText(path);
        return LoadFromString(world, json);
    }

    private class SceneEntityData
    {
        public string Name { get; set; } = "";
        public TransformData? Transform { get; set; }
        public MaterialData? Material { get; set; }
        public LightData? Light { get; set; }
        public CameraData? Camera { get; set; }
    }

    private class TransformData
    {
        public float[] Position { get; set; } = Array.Empty<float>();
        public float[] Rotation { get; set; } = Array.Empty<float>();
        public float[] Scale { get; set; } = Array.Empty<float>();
    }

    private class MaterialData
    {
        public float[] Albedo { get; set; } = Array.Empty<float>();
        public float Roughness { get; set; }
        public float Metallic { get; set; }
        public string? TexturePath { get; set; }
    }

    private class LightData
    {
        public string Type { get; set; } = "";
        public float[] Direction { get; set; } = Array.Empty<float>();
        public float[] Position { get; set; } = Array.Empty<float>();
        public float[] Color { get; set; } = Array.Empty<float>();
        public float Intensity { get; set; }
        public float Range { get; set; }
    }

    private class CameraData
    {
        public float[] Position { get; set; } = Array.Empty<float>();
        public float[] Target { get; set; } = Array.Empty<float>();
        public float[] Up { get; set; } = Array.Empty<float>();
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }
    }
}
