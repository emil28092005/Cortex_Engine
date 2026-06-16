using System.Numerics;

namespace Engine.Core.Components;

/// <summary>
/// Material component for simple PBR-like rendering.
/// Used by the renderer to tint the vertex color and control shading.
/// </summary>
public record struct Material
{
    public Vector3 Albedo;
    public float Roughness;
    public float Metallic;

    public Material(Vector3? albedo = null, float roughness = 0.5f, float metallic = 0.0f)
    {
        Albedo = albedo ?? new Vector3(0.7f, 0.6f, 0.5f);
        Roughness = roughness;
        Metallic = metallic;
    }

    public static Material Default => new(new Vector3(0.7f, 0.6f, 0.5f), 0.5f, 0.0f);
}
