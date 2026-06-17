using System.Numerics;

namespace Engine.Core.Components;

/// <summary>
/// Physics body motion type.
/// </summary>
public enum PhysicsMotionType
{
    Static = 0,
    Kinematic = 1,
    Dynamic = 2,
}

/// <summary>
/// Collision shape type for the physics body.
/// </summary>
public enum PhysicsShapeType
{
    Box = 0,
    Sphere = 1,
}

/// <summary>
/// RigidBody component — links an ECS entity to a Jolt physics body.
/// Added to entities that should participate in physics simulation.
/// </summary>
public record struct RigidBody
{
    public PhysicsMotionType MotionType;
    public PhysicsShapeType ShapeType;
    public Vector3 ShapeSize;
    public float Mass;
    public float Friction;
    public float Restitution;
    public float LinearDamping;
    public float AngularDamping;
    public bool IsInitialized;

    public RigidBody(
        PhysicsMotionType motionType = PhysicsMotionType.Dynamic,
        PhysicsShapeType shapeType = PhysicsShapeType.Box,
        Vector3? shapeSize = null,
        float mass = 1.0f,
        float friction = 0.5f,
        float restitution = 0.3f,
        float linearDamping = 0.1f,
        float angularDamping = 0.1f)
    {
        MotionType = motionType;
        ShapeType = shapeType;
        ShapeSize = shapeSize ?? new Vector3(0.5f);
        Mass = mass;
        Friction = friction;
        Restitution = restitution;
        LinearDamping = linearDamping;
        AngularDamping = angularDamping;
        IsInitialized = false;
    }

    public static RigidBody DynamicBox(Vector3 halfExtent, float mass = 1.0f) =>
        new(PhysicsMotionType.Dynamic, PhysicsShapeType.Box, halfExtent, mass);

    public static RigidBody DynamicSphere(float radius, float mass = 1.0f) =>
        new(PhysicsMotionType.Dynamic, PhysicsShapeType.Sphere, new Vector3(radius), mass);

    public static RigidBody StaticBox(Vector3 halfExtent) =>
        new(PhysicsMotionType.Static, PhysicsShapeType.Box, halfExtent, friction: 0.8f);

    public static RigidBody StaticPlane(float size = 50f) =>
        new(PhysicsMotionType.Static, PhysicsShapeType.Box, new Vector3(size, 0.5f, size), friction: 0.8f);
}
