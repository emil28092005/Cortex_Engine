using System;
using System.Numerics;
using Flecs.NET.Core;
using Engine.Core.Components;

namespace Engine.Core;

/// <summary>
/// Orbit camera controller. Rotates the camera around a fixed target point.
/// Right mouse drag rotates; mouse wheel zooms; WASD moves the target on the ground plane.
/// </summary>
public sealed class OrbitCameraController : ICameraController
{
    private readonly Entity _cameraEntity;
    private Vector3 _target;
    private float _distance;
    private float _yaw;
    private float _pitch;
    private float _speed = 3.0f;
    private float _fastSpeed = 8.0f;
    private float _mouseSensitivity = 0.005f;
    private float _zoomSensitivity = 0.1f;
    private int _lastMouseX;
    private int _lastMouseY;
    private bool _wasRightMouseDown;

    public string Name => "Orbit";

    public OrbitCameraController(Entity cameraEntity, Vector3 target)
    {
        _cameraEntity = cameraEntity;
        _target = target;

        var camera = cameraEntity.Get<Camera>();
        _distance = Vector3.Distance(camera.Position, target);

        var forward = Vector3.Normalize(target - camera.Position);
        _pitch = MathF.Asin(-forward.Y);
        _yaw = MathF.Atan2(forward.X, forward.Z);

        // Clamp pitch to avoid gimbal-lock and sudden flips.
        _pitch = Math.Clamp(_pitch, -MathF.PI / 2.0f + 0.01f, MathF.PI / 2.0f - 0.01f);
    }

    public void Update(IInputState input, float deltaTime)
    {
        var move = Vector3.Zero;
        var forward = new Vector3(MathF.Sin(_yaw), 0.0f, MathF.Cos(_yaw));
        var right = new Vector3(-MathF.Cos(_yaw), 0.0f, MathF.Sin(_yaw));
        var up = Vector3.UnitY;

        if (input.IsKeyDown(Key.W))
            move += forward;
        if (input.IsKeyDown(Key.S))
            move -= forward;
        if (input.IsKeyDown(Key.A))
            move -= right;
        if (input.IsKeyDown(Key.D))
            move += right;
        if (input.IsKeyDown(Key.E))
            move += up;
        if (input.IsKeyDown(Key.Q))
            move -= up;

        if (move.LengthSquared() > 0.0f)
        {
            move = Vector3.Normalize(move);
            var speed = input.IsKeyDown(Key.LeftShift) ? _fastSpeed : _speed;
            _target += move * speed * deltaTime;
        }

        if (input.MouseWheelDelta != 0)
        {
            _distance *= 1.0f - input.MouseWheelDelta * _zoomSensitivity;
            _distance = Math.Clamp(_distance, 1.0f, 200.0f);
        }

        if (input.MouseRight)
        {
            if (!_wasRightMouseDown)
            {
                _lastMouseX = input.MouseX;
                _lastMouseY = input.MouseY;
                _wasRightMouseDown = true;
            }
            else
            {
                var dx = input.MouseX - _lastMouseX;
                var dy = input.MouseY - _lastMouseY;
                _yaw -= dx * _mouseSensitivity;
                _pitch += dy * _mouseSensitivity;
                _pitch = Math.Clamp(_pitch, -MathF.PI / 2.0f + 0.01f, MathF.PI / 2.0f - 0.01f);
                _lastMouseX = input.MouseX;
                _lastMouseY = input.MouseY;
            }
        }
        else
        {
            _wasRightMouseDown = false;
        }

        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var camera = _cameraEntity.Get<Camera>();

        var direction = new Vector3(
            MathF.Cos(_pitch) * MathF.Sin(_yaw),
            -MathF.Sin(_pitch),
            MathF.Cos(_pitch) * MathF.Cos(_yaw));

        camera.Position = _target - direction * _distance;
        camera.Target = _target;
        camera.Up = Vector3.UnitY;
        _cameraEntity.Set(camera);
    }
}
