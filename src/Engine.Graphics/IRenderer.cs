using Engine.Core;
using Flecs.NET.Core;

namespace Engine.Graphics;

public interface IRenderer : IDisposable
{
    void RenderWorld(World world);
    void RequestScreenshot(string outputPath);
    bool IsScreenshotRequested { get; }
    IScreenshotProvider ScreenshotProvider { get; }
}
