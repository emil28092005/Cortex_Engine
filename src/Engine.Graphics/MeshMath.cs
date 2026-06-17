using System.Numerics;

namespace Engine.Graphics;

/// <summary>
/// Basic mesh math utilities.
/// </summary>
public static class MeshMath
{
    public static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var ab = b - a;
        var ac = c - a;
        var cross = Vector3.Cross(ab, ac);
        if (cross.LengthSquared() < 0.0000001f)
            return Vector3.UnitY;
        return Vector3.Normalize(cross);
    }
}
