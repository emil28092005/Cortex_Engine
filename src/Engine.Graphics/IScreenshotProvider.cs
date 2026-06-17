namespace Engine.Graphics;

/// <summary>
/// Provides access to the latest captured screenshot bytes.
/// </summary>
public interface IScreenshotProvider
{
    /// <summary>
    /// Returns the path of the screenshot file if a screenshot is available; otherwise null.
    /// </summary>
    string? TryTakeScreenshotPath();
}
