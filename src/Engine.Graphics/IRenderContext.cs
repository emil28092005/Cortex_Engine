using Engine.Core;

namespace Engine.Graphics;

/// <summary>
/// Abstraction over a graphics backend (Vulkan, Raylib, etc.).
/// Each backend owns its window and surface. The application retrieves
/// the window via <see cref="Window"/> for input and event polling.
/// </summary>
public interface IRenderContext : IDisposable
{
    /// <summary>
    /// The window owned by this backend. The application uses this for
    /// input polling, resize detection, and close requests.
    /// </summary>
    IWindow Window { get; }

    /// <summary>
    /// Create a renderer that can draw the ECS world using this backend.
    /// </summary>
    IRenderer CreateRenderer();

    /// <summary>
    /// Notify the backend that the output surface has been resized.
    /// </summary>
    void Resize(int width, int height);
}
