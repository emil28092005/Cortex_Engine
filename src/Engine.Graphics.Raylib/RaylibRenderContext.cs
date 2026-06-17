using Engine.Core;
using Engine.Graphics;
using Raylib_cs;

namespace Engine.Graphics.RaylibBackend;

/// <summary>
/// Raylib implementation of the render HAL context.
/// Creates and owns a <see cref="RaylibWindow"/> (GLFW-based).
/// No SDL3 dependency — the Raylib window handles both rendering and input.
/// </summary>
public sealed class RaylibRenderContext : IRenderContext
{
    private readonly RaylibWindow _window;

    public IWindow Window => _window;

    public RaylibRenderContext(int width, int height, bool enableValidation = false)
    {
        _window = new RaylibWindow("Cortex Engine", width, height);
    }

    public IRenderer CreateRenderer() => new RaylibRenderer();

    public void Resize(int width, int height) => Raylib.SetWindowSize(width, height);

    public void Dispose() => _window.Dispose();
}
