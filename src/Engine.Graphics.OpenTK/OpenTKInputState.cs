using System.Collections.Generic;
using Engine.Core;
using OpenTK.Windowing.GraphicsLibraryFramework;
using EngineKey = Engine.Core.Key;
using OpenTKKey = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace Engine.Graphics.OpenTK;

/// <summary>
/// OpenTK input-backed implementation of IInputState.
/// </summary>
public sealed class OpenTKInputState : IInputState
{
    private readonly HashSet<EngineKey> _keysDown = new();
    private readonly HashSet<EngineKey> _keysPressed = new();
    private readonly HashSet<EngineKey> _keysReleased = new();
    private readonly HashSet<EngineKey> _prevDown = new();

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }
    public bool MouseLeft { get; private set; }
    public bool MouseRight { get; private set; }
    public bool MouseMiddle { get; private set; }
    public float MouseWheelDelta { get; private set; }

    public void Update(KeyboardState kb, MouseState ms)
    {
        MouseX = (int)ms.Position.X;
        MouseY = (int)ms.Position.Y;
        MouseLeft = ms.IsButtonDown(MouseButton.Left);
        MouseRight = ms.IsButtonDown(MouseButton.Right);
        MouseMiddle = ms.IsButtonDown(MouseButton.Middle);
        MouseWheelDelta = (float)ms.ScrollDelta.Y;

        // Compute pressed/released edges
        _keysPressed.Clear();
        _keysReleased.Clear();

        var currentKeys = new HashSet<EngineKey>();
        foreach (var openTkKey in AllKeys)
        {
            if (kb.IsKeyDown(openTkKey))
            {
                var k = MapKey(openTkKey);
                if (k == EngineKey.Unknown) continue;
                currentKeys.Add(k);
                if (!_prevDown.Contains(k))
                    _keysPressed.Add(k);
            }
        }

        foreach (var k in _prevDown)
            if (!currentKeys.Contains(k))
                _keysReleased.Add(k);

        _keysDown.Clear();
        _keysDown.UnionWith(currentKeys);
        _prevDown.Clear();
        _prevDown.UnionWith(currentKeys);
    }

    public void BeginFrame()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();
        MouseWheelDelta = 0;
    }

    public bool IsKeyDown(EngineKey key) => _keysDown.Contains(key);
    public bool IsKeyPressed(EngineKey key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(EngineKey key) => _keysReleased.Contains(key);

    private static readonly OpenTKKey[] AllKeys =
    {
        OpenTKKey.W, OpenTKKey.A, OpenTKKey.S, OpenTKKey.D,
        OpenTKKey.Q, OpenTKKey.E, OpenTKKey.F,
        OpenTKKey.LeftShift,
        OpenTKKey.Space, OpenTKKey.Escape, OpenTKKey.Enter,
        OpenTKKey.Tab, OpenTKKey.Backspace,
        OpenTKKey.Up, OpenTKKey.Down, OpenTKKey.Left, OpenTKKey.Right,
    };

    private static EngineKey MapKey(OpenTKKey key) => key switch
    {
        OpenTKKey.W => EngineKey.W,
        OpenTKKey.A => EngineKey.A,
        OpenTKKey.S => EngineKey.S,
        OpenTKKey.D => EngineKey.D,
        OpenTKKey.Q => EngineKey.Q,
        OpenTKKey.E => EngineKey.E,
        OpenTKKey.F => EngineKey.F,
        OpenTKKey.LeftShift => EngineKey.LeftShift,
        OpenTKKey.Space => EngineKey.Space,
        OpenTKKey.Escape => EngineKey.Escape,
        OpenTKKey.Enter => EngineKey.Enter,
        OpenTKKey.Tab => EngineKey.Tab,
        OpenTKKey.Backspace => EngineKey.Backspace,
        OpenTKKey.Up => EngineKey.Up,
        OpenTKKey.Down => EngineKey.Down,
        OpenTKKey.Left => EngineKey.Left,
        OpenTKKey.Right => EngineKey.Right,
        _ => EngineKey.Unknown,
    };
}
