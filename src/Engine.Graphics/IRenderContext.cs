using Engine.Core;

namespace Engine.Graphics;

/// <summary>
/// Render context created by a backend. Owns the window and can create a renderer.
/// </summary>
public interface IRenderContext : IDisposable
{
    IWindow Window { get; }
    IRenderer CreateRenderer();
    void Resize(int width, int height);
}
