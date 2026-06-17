using System.Collections.Generic;
using SDL;

namespace Engine.Core;

/// <summary>
/// SDL3-backed implementation of <see cref="IInputState"/>.
/// Populated by polling SDL events via <see cref="ProcessEvent"/> once per frame.
/// </summary>
public sealed class InputMapping : IInputState
{
    private readonly HashSet<Key> _keysPressed = new();
    private readonly HashSet<Key> _keysDown = new();
    private readonly HashSet<Key> _keysReleased = new();

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }
    public bool MouseLeft { get; private set; }
    public bool MouseRight { get; private set; }
    public bool MouseMiddle { get; private set; }
    public float MouseWheelDelta { get; private set; }

    public void BeginFrame()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();
        MouseWheelDelta = 0;
    }

    public void ProcessEvent(SDL_Event evt)
    {
        switch ((SDL_EventType)evt.type)
        {
            case SDL_EventType.SDL_EVENT_KEY_DOWN:
            {
                var key = SdlKeyMap.ToKey((SDL_Keycode)evt.key.key);
                if (key == Key.Unknown) break;
                if (!_keysDown.Contains(key))
                    _keysPressed.Add(key);
                _keysDown.Add(key);
                break;
            }

            case SDL_EventType.SDL_EVENT_KEY_UP:
            {
                var key = SdlKeyMap.ToKey((SDL_Keycode)evt.key.key);
                if (key == Key.Unknown) break;
                _keysDown.Remove(key);
                _keysReleased.Add(key);
                break;
            }

            case SDL_EventType.SDL_EVENT_MOUSE_MOTION:
                MouseX = (int)evt.motion.x;
                MouseY = (int)evt.motion.y;
                break;

            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                SetMouseButton(evt.button.button, true);
                break;

            case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
                SetMouseButton(evt.button.button, false);
                break;

            case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:
                MouseWheelDelta += evt.wheel.y;
                break;
        }
    }

    public bool IsKeyDown(Key key) => _keysDown.Contains(key);
    public bool IsKeyPressed(Key key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(Key key) => _keysReleased.Contains(key);

    private void SetMouseButton(byte button, bool pressed)
    {
        switch (button)
        {
            case 1: MouseLeft = pressed; break;
            case 2: MouseMiddle = pressed; break;
            case 3: MouseRight = pressed; break;
        }
    }
}

/// <summary>
/// Maps SDL3 keycodes to the backend-agnostic <see cref="Key"/> enum.
/// </summary>
internal static class SdlKeyMap
{
    private static readonly Dictionary<SDL_Keycode, Key> _map = new()
    {
        { SDL_Keycode.SDLK_SPACE, Key.Space },
        { SDL_Keycode.SDLK_ESCAPE, Key.Escape },
        { SDL_Keycode.SDLK_RETURN, Key.Enter },
        { SDL_Keycode.SDLK_TAB, Key.Tab },
        { SDL_Keycode.SDLK_BACKSPACE, Key.Backspace },
        { SDL_Keycode.SDLK_INSERT, Key.Insert },
        { SDL_Keycode.SDLK_DELETE, Key.Delete },
        { SDL_Keycode.SDLK_HOME, Key.Home },
        { SDL_Keycode.SDLK_END, Key.End },
        { SDL_Keycode.SDLK_PAGEUP, Key.PageUp },
        { SDL_Keycode.SDLK_PAGEDOWN, Key.PageDown },
        { SDL_Keycode.SDLK_LEFT, Key.Left },
        { SDL_Keycode.SDLK_RIGHT, Key.Right },
        { SDL_Keycode.SDLK_UP, Key.Up },
        { SDL_Keycode.SDLK_DOWN, Key.Down },
        { SDL_Keycode.SDLK_A, Key.A },
        { SDL_Keycode.SDLK_B, Key.B },
        { SDL_Keycode.SDLK_C, Key.C },
        { SDL_Keycode.SDLK_D, Key.D },
        { SDL_Keycode.SDLK_E, Key.E },
        { SDL_Keycode.SDLK_F, Key.F },
        { SDL_Keycode.SDLK_G, Key.G },
        { SDL_Keycode.SDLK_H, Key.H },
        { SDL_Keycode.SDLK_I, Key.I },
        { SDL_Keycode.SDLK_J, Key.J },
        { SDL_Keycode.SDLK_K, Key.K },
        { SDL_Keycode.SDLK_L, Key.L },
        { SDL_Keycode.SDLK_M, Key.M },
        { SDL_Keycode.SDLK_N, Key.N },
        { SDL_Keycode.SDLK_O, Key.O },
        { SDL_Keycode.SDLK_P, Key.P },
        { SDL_Keycode.SDLK_Q, Key.Q },
        { SDL_Keycode.SDLK_R, Key.R },
        { SDL_Keycode.SDLK_S, Key.S },
        { SDL_Keycode.SDLK_T, Key.T },
        { SDL_Keycode.SDLK_U, Key.U },
        { SDL_Keycode.SDLK_V, Key.V },
        { SDL_Keycode.SDLK_W, Key.W },
        { SDL_Keycode.SDLK_X, Key.X },
        { SDL_Keycode.SDLK_Y, Key.Y },
        { SDL_Keycode.SDLK_Z, Key.Z },
        { SDL_Keycode.SDLK_0, Key.Zero },
        { SDL_Keycode.SDLK_1, Key.One },
        { SDL_Keycode.SDLK_2, Key.Two },
        { SDL_Keycode.SDLK_3, Key.Three },
        { SDL_Keycode.SDLK_4, Key.Four },
        { SDL_Keycode.SDLK_5, Key.Five },
        { SDL_Keycode.SDLK_6, Key.Six },
        { SDL_Keycode.SDLK_7, Key.Seven },
        { SDL_Keycode.SDLK_8, Key.Eight },
        { SDL_Keycode.SDLK_9, Key.Nine },
        { SDL_Keycode.SDLK_F1, Key.F1 },
        { SDL_Keycode.SDLK_F2, Key.F2 },
        { SDL_Keycode.SDLK_F3, Key.F3 },
        { SDL_Keycode.SDLK_F4, Key.F4 },
        { SDL_Keycode.SDLK_F5, Key.F5 },
        { SDL_Keycode.SDLK_F6, Key.F6 },
        { SDL_Keycode.SDLK_F7, Key.F7 },
        { SDL_Keycode.SDLK_F8, Key.F8 },
        { SDL_Keycode.SDLK_F9, Key.F9 },
        { SDL_Keycode.SDLK_F10, Key.F10 },
        { SDL_Keycode.SDLK_F11, Key.F11 },
        { SDL_Keycode.SDLK_F12, Key.F12 },
        { SDL_Keycode.SDLK_LSHIFT, Key.LeftShift },
        { SDL_Keycode.SDLK_LCTRL, Key.LeftControl },
        { SDL_Keycode.SDLK_LALT, Key.LeftAlt },
        { SDL_Keycode.SDLK_RSHIFT, Key.RightShift },
        { SDL_Keycode.SDLK_RCTRL, Key.RightControl },
        { SDL_Keycode.SDLK_RALT, Key.RightAlt },
    };

    public static Key ToKey(SDL_Keycode sdlKey) =>
        _map.TryGetValue(sdlKey, out var key) ? key : Key.Unknown;
}
