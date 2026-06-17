using System.Numerics;

namespace Engine.Core.Components;

/// <summary>
/// Light type: directional (sun) or point (bulb).
/// </summary>
public enum LightType
{
    Directional = 0,
    Point = 1,
}

/// <summary>
/// A light component for the ECS.
/// Directional lights use Direction; point lights use Position with distance attenuation.
/// </summary>
public record struct Light
{
    public LightType Type;
    public Vector3 Direction;
    public Vector3 Position;
    public Vector3 Color;
    public float Intensity;
    public float Range;

    /// <summary>
    /// Create a directional light. Direction is normalized.
    /// </summary>
    public static Light Directional(Vector3 direction, Vector3 color, float intensity = 1.0f)
    {
        return new Light
        {
            Type = LightType.Directional,
            Direction = direction.LengthSquared() > 0.0001f
                ? Vector3.Normalize(direction)
                : Vector3.UnitY,
            Position = Vector3.Zero,
            Color = color,
            Intensity = intensity,
            Range = 0
        };
    }

    /// <summary>
    /// Create a point light. Light fades with distance based on Range.
    /// </summary>
    public static Light Point(Vector3 position, Vector3 color, float intensity = 1.0f, float range = 20.0f)
    {
        return new Light
        {
            Type = LightType.Point,
            Direction = Vector3.Zero,
            Position = position,
            Color = color,
            Intensity = intensity,
            Range = range
        };
    }

    public bool IsPoint => Type == LightType.Point;
    public bool IsDirectional => Type == LightType.Directional;
}
