using System;
using Engine.Core;
using Raylib_cs;

namespace Engine.Graphics.RaylibBackend;

/// <summary>
/// Raylib-backed implementation of <see cref="IWindow"/>.
/// Wraps Raylib's GLFW window creation, event polling, and input.
/// </summary>
public sealed class RaylibWindow : IWindow
{
    private readonly RaylibInputState _input = new();
    private bool _shouldClose;
    private bool _disposed;

    public int Width => Raylib.GetScreenWidth();
    public int Height => Raylib.GetScreenHeight();
    public bool ShouldClose => _shouldClose;
    public IInputState Input => _input;
    public nint Handle => 0;

    public RaylibWindow(string title, int width, int height)
    {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.InitWindow(width, height, title);
        Raylib.SetTargetFPS(0);

        // Present a blank frame so the window is visible immediately.
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(25, 30, 40, 255));
        Raylib.EndDrawing();
    }

    public void PumpEvents()
    {
        _input.Poll();
        _shouldClose = Raylib.WindowShouldClose() || _shouldClose;

        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
            _shouldClose = true;
    }

    public void Close() => _shouldClose = true;

    public string[] GetRequiredVulkanExtensions() => Array.Empty<string>();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Raylib.CloseWindow();
    }
}
