namespace Engine.Core;

/// <summary>
/// Provider that can capture the current rendered frame to a PNG byte array.
/// Implemented by the graphics subsystem and consumed by the AI layer.
/// </summary>
public interface IScreenshotProvider
{
    /// <summary>
    /// Request a screenshot of the next rendered frame.
    /// The returned task completes once the frame has been rendered and the PNG bytes are available.
    /// The image is also saved to <paramref name="outputPath"/> on disk.
    /// </summary>
    Task<byte[]> CaptureAsync(string outputPath);
}
