using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Flecs.NET.Core;

namespace Engine.Tests;

/// <summary>
/// Test double for IInputState — set properties before calling controller.Update().
/// </summary>
internal sealed class FakeInputState : IInputState
{
    private readonly HashSet<Key> _down = new();
    private readonly HashSet<Key> _pressed = new();

    public int MouseX { get; set; }
    public int MouseY { get; set; }
    public bool MouseLeft { get; set; }
    public bool MouseRight { get; set; }
    public bool MouseMiddle { get; set; }
    public float MouseWheelDelta { get; set; }

    public void SetKeyDown(Key key) => _down.Add(key);
    public void SetKeyPressed(Key key)
    {
        _down.Add(key);
        _pressed.Add(key);
    }

    public bool IsKeyDown(Key key) => _down.Contains(key);
    public bool IsKeyPressed(Key key) => _pressed.Contains(key);
    public bool IsKeyReleased(Key key) => false;

    public void BeginFrame()
    {
        _pressed.Clear();
        MouseWheelDelta = 0;
    }
}

public class FreeFlyCameraControllerTests
{
    private static (FreeFlyCameraController, Entity) CreateController(float yaw = 0f, float pitch = 0f)
    {
        var world = World.Create();
        var pos = new Vector3(0, 1, -10);
        var dir = new Vector3(
            MathF.Cos(pitch) * MathF.Sin(yaw),
            -MathF.Sin(pitch),
            MathF.Cos(pitch) * MathF.Cos(yaw));
        var cam = new Camera(pos, pos + dir, Vector3.UnitY);
        var entity = world.Entity("TestCamera").Set(cam);
        return (new FreeFlyCameraController(entity), entity);
    }

    [Fact]
    public void W_Moves_Forward()
    {
        var (controller, entity) = CreateController(yaw: 0f);
        var input = new FakeInputState();
        input.SetKeyDown(Key.W);

        controller.Update(input, 1.0f);

        var cam = entity.Get<Camera>();
        Assert.True(cam.Position.Z > -10f);
    }

    [Fact]
    public void S_Moves_Backward()
    {
        var (controller, entity) = CreateController(yaw: 0f);
        var input = new FakeInputState();
        input.SetKeyDown(Key.S);

        controller.Update(input, 1.0f);

        var cam = entity.Get<Camera>();
        Assert.True(cam.Position.Z < -10f);
    }

    [Fact]
    public void Shift_Boosts_Speed()
    {
        var (controller, entity) = CreateController(yaw: 0f);
        var inputNormal = new FakeInputState();
        inputNormal.SetKeyDown(Key.W);
        controller.Update(inputNormal, 1.0f);
        var normalPos = entity.Get<Camera>().Position;

        var (controller2, entity2) = CreateController(yaw: 0f);
        var inputBoost = new FakeInputState();
        inputBoost.SetKeyDown(Key.W);
        inputBoost.SetKeyDown(Key.LeftShift);
        controller2.Update(inputBoost, 1.0f);
        var boostPos = entity2.Get<Camera>().Position;

        Assert.True(boostPos.Z > normalPos.Z);
    }

    [Fact]
    public void Q_Moves_Down()
    {
        var (controller, entity) = CreateController();
        var input = new FakeInputState();
        input.SetKeyDown(Key.Q);

        controller.Update(input, 1.0f);

        var cam = entity.Get<Camera>();
        Assert.True(cam.Position.Y < 1f);
    }

    [Fact]
    public void E_Moves_Up()
    {
        var (controller, entity) = CreateController();
        var input = new FakeInputState();
        input.SetKeyDown(Key.E);

        controller.Update(input, 1.0f);

        var cam = entity.Get<Camera>();
        Assert.True(cam.Position.Y > 1f);
    }

    [Fact]
    public void Mouse_Wheel_Adjusts_Speed()
    {
        var (controller, entity) = CreateController(yaw: 0f);
        var input = new FakeInputState();
        input.SetKeyDown(Key.W);
        input.MouseWheelDelta = 10f;

        controller.Update(input, 0.1f);

        input.BeginFrame();
        input.SetKeyDown(Key.W);
        controller.Update(input, 1.0f);
        var cam = entity.Get<Camera>();

        // With boosted speed, movement should be much larger than default 3 units
        Assert.True(cam.Position.Z > -7f);
    }

    [Fact]
    public void Target_Follows_Position()
    {
        var (controller, entity) = CreateController(yaw: 0f);
        var input = new FakeInputState();
        input.SetKeyDown(Key.W);

        controller.Update(input, 1.0f);

        var cam = entity.Get<Camera>();
        var dir = Vector3.Normalize(cam.Target - cam.Position);
        Assert.Equal(0f, dir.X, 0.01f);
        Assert.Equal(0f, dir.Y, 0.01f);
    }
}

public class OrbitCameraControllerTests
{
    private static (OrbitCameraController, Entity) CreateController()
    {
        var world = World.Create();
        var target = new Vector3(0, 0.5f, 0);
        var pos = new Vector3(0, 0.5f, -10);
        var cam = new Camera(pos, target, Vector3.UnitY);
        var entity = world.Entity("TestOrbitCamera").Set(cam);
        return (new OrbitCameraController(entity, target), entity);
    }

    [Fact]
    public void W_Moves_Target_Forward()
    {
        var (controller, entity) = CreateController();
        var input = new FakeInputState();
        input.SetKeyDown(Key.W);

        controller.Update(input, 1.0f);

        var cam = entity.Get<Camera>();
        Assert.True(cam.Target.Z > 0f);
    }

    [Fact]
    public void Zoom_Decreases_Distance()
    {
        var (controller, entity) = CreateController();
        var input = new FakeInputState();
        input.MouseWheelDelta = 1f;

        controller.Update(input, 0.1f);

        var cam = entity.Get<Camera>();
        var dist = Vector3.Distance(cam.Position, cam.Target);
        Assert.True(dist < 10f);
    }

    [Fact]
    public void Camera_Position_Orbits_Target()
    {
        var (controller, entity) = CreateController();
        var input = new FakeInputState();
        input.MouseRight = true;
        input.MouseX = 100;
        input.MouseY = 100;

        controller.Update(input, 0.1f);

        input.BeginFrame();
        input.MouseRight = true;
        input.MouseX = 200;
        input.MouseY = 100;
        controller.Update(input, 0.1f);

        var cam = entity.Get<Camera>();
        var dist = Vector3.Distance(cam.Position, cam.Target);
        Assert.Equal(10f, dist, 1f);
    }

    [Fact]
    public void Target_Stays_At_Ground_Level_With_WASD()
    {
        var (controller, entity) = CreateController();
        var input = new FakeInputState();
        input.SetKeyDown(Key.W);

        controller.Update(input, 1.0f);

        var cam = entity.Get<Camera>();
        Assert.Equal(0.5f, cam.Target.Y, 0.001f);
    }
}
