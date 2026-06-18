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

    /// <summary>
    /// Called before RenderWorld to begin a new ImGui frame.
    /// Null if the backend doesn't support ImGui.
    /// </summary>
    void BeginImGuiFrame() { }

    /// <summary>
    /// Called after RenderWorld to render ImGui draw data.
    /// Null if the backend doesn't support ImGui.
    /// </summary>
    void EndImGuiFrame() { }
}
