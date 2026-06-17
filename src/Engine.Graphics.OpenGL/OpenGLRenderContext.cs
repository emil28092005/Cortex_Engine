using Engine.Core;
using Engine.Graphics;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkWindow = Silk.NET.Windowing.IWindow;
using CoreIWindow = Engine.Core.IWindow;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLRenderContext : IRenderContext
{
    private readonly OpenGLWindow _window;
    private GL? _gl;
    private OpenGLRenderer? _renderer;
    private bool _disposed;

    public CoreIWindow Window => _window;

    public OpenGLRenderContext(int width, int height, bool enableValidation = false)
    {
        _window = new OpenGLWindow("Cortex Engine (OpenGL)", width, height);
        // Initialize() triggers the Load callback which creates the GL context
        _window.SilkView.Initialize();
        // After Initialize, CreateOpenGL should work
        _gl = _window.CreateGL();
        _window.OnLoad();
    }

    public IRenderer CreateRenderer()
    {
        _renderer = new OpenGLRenderer(_gl!);
        return _renderer;
    }

    public void Resize(int width, int height) => _renderer?.SetScreenSize(width, height);

    public void SwapBuffers() => _window.SilkView.SwapBuffers();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _renderer?.Dispose();
        _window.Dispose();
    }
}
