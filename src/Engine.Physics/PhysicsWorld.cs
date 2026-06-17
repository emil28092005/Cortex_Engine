using System.Numerics;
using Engine.Core.Components;
using Flecs.NET.Core;
using JoltPhysicsSharp;

namespace Engine.Physics;

/// <summary>
/// Physics world wrapper — manages Jolt PhysicsSystem, body creation, and ECS sync.
/// </summary>
public sealed class PhysicsWorld : IDisposable
{
    private const int MaxBodies = 65536;
    private const int MaxBodyPairs = 65536;
    private const int MaxContactConstraints = 65536;

    private static class Layers
    {
        public static readonly ObjectLayer NonMoving = 0;
        public static readonly ObjectLayer Moving = 1;
    }

    private readonly PhysicsSystem _physicsSystem;
    private readonly BodyInterface _bodyInterface;
    private readonly JobSystemThreadPool _jobSystem;
    private readonly BroadPhaseLayerInterfaceTable _broadPhaseLayerInterface;
    private readonly ObjectLayerPairFilterTable _objectLayerPairFilter;
    private readonly ObjectVsBroadPhaseLayerFilterTable _objectVsBroadPhaseLayerFilter;
    private readonly Dictionary<Entity, BodyID> _entityToBody = new();
    private readonly Dictionary<BodyID, Entity> _bodyToEntity = new();
    private readonly List<Body> _bodies = new();
    private bool _disposed;

    public Vector3 Gravity
    {
        get => _physicsSystem.Gravity;
        set => _physicsSystem.Gravity = value;
    }

    public PhysicsWorld()
    {
        Foundation.Init(false);

        _broadPhaseLayerInterface = new BroadPhaseLayerInterfaceTable(2, 2);
        _broadPhaseLayerInterface.MapObjectToBroadPhaseLayer(Layers.NonMoving, new BroadPhaseLayer(0));
        _broadPhaseLayerInterface.MapObjectToBroadPhaseLayer(Layers.Moving, new BroadPhaseLayer(1));

        _objectLayerPairFilter = new ObjectLayerPairFilterTable(2);

        _objectVsBroadPhaseLayerFilter = new ObjectVsBroadPhaseLayerFilterTable(
            _broadPhaseLayerInterface, 2, _objectLayerPairFilter, 2);

        var settings = new PhysicsSystemSettings
        {
            MaxBodies = MaxBodies,
            MaxBodyPairs = MaxBodyPairs,
            MaxContactConstraints = MaxContactConstraints,
            BroadPhaseLayerInterface = _broadPhaseLayerInterface,
            ObjectLayerPairFilter = _objectLayerPairFilter,
            ObjectVsBroadPhaseLayerFilter = _objectVsBroadPhaseLayerFilter,
        };

        _physicsSystem = new PhysicsSystem(settings);
        _physicsSystem.Gravity = new Vector3(0, -9.81f, 0);
        _bodyInterface = _physicsSystem.BodyInterface;
        _jobSystem = new JobSystemThreadPool();
    }

    public void CreateBody(Entity entity, in RigidBody rigidBody, in Transform transform)
    {
        if (_entityToBody.ContainsKey(entity))
            return;

        var shape = CreateShape(rigidBody);
        var motionType = ToMotionType(rigidBody.MotionType);
        var layer = rigidBody.MotionType == PhysicsMotionType.Static ? Layers.NonMoving : Layers.Moving;

        var creationSettings = new BodyCreationSettings(
            shape,
            transform.Position,
            transform.Rotation,
            motionType,
            layer);

        var activation = rigidBody.MotionType == PhysicsMotionType.Static
            ? Activation.DontActivate
            : Activation.Activate;

        var body = _bodyInterface.CreateBody(creationSettings);
        _bodyInterface.AddBody(body.ID, activation);

        if (rigidBody.MotionType != PhysicsMotionType.Static)
        {
            body.SetFriction(rigidBody.Friction);
            body.SetRestitution(rigidBody.Restitution);
        }

        _entityToBody[entity] = body.ID;
        _bodyToEntity[body.ID] = entity;
        _bodies.Add(body);
    }

    public void RemoveBody(Entity entity)
    {
        if (!_entityToBody.TryGetValue(entity, out var bodyId))
            return;

        _bodyInterface.RemoveBody(bodyId);
        _bodyInterface.DestroyBody(bodyId);
        _entityToBody.Remove(entity);
        _bodyToEntity.Remove(bodyId);
    }

    public void Update(float deltaTime, int collisionSteps = 1)
    {
        _physicsSystem.Update(deltaTime, collisionSteps, _jobSystem);
    }

    public void SyncTransforms(World world)
    {
        // Collect updates first to avoid calling entity.Set() during dictionary iteration
        var updates = new List<(Entity entity, Vector3 pos, Quaternion rot)>();
        foreach (var (entity, bodyId) in _entityToBody)
        {
            if ((ulong)entity.Id == 0)
                continue;

            var pos = _bodyInterface.GetPosition(bodyId);
            var rot = _bodyInterface.GetRotation(bodyId);
            updates.Add((entity, pos, rot));
        }

        // Apply updates outside the iteration
        foreach (var (entity, pos, rot) in updates)
        {
            if (entity.Has<Transform>())
            {
                var t = entity.Get<Transform>();
                t.Position = pos;
                t.Rotation = rot;
                entity.Set(t);
            }
        }
    }

    public void SyncToPhysics(Entity entity, in Transform transform)
    {
        if (!_entityToBody.TryGetValue(entity, out var bodyId))
            return;

        _bodyInterface.SetPositionAndRotation(bodyId, transform.Position, transform.Rotation, Activation.Activate);
    }

    private static Shape CreateShape(RigidBody rb)
    {
        return rb.ShapeType switch
        {
            PhysicsShapeType.Box => new BoxShape(rb.ShapeSize),
            PhysicsShapeType.Sphere => new SphereShape(rb.ShapeSize.X),
            _ => new BoxShape(rb.ShapeSize),
        };
    }

    private static MotionType ToMotionType(PhysicsMotionType type) => type switch
    {
        PhysicsMotionType.Static => MotionType.Static,
        PhysicsMotionType.Kinematic => MotionType.Kinematic,
        PhysicsMotionType.Dynamic => MotionType.Dynamic,
        _ => MotionType.Dynamic,
    };

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var body in _bodies)
            body.Dispose();
        _bodies.Clear();
        _entityToBody.Clear();
        _bodyToEntity.Clear();

        _jobSystem.Dispose();
        _physicsSystem.Dispose();
        _objectVsBroadPhaseLayerFilter.Dispose();
        _objectLayerPairFilter.Dispose();
        _broadPhaseLayerInterface.Dispose();
        Foundation.Shutdown();
    }
}
