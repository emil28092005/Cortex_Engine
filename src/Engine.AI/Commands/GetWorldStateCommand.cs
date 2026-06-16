namespace Engine.AI.Commands;

/// <summary>
/// Dump the current ECS world state as JSON for AI analysis.
/// </summary>
public sealed record GetWorldStateCommand : AiCommand;
