using System.Collections.Generic;
using Engine.Core;
using Silk.NET.Input;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLInputState : IInputState
{
    private IKeyboard? _keyboard;
    private IMouse? _mouse;
    private readonly HashSet<Engine.Core.Key> _keysDown = new();
    private readonly HashSet<Engine.Core.Key> _keysPressed = new();
    private readonly HashSet<Engine.Core.Key> _keysReleased = new();
    private float _mouseWheelDelta;
    private bool _mouseLeft, _mouseRight, _mouseMiddle;

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }
    public bool MouseLeft => _mouseLeft;
    public bool MouseRight => _mouseRight;
    public bool MouseMiddle => _mouseMiddle;
    public float MouseWheelDelta => _mouseWheelDelta;

    public void Initialize(IInputContext input)
    {
        if (input.Keyboards.Count > 0)
        {
            _keyboard = input.Keyboards[0];
            _keyboard.KeyDown += (key, _) =>
            {
                var k = SilkToKey(key);
                if (k == Engine.Core.Key.Unknown) return;
                if (!_keysDown.Contains(k)) _keysPressed.Add(k);
                _keysDown.Add(k);
            };
            _keyboard.KeyUp += (key, _) =>
            {
                var k = SilkToKey(key);
                if (k == Engine.Core.Key.Unknown) return;
                _keysDown.Remove(k);
                _keysReleased.Add(k);
            };
        }
        if (input.Mice.Count > 0)
        {
            _mouse = input.Mice[0];
            _mouse.MouseMove += m => { MouseX = (int)m.Position.X; MouseY = (int)m.Position.Y; };
            _mouse.MouseDown += btn => {
                if (btn == SilkMouseButton.Left) _mouseLeft = true;
                if (btn == SilkMouseButton.Right) _mouseRight = true;
                if (btn == SilkMouseButton.Middle) _mouseMiddle = true;
            };
            _mouse.MouseUp += btn => {
                if (btn == SilkMouseButton.Left) _mouseLeft = false;
                if (btn == SilkMouseButton.Right) _mouseRight = false;
                if (btn == SilkMouseButton.Middle) _mouseMiddle = false;
            };
            _mouse.Scroll += (_, y) => _mouseWheelDelta += (float)y;
        }
    }

    public void Poll() { }

    public void BeginFrame()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();
        _mouseWheelDelta = 0;
    }

    public bool IsKeyDown(Engine.Core.Key key) => _keysDown.Contains(key);
    public bool IsKeyPressed(Engine.Core.Key key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(Engine.Core.Key key) => _keysReleased.Contains(key);

    private static Engine.Core.Key SilkToKey(Silk.NET.Input.Key key) => key switch
    {
        Silk.NET.Input.Key.Space => Engine.Core.Key.Space,
        Silk.NET.Input.Key.Escape => Engine.Core.Key.Escape,
        Silk.NET.Input.Key.Enter => Engine.Core.Key.Enter,
        Silk.NET.Input.Key.W => Engine.Core.Key.W,
        Silk.NET.Input.Key.A => Engine.Core.Key.A,
        Silk.NET.Input.Key.S => Engine.Core.Key.S,
        Silk.NET.Input.Key.D => Engine.Core.Key.D,
        Silk.NET.Input.Key.Q => Engine.Core.Key.Q,
        Silk.NET.Input.Key.E => Engine.Core.Key.E,
        Silk.NET.Input.Key.F => Engine.Core.Key.F,
        Silk.NET.Input.Key.ShiftLeft => Engine.Core.Key.LeftShift,
        _ => Engine.Core.Key.Unknown,
    };
}
