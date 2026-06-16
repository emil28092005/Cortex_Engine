using System.Numerics;

namespace Engine.AI.Commands;

/// <summary>
/// Update the Material component of an existing entity by name.
/// </summary>
public sealed record SetMaterialCommand : AiCommand
{
    public required string Name { get; init; }
    public Vector3? Albedo { get; init; }
    public float? Roughness { get; init; }
    public float? Metallic { get; init; }
    public string? TexturePath { get; init; }
}
