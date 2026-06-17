using Engine.Core;
using Engine.Graphics;

namespace Engine.Graphics.OpenTK;

/// <summary>
/// OpenTK render context — owns the GameWindow and creates the renderer.
/// </summary>
public sealed class OpenTKRenderContext : IRenderContext
{
    private readonly OpenTKWindow _window;
    private OpenTKRenderer? _renderer;
    private bool _disposed;

    public IWindow Window => _window;

    public OpenTKRenderContext(int width, int height, bool enableValidation = false)
    {
        _window = new OpenTKWindow("Cortex Engine (OpenTK)", width, height);
        // GameWindow creates OpenGL context in constructor — MakeCurrent is called automatically
        _window.MakeCurrent();
    }

    public IRenderer CreateRenderer()
    {
        _renderer = new OpenTKRenderer();
        return _renderer;
    }

    public void Resize(int width, int height)
    {
        _renderer?.SetScreenSize(width, height);
    }

    /// <summary>
    /// Swap buffers — called after RenderWorld in the main loop.
    /// </summary>
    public void SwapBuffers() => _window.SwapBuffers();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _renderer?.Dispose();
        _window.Dispose();
    }
}
