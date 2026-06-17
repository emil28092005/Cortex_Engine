using Engine.Core;
using Flecs.NET.Core;

namespace Engine.Graphics;

/// <summary>
/// Renders the ECS world and exposes screenshot capture.
/// Implemented by concrete graphics backends.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Render one frame of the ECS world and present it.
    /// </summary>
    void RenderWorld(World world);

    /// <summary>
    /// Request a screenshot of the next rendered frame to be saved to disk.
    /// </summary>
    void RequestScreenshot(string outputPath);

    /// <summary>
    /// True if a screenshot has been requested but not yet captured.
    /// </summary>
    bool IsScreenshotRequested { get; }

    /// <summary>
    /// Provider that can asynchronously capture the current frame to PNG bytes.
    /// </summary>
    IScreenshotProvider ScreenshotProvider { get; }
}
