using Engine.Core;
using Flecs.NET.Core;

namespace Engine.Graphics;

/// <summary>
/// Backend-agnostic renderer interface. Minimal version for triangle rendering.
/// </summary>
public interface IRenderer : IDisposable
{
    void RenderWorld(World world);

    void RequestScreenshot(string path);
    bool IsScreenshotRequested { get; }
    IScreenshotProvider ScreenshotProvider { get; }
}
