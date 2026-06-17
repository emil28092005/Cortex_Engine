using Engine.Core;
using Engine.Graphics;
using Silk.NET.OpenGL;
using SilkWindow = Silk.NET.Windowing.IWindow;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLRenderContext : IRenderContext
{
    private readonly OpenGLWindow _window;
    private GL? _gl;
    private OpenGLRenderer? _renderer;
    private bool _disposed;

    public IWindow Window => _window;

    public OpenGLRenderContext(int width, int height, bool enableValidation = false)
    {
        _window = new OpenGLWindow("Cortex Engine (OpenGL)", width, height);
    }

    public void OnLoad()
    {
        _gl = _window.SilkView.CreateOpenGL();
        _window.OnLoad();
    }

    public IRenderer CreateRenderer()
    {
        _renderer = new OpenGLRenderer(_gl!);
        return _renderer;
    }

    public void Resize(int width, int height)
    {
        _renderer?.SetScreenSize(width, height);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _renderer?.Dispose();
        _window.Dispose();
    }
}
