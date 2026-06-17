using System.Numerics;
using Engine.Core;
using Flecs.NET.Core;
using ImGuiNET;
using rlImGui_cs;
using EngineTransform = Engine.Core.Components.Transform;
using EngineMaterial = Engine.Core.Components.Material;
using EngineLight = Engine.Core.Components.Light;
using EngineCamera = Engine.Core.Components.Camera;

namespace Engine.Graphics.RaylibBackend;

/// <summary>
/// Dear ImGui integration for the Raylib backend.
/// Provides an entity inspector, hierarchy panel, and debug overlay.
/// </summary>
public sealed class ImGuiLayer : IDisposable
{
    private bool _initialized;
    private bool _disposed;
    private string _selectedEntity = "";
    private float[] _fpsHistory = new float[120];
    private int _fpsHistoryIndex;

    private World? _world;
    private Timing? _timing;
    private int _fps;

    /// <summary>
    /// Set per-frame data before calling RenderImGuiUI.
    /// </summary>
    public void SetFrameData(World world, Timing timing, int fps)
    {
        _world = world;
        _timing = timing;
        _fps = fps;
    }

    /// <summary>
    /// Render the ImGui UI. Called internally by RaylibRenderer between Begin() and End().
    /// </summary>
    internal void RenderImGuiUI()
    {
        if (!_initialized || _world is not { } world || _timing is not { } timing) return;
        RenderDebugOverlay(timing, _fps);
        RenderHierarchy(world);
        RenderInspector(world);
    }

    public void Initialize()
    {
        if (_initialized) return;
        rlImGui.Setup(true);
        _initialized = true;
    }

    /// <summary>
    /// Call at the start of the frame (after EndMode3D, before EndDrawing).
    /// Begins the ImGui render pass.
    /// </summary>
    public void Begin()
    {
        if (!_initialized) return;
        rlImGui.Begin();
    }

    /// <summary>
    /// Call at the end of the frame (before EndDrawing).
    /// Ends the ImGui render pass and renders all ImGui draw data.
    /// </summary>
    public void End()
    {
        if (!_initialized) return;
        rlImGui.End();
    }

    private void RenderDebugOverlay(Timing timing, int fps)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.7f);

        if (!ImGui.Begin("Debug", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.End();
            return;
        }

        ImGui.Text($"FPS: {fps}");
        ImGui.Text($"Frame: {timing.DeltaTime * 1000.0f:F2} ms");
        ImGui.Text($"Time: {timing.TotalTime:F1} s");

        _fpsHistory[_fpsHistoryIndex] = fps;
        _fpsHistoryIndex = (_fpsHistoryIndex + 1) % _fpsHistory.Length;

        ImGui.PlotLines("##fps", ref _fpsHistory[0], _fpsHistory.Length, _fpsHistoryIndex, "", 0, 200, new Vector2(200, 40));

        ImGui.End();
    }

    private void RenderHierarchy(World world)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 120), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(250, 400), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Hierarchy"))
        {
            ImGui.End();
            return;
        }

        world.Each((Entity e, ref EngineTransform _) =>
        {
            var name = e.Name();
            if (string.IsNullOrEmpty(name))
                return;

            var isSelected = name == _selectedEntity;
            if (ImGui.Selectable(name, isSelected))
                _selectedEntity = name;
        });

        ImGui.End();
    }

    private void RenderInspector(World world)
    {
        ImGui.SetNextWindowPos(new Vector2(270, 120), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(300, 400), ImGuiCond.FirstUseEver);

        if (!ImGui.Begin("Inspector"))
        {
            ImGui.End();
            return;
        }

        if (string.IsNullOrEmpty(_selectedEntity))
        {
            ImGui.TextDisabled("Select an entity from the Hierarchy");
            ImGui.End();
            return;
        }

        var entity = world.Lookup(_selectedEntity);
        if ((ulong)entity.Id == 0)
        {
            ImGui.TextDisabled($"Entity '{_selectedEntity}' not found");
            ImGui.End();
            return;
        }

        ImGui.Text($"Entity: {_selectedEntity}");
        ImGui.Separator();

        if (entity.Has<EngineTransform>())
        {
            var t = entity.Get<EngineTransform>();

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var pos = t.Position;
                if (ImGui.DragFloat3("Position", ref pos, 0.1f))
                {
                    t.Position = pos;
                    entity.Set(t);
                }

                var scale = t.Scale;
                if (ImGui.DragFloat3("Scale", ref scale, 0.1f, 0.01f, 100f))
                {
                    t.Scale = scale;
                    entity.Set(t);
                }

                var euler = ToEuler(t.Rotation);
                if (ImGui.DragFloat3("Rotation", ref euler, 1.0f, -180f, 180f))
                {
                    t.Rotation = FromEuler(euler);
                    entity.Set(t);
                }
            }
        }

        if (entity.Has<EngineMaterial>())
        {
            var m = entity.Get<EngineMaterial>();

            if (ImGui.CollapsingHeader("Material", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var albedo = m.Albedo;
                if (ImGui.ColorEdit3("Albedo", ref albedo))
                {
                    m.Albedo = albedo;
                    entity.Set(m);
                }

                var rough = m.Roughness;
                if (ImGui.SliderFloat("Roughness", ref rough, 0.0f, 1.0f))
                {
                    m.Roughness = rough;
                    entity.Set(m);
                }

                var metal = m.Metallic;
                if (ImGui.SliderFloat("Metallic", ref metal, 0.0f, 1.0f))
                {
                    m.Metallic = metal;
                    entity.Set(m);
                }

                if (m.HasTexture)
                    ImGui.Text($"Texture: {m.TexturePath}");
                else
                    ImGui.TextDisabled("No texture");
            }
        }

        if (entity.Has<EngineLight>())
        {
            var l = entity.Get<EngineLight>();

            if (ImGui.CollapsingHeader("Light", ImGuiTreeNodeFlags.DefaultOpen))
            {
                var color = l.Color;
                if (ImGui.ColorEdit3("Color", ref color))
                {
                    l.Color = color;
                    entity.Set(l);
                }

                var intensity = l.Intensity;
                if (ImGui.SliderFloat("Intensity", ref intensity, 0.0f, 5.0f))
                {
                    l.Intensity = intensity;
                    entity.Set(l);
                }

                var dir = l.Direction;
                if (ImGui.DragFloat3("Direction", ref dir, 0.01f, -1f, 1f))
                {
                    l.Direction = dir;
                    entity.Set(l);
                }
            }
        }

        if (entity.Has<EngineCamera>())
        {
            var c = entity.Get<EngineCamera>();

            if (ImGui.CollapsingHeader("Camera"))
            {
                var pos = c.Position;
                if (ImGui.DragFloat3("Position", ref pos, 0.1f))
                {
                    c.Position = pos;
                    entity.Set(c);
                }

                var target = c.Target;
                if (ImGui.DragFloat3("Target", ref target, 0.1f))
                {
                    c.Target = target;
                    entity.Set(c);
                }

                var fov = c.FieldOfView * 180.0f / MathF.PI;
                if (ImGui.SliderFloat("FOV", ref fov, 5f, 120f))
                {
                    c.FieldOfView = fov * MathF.PI / 180.0f;
                    entity.Set(c);
                }
            }
        }

        ImGui.End();
    }

    private static Vector3 ToEuler(Quaternion q)
    {
        var pitch = MathF.Atan2(2 * (q.W * q.X + q.Y * q.Z), 1 - 2 * (q.X * q.X + q.Y * q.Y));
        var yaw = MathF.Asin(Math.Clamp(2 * (q.W * q.Y - q.Z * q.X), -1f, 1f));
        var roll = MathF.Atan2(2 * (q.W * q.Z + q.X * q.Y), 1 - 2 * (q.Y * q.Y + q.Z * q.Z));
        return new Vector3(pitch * 180f / MathF.PI, yaw * 180f / MathF.PI, roll * 180f / MathF.PI);
    }

    private static Quaternion FromEuler(Vector3 euler)
    {
        var rad = euler * MathF.PI / 180f;
        return Quaternion.CreateFromYawPitchRoll(rad.Y, rad.X, rad.Z);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_initialized)
            rlImGui.Shutdown();
    }
}
