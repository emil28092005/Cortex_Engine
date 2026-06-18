using System.Numerics;

namespace Engine.AI.Commands;

/// <summary>
/// Spawn a named entity with a Mesh loaded from a model file and a Transform.
/// </summary>
public sealed record SpawnModelCommand : AiCommand
{
    public required string Name { get; init; }
    public required string ModelPath { get; init; }
    public Vector3 Position { get; init; } = Vector3.Zero;
    public Quaternion Rotation { get; init; } = Quaternion.Identity;
    public Vector3 Scale { get; init; } = Vector3.One;
    public bool Physics { get; init; } = false;
}
