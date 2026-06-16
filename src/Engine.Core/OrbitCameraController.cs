using System;
using System.Numerics;
using Flecs.NET.Core;

namespace Engine.Core;

/// <summary>
/// Orbit camera controller. Right mouse drag rotates around the target,
/// mouse wheel zooms in/out.
/// </summary>
public sealed class OrbitCameraController
{
    private readonly Entity _cameraEntity;
    private float _distance;
    private float _yaw;
    private float _pitch;
    private readonly Vector3 _target;
    private int _lastMouseX;
    private int _lastMouseY;
    private bool _isDragging;

    public OrbitCameraController(Entity cameraEntity, Vector3? target = null)
    {
        _cameraEntity = cameraEntity;
        var camera = cameraEntity.Get<Components.Camera>();
        _target = target ?? Vector3.Zero;
        _distance = Vector3.Distance(camera.Position, _target);

        var direction = Vector3.Normalize(camera.Position - _target);
        _pitch = MathF.Asin(-direction.Y);
        _yaw = MathF.Atan2(direction.X, direction.Z);
    }

    public void Update(InputMapping input, float deltaTime)
    {
        if (input.MouseRight)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                _lastMouseX = input.MouseX;
                _lastMouseY = input.MouseY;
            }
            else
            {
                var dx = input.MouseX - _lastMouseX;
                var dy = input.MouseY - _lastMouseY;
                _yaw -= dx * 0.005f;
                _pitch -= dy * 0.005f;
                _pitch = Math.Clamp(_pitch, -MathF.PI / 2.0f + 0.1f, MathF.PI / 2.0f - 0.1f);
                _lastMouseX = input.MouseX;
                _lastMouseY = input.MouseY;
            }
        }
        else
        {
            _isDragging = false;
        }

        if (input.MouseWheelDelta != 0)
        {
            _distance *= 1.0f - input.MouseWheelDelta * 0.1f;
            _distance = Math.Clamp(_distance, 0.5f, 50.0f);
        }

        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var x = _distance * MathF.Cos(_pitch) * MathF.Sin(_yaw);
        var y = _distance * MathF.Sin(_pitch);
        var z = _distance * MathF.Cos(_pitch) * MathF.Cos(_yaw);

        var camera = _cameraEntity.Get<Components.Camera>();
        camera.Position = _target + new Vector3(x, y, z);
        camera.Target = _target;
        _cameraEntity.Set(camera);
    }
}
