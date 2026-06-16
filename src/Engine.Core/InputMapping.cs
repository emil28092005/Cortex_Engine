using System.Collections.Generic;
using SDL;

namespace Engine.Core;

/// <summary>
/// Minimal snapshot of current input state.
/// Populated by polling SDL events once per frame.
/// </summary>
public sealed class InputMapping
{
    private readonly HashSet<SDL_Keycode> _keysPressed = new();
    private readonly HashSet<SDL_Keycode> _keysDown = new();
    private readonly HashSet<SDL_Keycode> _keysReleased = new();

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
                if (!_keysDown.Contains((SDL_Keycode)evt.key.key))
                    _keysPressed.Add((SDL_Keycode)evt.key.key);
                _keysDown.Add((SDL_Keycode)evt.key.key);
                break;

            case SDL_EventType.SDL_EVENT_KEY_UP:
                _keysDown.Remove((SDL_Keycode)evt.key.key);
                _keysReleased.Add((SDL_Keycode)evt.key.key);
                break;

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

    public bool IsKeyDown(SDL_Keycode key) => _keysDown.Contains(key);
    public bool IsKeyPressed(SDL_Keycode key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(SDL_Keycode key) => _keysReleased.Contains(key);

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
