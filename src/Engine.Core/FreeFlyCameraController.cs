using System;
using System.Numerics;
using Flecs.NET.Core;
using SDL;
using Engine.Core.Components;

namespace Engine.Core;

/// <summary>
/// First-person / free-fly camera controller.
/// WASD moves on the ground plane, Q/E move up/down, Shift boosts speed.
/// Mouse look while right mouse button is held.
/// </summary>
public sealed class FreeFlyCameraController : ICameraController
{
    private readonly Entity _cameraEntity;
    private float _yaw;
    private float _pitch;
    private float _speed = 3.0f;
    private float _fastSpeed = 8.0f;
    private float _mouseSensitivity = 0.003f;
    private int _lastMouseX;
    private int _lastMouseY;
    private bool _wasRightMouseDown;
    private Vector3 _position;

    public string Name => "FreeFly";

    public FreeFlyCameraController(Entity cameraEntity)
    {
        _cameraEntity = cameraEntity;
        var camera = cameraEntity.Get<Camera>();
        _position = camera.Position;

        var forward = Vector3.Normalize(camera.Target - camera.Position);
        _pitch = MathF.Asin(-forward.Y);
        _yaw = MathF.Atan2(forward.X, forward.Z);
    }

    public void Update(InputMapping input, float deltaTime)
    {
        var move = Vector3.Zero;
        var forward = new Vector3(MathF.Sin(_yaw), 0.0f, MathF.Cos(_yaw));
        var right = new Vector3(MathF.Cos(_yaw), 0.0f, -MathF.Sin(_yaw));
        var up = Vector3.UnitY;

        if (input.IsKeyDown(SDL_Keycode.SDLK_W))
            move += forward;
        if (input.IsKeyDown(SDL_Keycode.SDLK_S))
            move -= forward;
        if (input.IsKeyDown(SDL_Keycode.SDLK_A))
            move -= right;
        if (input.IsKeyDown(SDL_Keycode.SDLK_D))
            move += right;
        if (input.IsKeyDown(SDL_Keycode.SDLK_E))
            move += up;
        if (input.IsKeyDown(SDL_Keycode.SDLK_Q))
            move -= up;

        if (move.LengthSquared() > 0.0f)
        {
            move = Vector3.Normalize(move);
            var speed = input.IsKeyDown(SDL_Keycode.SDLK_LSHIFT) ? _fastSpeed : _speed;
            _position += move * speed * deltaTime;
        }

        if (input.MouseWheelDelta != 0)
        {
            _speed *= 1.0f + input.MouseWheelDelta * 0.1f;
            _speed = Math.Clamp(_speed, 0.5f, 30.0f);
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
                _yaw += dx * _mouseSensitivity;
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
        camera.Position = _position;

        var direction = new Vector3(
            MathF.Cos(_pitch) * MathF.Sin(_yaw),
            -MathF.Sin(_pitch),
            MathF.Cos(_pitch) * MathF.Cos(_yaw));

        camera.Target = _position + direction;
        camera.Up = Vector3.UnitY;
        _cameraEntity.Set(camera);
    }
}
