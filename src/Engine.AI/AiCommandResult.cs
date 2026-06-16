namespace Engine.AI;

/// <summary>
/// Result returned by the AI command processor.
/// </summary>
public sealed record AiCommandResult(bool Success, string Message)
{
    public static AiCommandResult Ok(string message) => new(true, message);
    public static AiCommandResult Error(string message) => new(false, message);
}
