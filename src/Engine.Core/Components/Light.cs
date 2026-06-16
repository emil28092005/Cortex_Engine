using System.Numerics;

namespace Engine.Core.Components;

/// <summary>
/// A directional light component for the ECS.
/// </summary>
public record struct Light
{
    public Vector3 Direction;
    public Vector3 Color;
    public float Intensity;

    public Light(Vector3 direction, Vector3 color, float intensity = 1.0f)
    {
        Direction = Vector3.Normalize(direction);
        Color = color;
        Intensity = intensity;
    }
}
