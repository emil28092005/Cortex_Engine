using System.Numerics;

namespace Engine.AI.Commands;

/// <summary>
/// Update the Transform component of an existing entity by name.
/// </summary>
public sealed record SetTransformCommand : AiCommand
{
    public required string Name { get; init; }
    public Vector3? Position { get; init; }
    public Quaternion? Rotation { get; init; }
    public Vector3? Scale { get; init; }
}
