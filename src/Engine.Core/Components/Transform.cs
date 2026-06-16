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
}
