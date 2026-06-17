using System;
using System.Collections.Generic;
using Engine.Core;
using Raylib_cs;

namespace Engine.Graphics.RaylibBackend;

/// <summary>
/// Raylib-backed implementation of <see cref="IInputState"/>.
/// Queries Raylib's input functions directly each frame.
/// </summary>
public sealed class RaylibInputState : IInputState
{
    private static readonly Key[] _allKeys = (Key[])Enum.GetValues(typeof(Key));

    private readonly HashSet<Key> _keysDown = new();
    private readonly HashSet<Key> _keysPressed = new();
    private readonly HashSet<Key> _keysReleased = new();

    private float _mouseWheelDelta;
    private bool _wheelConsumed;

    public int MouseX => Raylib.GetMouseX();
    public int MouseY => Raylib.GetMouseY();
    public bool MouseLeft => Raylib.IsMouseButtonDown(MouseButton.Left);
    public bool MouseRight => Raylib.IsMouseButtonDown(MouseButton.Right);
    public bool MouseMiddle => Raylib.IsMouseButtonDown(MouseButton.Middle);

    public float MouseWheelDelta
    {
        get
        {
            if (!_wheelConsumed)
            {
                _mouseWheelDelta = Raylib.GetMouseWheelMove();
                _wheelConsumed = true;
            }
            return _mouseWheelDelta;
        }
    }

    public void BeginFrame()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();
        _mouseWheelDelta = 0;
        _wheelConsumed = false;
    }

    /// <summary>
    /// Poll Raylib input and update edge state. Called by <see cref="RaylibWindow.PumpEvents"/>.
    /// </summary>
    public void Poll()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();

        foreach (var key in _allKeys)
        {
            if (key == Key.Unknown) continue;
            var rlKey = ToRaylibKey(key);
            if (rlKey == KeyboardKey.Null) continue;

            var isDown = Raylib.IsKeyDown(rlKey);
            var wasDown = _keysDown.Contains(key);

            if (isDown && !wasDown)
                _keysPressed.Add(key);
            if (!isDown && wasDown)
                _keysReleased.Add(key);

            if (isDown)
                _keysDown.Add(key);
            else
                _keysDown.Remove(key);
        }
    }

    public bool IsKeyDown(Key key) => _keysDown.Contains(key);
    public bool IsKeyPressed(Key key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(Key key) => _keysReleased.Contains(key);

    private static KeyboardKey ToRaylibKey(Key key) => key switch
    {
        Key.Space => KeyboardKey.Space,
        Key.Escape => KeyboardKey.Escape,
        Key.Enter => KeyboardKey.Enter,
        Key.Tab => KeyboardKey.Tab,
        Key.Backspace => KeyboardKey.Backspace,
        Key.Insert => KeyboardKey.Insert,
        Key.Delete => KeyboardKey.Delete,
        Key.Home => KeyboardKey.Home,
        Key.End => KeyboardKey.End,
        Key.PageUp => KeyboardKey.PageUp,
        Key.PageDown => KeyboardKey.PageDown,
        Key.Left => KeyboardKey.Left,
        Key.Right => KeyboardKey.Right,
        Key.Up => KeyboardKey.Up,
        Key.Down => KeyboardKey.Down,
        Key.A => KeyboardKey.A,
        Key.B => KeyboardKey.B,
        Key.C => KeyboardKey.C,
        Key.D => KeyboardKey.D,
        Key.E => KeyboardKey.E,
        Key.F => KeyboardKey.F,
        Key.G => KeyboardKey.G,
        Key.H => KeyboardKey.H,
        Key.I => KeyboardKey.I,
        Key.J => KeyboardKey.J,
        Key.K => KeyboardKey.K,
        Key.L => KeyboardKey.L,
        Key.M => KeyboardKey.M,
        Key.N => KeyboardKey.N,
        Key.O => KeyboardKey.O,
        Key.P => KeyboardKey.P,
        Key.Q => KeyboardKey.Q,
        Key.R => KeyboardKey.R,
        Key.S => KeyboardKey.S,
        Key.T => KeyboardKey.T,
        Key.U => KeyboardKey.U,
        Key.V => KeyboardKey.V,
        Key.W => KeyboardKey.W,
        Key.X => KeyboardKey.X,
        Key.Y => KeyboardKey.Y,
        Key.Z => KeyboardKey.Z,
        Key.Zero => KeyboardKey.Zero,
        Key.One => KeyboardKey.One,
        Key.Two => KeyboardKey.Two,
        Key.Three => KeyboardKey.Three,
        Key.Four => KeyboardKey.Four,
        Key.Five => KeyboardKey.Five,
        Key.Six => KeyboardKey.Six,
        Key.Seven => KeyboardKey.Seven,
        Key.Eight => KeyboardKey.Eight,
        Key.Nine => KeyboardKey.Nine,
        Key.F1 => KeyboardKey.F1,
        Key.F2 => KeyboardKey.F2,
        Key.F3 => KeyboardKey.F3,
        Key.F4 => KeyboardKey.F4,
        Key.F5 => KeyboardKey.F5,
        Key.F6 => KeyboardKey.F6,
        Key.F7 => KeyboardKey.F7,
        Key.F8 => KeyboardKey.F8,
        Key.F9 => KeyboardKey.F9,
        Key.F10 => KeyboardKey.F10,
        Key.F11 => KeyboardKey.F11,
        Key.F12 => KeyboardKey.F12,
        Key.LeftShift => KeyboardKey.LeftShift,
        Key.LeftControl => KeyboardKey.LeftControl,
        Key.LeftAlt => KeyboardKey.LeftAlt,
        Key.RightShift => KeyboardKey.RightShift,
        Key.RightControl => KeyboardKey.RightControl,
        Key.RightAlt => KeyboardKey.RightAlt,
        _ => KeyboardKey.Null,
    };
}
