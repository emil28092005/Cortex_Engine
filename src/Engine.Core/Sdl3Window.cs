using System;
using System.Runtime.InteropServices;
using System.Text;
using SDL;

namespace Engine.Core;

/// <summary>
/// A thin, disposable wrapper around an SDL3 window.
/// Handles creation, Vulkan surface discovery, and event polling.
/// </summary>
public sealed unsafe class Sdl3Window : IDisposable
{
    private readonly SDL_Window* _window;
    private bool _disposed;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public nint Handle => (nint)_window;
    public bool ShouldClose { get; private set; }

    public Sdl3Window(string title, int width, int height)
    {
        Width = width;
        Height = height;

        if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
        {
            throw new InvalidOperationException($"SDL_Init failed: {SDL3.SDL_GetError()}");
        }

        var titleBytes = Encoding.UTF8.GetBytes(title + '\0');
        fixed (byte* titlePtr = titleBytes)
        {
            _window = SDL3.SDL_CreateWindow(
                titlePtr,
                width,
                height,
                SDL_WindowFlags.SDL_WINDOW_VULKAN | SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
        }

        if (_window == null)
        {
            throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL3.SDL_GetError()}");
        }
    }

    public void PumpEvents()
    {
        SDL_Event evt;
        while (SDL3.SDL_PollEvent(&evt))
        {
            switch ((SDL_EventType)evt.Type)
            {
                case SDL_EventType.SDL_EVENT_QUIT:
                    ShouldClose = true;
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
                    Width = evt.window.data1;
                    Height = evt.window.data2;
                    break;

                case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    if (evt.key.key == SDL_Keycode.SDLK_ESCAPE)
                        ShouldClose = true;
                    break;
            }
        }
    }

    public string[] GetRequiredInstanceExtensions()
    {
        uint count;
        var extensionsPtr = SDL3.SDL_Vulkan_GetInstanceExtensions(&count);
        if (extensionsPtr == null)
        {
            throw new InvalidOperationException($"SDL_Vulkan_GetInstanceExtensions failed: {SDL3.SDL_GetError()}");
        }

        var result = new string[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = SDL3.PtrToStringUTF8(extensionsPtr[i]) ?? string.Empty;
        }

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        SDL3.SDL_DestroyWindow(_window);
        SDL3.SDL_Quit();
    }
}
