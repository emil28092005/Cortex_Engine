using System;
using System.Collections.Generic;
using Engine.Core;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SilkWindow = Silk.NET.Windowing.IWindow;

namespace Engine.Graphics.OpenGL;

public sealed class OpenGLWindow : Engine.Core.IWindow, IDisposable
{
    private readonly SilkWindow _silkWindow;
    private readonly OpenGLInputState _input = new();
    private bool _shouldClose;
    private bool _disposed;

    public int Width => _silkWindow.Size.X;
    public int Height => _silkWindow.Size.Y;
    public bool ShouldClose => _shouldClose || _silkWindow.IsClosing;
    IInputState Engine.Core.IWindow.Input => _input;
    public nint Handle => 0;
    public SilkWindow SilkView => _silkWindow;

    public OpenGLWindow(string title, int width, int height)
    {
        var options = WindowOptions.Default;
        options.Title = title;
        options.Size = new Silk.NET.Maths.Vector2D<int>(width, height);
        options.VSync = false;
        options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3));
        options.WindowState = WindowState.Normal;
        options.ShouldSwapAutomatically = false;

        _silkWindow = Window.Create(options);
        _silkWindow.Closing += () => _shouldClose = true;
        // Initialize without Run() — enables non-blocking event loop
        _silkWindow.Initialize();
    }

    public void OnLoad()
    {
        var inputContext = _silkWindow.CreateInput();
        _input.Initialize(inputContext);
    }

    public GL CreateGL() => _silkWindow.CreateOpenGL();

    public void PumpEvents() => _silkWindow.DoEvents();
    public void Close() => _shouldClose = true;
    public string[] GetRequiredVulkanExtensions() => Array.Empty<string>();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _silkWindow.Close();
        _silkWindow.Dispose();
    }
}
