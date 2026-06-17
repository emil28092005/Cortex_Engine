using System;
using Engine.Core;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Engine.Graphics.OpenTK;

public sealed class OpenTKWindow : GameWindow, IWindow, IDisposable
{
    private readonly OpenTKInputState _input = new();
    private bool _shouldClose;

    public new int Width => Size.X;
    public new int Height => Size.Y;
    public bool ShouldClose => _shouldClose || IsExiting;
    IInputState IWindow.Input => _input;
    public nint Handle => 0;

    public OpenTKWindow(string title, int width, int height)
        : base(GameWindowSettings.Default, new NativeWindowSettings
        {
            Title = title,
            Size = (width, height),
            APIVersion = new Version(3, 3),
            Profile = ContextProfile.Core,
            Flags = ContextFlags.ForwardCompatible,
            Vsync = VSyncMode.Off,
            NumberOfSamples = 0,
        })
    {
        // GameWindow creates the GL context in the base constructor.
        // MakeCurrent is called automatically by OpenTK on first ProcessEvents.
        // We call it explicitly here to ensure it's ready before renderer creation.
        MakeCurrent();
    }

    public void PumpEvents()
    {
        ProcessEvents(0);
        _input.Update(KeyboardState, MouseState);
    }

    void IWindow.Close() => _shouldClose = true;

    public string[] GetRequiredVulkanExtensions() => Array.Empty<string>();

    public new void Dispose()
    {
        Close();
        base.Dispose();
    }
}
