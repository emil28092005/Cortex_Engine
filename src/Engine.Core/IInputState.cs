namespace Engine.Core;

/// <summary>
/// Read-only query interface for keyboard and mouse input state.
/// Implemented by each windowing backend (SDL3, Raylib, etc.).
/// </summary>
public interface IInputState
{
    int MouseX { get; }
    int MouseY { get; }
    bool MouseLeft { get; }
    bool MouseRight { get; }
    bool MouseMiddle { get; }
    float MouseWheelDelta { get; }

    bool IsKeyDown(Key key);
    bool IsKeyPressed(Key key);
    bool IsKeyReleased(Key key);

    /// <summary>
    /// Called at the start of each frame to clear per-frame edge state
    /// (key-pressed, key-released, mouse-wheel delta).
    /// </summary>
    void BeginFrame();
}
