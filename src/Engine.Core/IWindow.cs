namespace Engine.Core;

/// <summary>
/// Backend-agnostic window abstraction.
/// Each render backend (Vulkan+SDL3, Raylib+GLFW, etc.) owns and creates its own window.
/// The application retrieves the window from <see cref="Graphics.IRenderContext.Window"/>.
/// </summary>
public interface IWindow : IDisposable
{
    int Width { get; }
    int Height { get; }
    bool ShouldClose { get; }

    /// <summary>
    /// Read-only input state populated during <see cref="PumpEvents"/>.
    /// </summary>
    IInputState Input { get; }

    /// <summary>
    /// Poll window events and update <see cref="Input"/>. Called once per frame
    /// before reading input state or rendering.
    /// </summary>
    void PumpEvents();

    /// <summary>
    /// Request the window to close at the next frame boundary.
    /// </summary>
    void Close();

    /// <summary>
    /// Native window handle (e.g. <c>SDL_Window*</c>). Used by backends that need
    /// the raw OS handle for surface creation. Returns 0 if not applicable.
    /// </summary>
    nint Handle { get; }

    /// <summary>
    /// Vulkan instance extensions required by this window (e.g. VK_KHR_xlib_surface).
    /// Returns an empty array if the windowing system does not support Vulkan.
    /// </summary>
    string[] GetRequiredVulkanExtensions();
}
