namespace Engine.Core;

/// <summary>
/// Backend-agnostic key codes used by <see cref="IInputState"/> and camera controllers.
/// Each windowing backend (SDL3, Raylib, etc.) maps its native key codes to these values.
/// </summary>
public enum Key
{
    Unknown = 0,
    Space,
    Escape,
    Enter,
    Tab,
    Backspace,
    Insert,
    Delete,
    Home,
    End,
    PageUp,
    PageDown,
    Left,
    Right,
    Up,
    Down,

    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    Zero, One, Two, Three, Four, Five, Six, Seven, Eight, Nine,

    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,

    LeftShift,
    LeftControl,
    LeftAlt,
    RightShift,
    RightControl,
    RightAlt,
}
