using Engine.Core;

namespace Engine.Graphics;

public interface IRenderContext : IDisposable
{
    IWindow Window { get; }
    IRenderer CreateRenderer();
    void Resize(int width, int height);
}
