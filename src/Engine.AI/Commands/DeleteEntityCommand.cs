namespace Engine.AI.Commands;

/// <summary>
/// Delete an entity by name.
/// </summary>
public sealed record DeleteEntityCommand : AiCommand
{
    public required string Name { get; init; }
}
