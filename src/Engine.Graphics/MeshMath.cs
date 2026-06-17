using System.Numerics;

namespace Engine.Graphics;

public static class MeshMath
{
    public static Vector3 ComputeFaceNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var edge1 = b - a;
        var edge2 = c - a;
        var normal = Vector3.Cross(edge2, edge1);

        if (normal.LengthSquared() < 1e-12f)
            return Vector3.UnitY;

        return Vector3.Normalize(normal);
    }
}
