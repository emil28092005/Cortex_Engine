using System.Numerics;

namespace Engine.Graphics;

/// <summary>
/// Shared mesh math utilities used by loaders and procedural generators.
/// </summary>
public static class MeshMath
{
    /// <summary>
    /// Compute a flat face normal from three vertex positions.
    /// Falls back to Vector3.UnitY for degenerate (zero-area) triangles.
    /// </summary>
    public static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var ab = b - a;
        var ac = c - a;
        var normal = Vector3.Cross(ab, ac);
        if (normal.LengthSquared() > 0.00001f)
            normal = Vector3.Normalize(normal);
        else
            normal = Vector3.UnitY;
        return normal;
    }
}
