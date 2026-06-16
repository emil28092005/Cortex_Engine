using System.Numerics;

namespace Engine.Core.Components;

/// <summary>
/// Basic 3D transform component for the ECS.
/// Uses SIMD-friendly System.Numerics types.
/// </summary>
public record struct Transform
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public Transform(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        Position = position ?? Vector3.Zero;
        Rotation = rotation ?? Quaternion.Identity;
        Scale = scale ?? Vector3.One;
    }

    public static Transform Identity => new(Vector3.Zero, Quaternion.Identity, Vector3.One);

    public Matrix4x4 GetMatrix()
    {
        return Matrix4x4.CreateScale(Scale)
             * Matrix4x4.CreateFromQuaternion(Rotation)
             * Matrix4x4.CreateTranslation(Position);
    }

    /// <summary>
    /// Transform a normal vector from local to world space using the inverse-transpose of the model matrix.
    /// </summary>
    public Vector3 TransformNormal(Vector3 normal)
    {
        var matrix = GetMatrix();
        if (!Matrix4x4.Invert(matrix, out var inverted))
            return normal;

        // Use the upper 3x3 of the transposed inverse matrix.
        var nx = inverted.M11 * normal.X + inverted.M21 * normal.Y + inverted.M31 * normal.Z;
        var ny = inverted.M12 * normal.X + inverted.M22 * normal.Y + inverted.M32 * normal.Z;
        var nz = inverted.M13 * normal.X + inverted.M23 * normal.Y + inverted.M33 * normal.Z;

        var result = new Vector3(nx, ny, nz);
        if (result.LengthSquared() > 0.00001f)
            result = Vector3.Normalize(result);

        return result;
    }
}
