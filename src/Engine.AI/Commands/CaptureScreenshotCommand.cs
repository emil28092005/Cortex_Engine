namespace Engine.AI.Commands;

/// <summary>
/// Request a screenshot of the current rendered frame for AI visual analysis.
/// </summary>
public sealed record CaptureScreenshotCommand : AiCommand
{
    /// <summary>
    /// Output file path. If omitted, the engine chooses a default path.
    /// </summary>
    public string? OutputPath { get; init; }
}
