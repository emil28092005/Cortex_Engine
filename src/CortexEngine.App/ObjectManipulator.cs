using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Physics;
using Flecs.NET.Core;
using Raylib_cs;
using EngineMesh = Engine.Core.Components.Mesh;
using EngineTransform = Engine.Core.Components.Transform;
using EngineRigidBody = Engine.Core.Components.RigidBody;

namespace CortexEngine.App;

/// <summary>
/// Unity-like object manipulation: left-click to pick, drag to move on ground plane.
/// Only active when ImGui is not capturing the mouse.
/// </summary>
public sealed class ObjectManipulator
{
    private Entity _selectedEntity;
    private bool _isDragging;
    private Vector3 _dragOffset;
    private float _dragDepth;

    public string SelectedEntityName => _selectedEntity.IsValid() ? _selectedEntity.Name() : "";
    public bool IsDragging => _isDragging;

    /// <summary>
    /// Process input for object picking and dragging.
    /// Returns true if input was consumed (ImGui should be ignored).
    /// </summary>
    public bool ProcessInput(World world, Camera camera, IInputState input)
    {
        // Skip if ImGui is capturing mouse
        if (ImGuiNET.ImGui.GetIO().WantCaptureMouse)
        {
            _isDragging = false;
            return false;
        }

        var mousePos = new Vector2(input.MouseX, input.MouseY);
        var ray = Raylib.GetScreenToWorldRay(mousePos, ToRaylibCamera(camera));

        // Left click — pick or start drag
        if (input.MouseLeft && !_isDragging)
        {
            var hit = RaycastEntities(world, ray);
            if (hit.IsValid())
            {
                _selectedEntity = hit;
                _isDragging = true;
                // Store depth along camera forward axis and offset from object center
                var objPos = hit.Get<EngineTransform>().Position;
                var camForward = Vector3.Normalize(camera.Target - camera.Position);
                _dragDepth = Vector3.Dot(objPos - camera.Position, camForward);
                _dragOffset = objPos - ProjectToCameraPlane(ray, camera, _dragDepth);
                return true;
            }
        }

        // Drag — move object on plane orthogonal to camera (screen-space movement)
        if (_isDragging)
        {
            if (input.MouseLeft)
            {
                var targetPos = ProjectToCameraPlane(ray, camera, _dragDepth) + _dragOffset;

                if (_selectedEntity.IsValid() && _selectedEntity.Has<EngineTransform>())
                {
                    var t = _selectedEntity.Get<EngineTransform>();
                    t.Position = targetPos;
                    _selectedEntity.Set(t);
                }
                return true;
            }
            else
            {
                _isDragging = false;
            }
        }

        return false;
    }

    /// <summary>
    /// After physics step, re-sync dragged entity position to physics body.
    /// </summary>
    public void SyncToPhysics(PhysicsWorld physicsWorld)
    {
        if (_isDragging && _selectedEntity.IsValid() && _selectedEntity.Has<EngineTransform>())
        {
            var t = _selectedEntity.Get<EngineTransform>();
            physicsWorld.SyncToPhysics(_selectedEntity, t);
        }
    }

    /// <summary>
    /// Get the entity currently being dragged (for physics sync skip).
    /// </summary>
    public Entity? GetDraggedEntity() => _isDragging ? _selectedEntity : null;

    private static Entity RaycastEntities(World world, Ray ray)
    {
        Entity closest = default;
        var closestDist = float.MaxValue;

        world.Each((Entity e, ref EngineTransform t, ref EngineMesh _) =>
        {
            var name = e.Name();
            if (string.IsNullOrEmpty(name) || name == "Grid" || name == "Floor")
                return;

            // Simple sphere intersection using position + approximate radius
            var radius = 1.0f;
            if (e.Has<EngineRigidBody>())
            {
                var rb = e.Get<EngineRigidBody>();
                radius = rb.ShapeSize.Length();
            }

            var toCenter = t.Position - ray.Position;
            var proj = Vector3.Dot(toCenter, ray.Direction);
            if (proj < 0) return; // behind camera

            var closestPoint = ray.Position + ray.Direction * proj;
            var dist = Vector3.Distance(closestPoint, t.Position);

            if (dist <= radius)
            {
                var rayDist = Vector3.Distance(ray.Position, t.Position);
                if (rayDist < closestDist)
                {
                    closestDist = rayDist;
                    closest = e;
                }
            }
        });

        return closest;
    }

    /// <summary>
    /// Project a screen ray onto a plane orthogonal to the camera at the given depth.
    /// This makes objects move in screen-space (like Unity's screen-space drag).
    /// </summary>
    private static Vector3 ProjectToCameraPlane(Ray ray, Camera camera, float depth)
    {
        var camForward = Vector3.Normalize(camera.Target - camera.Position);
        var planePoint = camera.Position + camForward * depth;

        // Ray-plane intersection: plane through planePoint with normal = camForward
        var denom = Vector3.Dot(ray.Direction, camForward);
        if (MathF.Abs(denom) < 0.0001f)
            return planePoint;

        var t = Vector3.Dot(planePoint - ray.Position, camForward) / denom;
        if (t < 0)
            return planePoint;

        return ray.Position + ray.Direction * t;
    }

    private static Camera3D ToRaylibCamera(Camera camera)
    {
        return new Camera3D
        {
            Position = camera.Position,
            Target = camera.Target,
            Up = camera.Up,
            FovY = camera.FieldOfView * 180.0f / MathF.PI,
            Projection = CameraProjection.Perspective
        };
    }
}
