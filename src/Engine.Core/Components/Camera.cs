using System.Numerics;

namespace Engine.Core.Components;

/// <summary>
/// A perspective camera component for the ECS.
/// Provides view and projection matrices for the renderer.
/// </summary>
public record struct Camera
{
    public Vector3 Position;
    public Vector3 Target;
    public Vector3 Up;
    public float FieldOfView;
    public float AspectRatio;
    public float NearPlane;
    public float FarPlane;

    public Camera(
        Vector3 position,
        Vector3 target,
        Vector3 up,
        float fieldOfView = MathF.PI / 4.0f,
        float aspectRatio = 16.0f / 9.0f,
        float nearPlane = 0.1f,
        float farPlane = 100.0f)
    {
        Position = position;
        Target = target;
        Up = up;
        FieldOfView = fieldOfView;
        AspectRatio = aspectRatio;
        NearPlane = nearPlane;
        FarPlane = farPlane;
    }

    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Target, Up);
    }

    public Matrix4x4 GetProjectionMatrix()
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
    }
}
