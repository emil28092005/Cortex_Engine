using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Flecs.NET.Core;
using ImGuiNET;

namespace CortexEngine.App;

/// <summary>
/// Small, self-contained FPS game loop for the Arena Strike demo. It deliberately uses
/// deterministic hitscan against ECS targets instead of coupling gameplay to a renderer.
/// </summary>
internal sealed class FpsShooterGame
{
    private const int MagazineSize = 12;
    private readonly World _world;
    private readonly Func<string, Vector3, Mesh> _loadMesh;
    private readonly List<Target> _targets = [];
    private readonly List<Blocker> _blockers = [];
    private Mesh _targetMesh;
    private Mesh _droneMesh;
    private float _lastShotAt = -10f;
    private float _reloadEndsAt;
    private float _nextWaveAt;
    private bool _wasFiring;
    private string _eventText = "Clear the arena.";
    private float _eventEndsAt = 3f;
    private Vector3 _lastSafePlayerPosition = new(0, 2.3f, -17f);

    public int Score { get; private set; }
    public int Kills { get; private set; }
    public int Health { get; private set; } = 100;
    public int Ammo { get; private set; } = MagazineSize;
    public int ReserveAmmo { get; private set; } = 84;
    public int Wave { get; private set; } = 1;

    public FpsShooterGame(World world, Func<string, Vector3, Mesh> loadMesh)
    {
        _world = world;
        _loadMesh = loadMesh;
    }

    public void CreateArena()
    {
        _targets.Clear();
        _blockers.Clear();
        Score = 0;
        Kills = 0;
        Health = 100;
        Ammo = MagazineSize;
        ReserveAmmo = 84;
        Wave = 1;
        _reloadEndsAt = 0;
        _nextWaveAt = 0;
        _eventText = "WAVE 1 — eliminate the drones";
        _eventEndsAt = 4f;

        var wallMesh = _loadMesh("Content/cube.obj", new Vector3(0.06f, 0.09f, 0.16f));
        CreateStaticBlock("ArenaBackWall", new Vector3(0, 3.5f, 15), new Vector3(18, 4, 0.7f), wallMesh);
        CreateStaticBlock("ArenaLeftWall", new Vector3(-18, 3.5f, 0), new Vector3(0.7f, 4, 15), wallMesh);
        CreateStaticBlock("ArenaRightWall", new Vector3(18, 3.5f, 0), new Vector3(0.7f, 4, 15), wallMesh);

        var coverMesh = _loadMesh("Content/cube.obj", new Vector3(0.13f, 0.27f, 0.38f));
        var coverPositions = new[]
        {
            new Vector3(-8, 1.5f, -2), new Vector3(8, 1.5f, -1),
            new Vector3(-5, 1.5f, 7), new Vector3(6, 1.5f, 8),
        };
        for (var i = 0; i < coverPositions.Length; i++)
            CreateStaticBlock($"ArenaCover_{i}", coverPositions[i], new Vector3(2.4f, 2.8f, 1.1f), coverMesh);

        var pillarMesh = _loadMesh("Content/cone.obj", new Vector3(0.8f, 0.16f, 0.04f));
        for (var i = 0; i < 6; i++)
        {
            var x = -13f + i * 5.2f;
            _world.Entity($"ArenaPillar_{i}")
                .Set(new Transform(new Vector3(x, 2.5f, 12.5f), Quaternion.Identity, new Vector3(1.3f, 3.5f, 1.3f)))
                .Set(pillarMesh);
        }

        _targetMesh = _loadMesh("Content/diamond.obj", new Vector3(1.0f, 0.08f, 0.16f));
        _droneMesh = _loadMesh("Content/sphere.obj", new Vector3(0.12f, 0.75f, 1.0f));
        SpawnWave(0f);
    }

    public void Update(IInputState input, Entity cameraEntity, float totalTime, float deltaTime, bool allowGameplayInput)
    {
        if (totalTime >= _eventEndsAt)
            _eventText = string.Empty;
        if (_reloadEndsAt > 0 && totalTime >= _reloadEndsAt)
        {
            var loaded = Math.Min(MagazineSize - Ammo, ReserveAmmo);
            Ammo += loaded;
            ReserveAmmo -= loaded;
            _reloadEndsAt = 0;
            SetEvent(loaded > 0 ? "Reload complete" : "No reserve ammo", totalTime, 1.4f);
        }

        if (allowGameplayInput && _reloadEndsAt <= 0 && input.IsKeyPressed(Key.R) && Ammo < MagazineSize && ReserveAmmo > 0)
        {
            _reloadEndsAt = totalTime + 1.05f;
            SetEvent("Reloading…", totalTime, 1.1f);
        }

        var firingEdge = input.MouseLeft && !_wasFiring;
        _wasFiring = input.MouseLeft;
        if (allowGameplayInput && firingEdge && _reloadEndsAt <= 0 && totalTime - _lastShotAt >= 0.12f)
            Fire(cameraEntity.Get<Camera>(), totalTime);

        var cameraPosition = cameraEntity.Get<Camera>().Position;
        for (var i = _targets.Count - 1; i >= 0; i--)
        {
            var target = _targets[i];
            if (!target.Entity.IsAlive())
            {
                _targets.RemoveAt(i);
                continue;
            }

            var transform = target.Entity.Get<Transform>();
            var orbit = new Vector3(
                MathF.Cos(totalTime * target.Speed + target.Phase) * target.MotionRadius,
                MathF.Sin(totalTime * (target.Speed * 1.7f) + target.Phase) * 0.65f,
                MathF.Sin(totalTime * target.Speed + target.Phase) * target.MotionRadius);
            transform.Position = target.Origin + orbit;
            transform.Rotation = Quaternion.CreateFromYawPitchRoll(totalTime * target.Speed, totalTime * 0.45f, 0);
            target.Entity.Set(transform);

            if (target.IsDrone && Vector3.Distance(transform.Position, cameraPosition) < 5f &&
                HasLineOfSight(transform.Position, cameraPosition) && totalTime >= target.NextAttackAt)
            {
                target.NextAttackAt = totalTime + 1.25f;
                Health = Math.Max(0, Health - 8);
                SetEvent("Drone hit — find cover!", totalTime, 1.1f);
                if (Health == 0)
                {
                    Health = 100;
                    Score = Math.Max(0, Score - 300);
                    SetEvent("Armor restored — score penalty", totalTime, 2.4f);
                }
            }
        }

        if (_targets.Count == 0 && _nextWaveAt <= 0)
        {
            _nextWaveAt = totalTime + 2.2f;
            SetEvent("Arena clear — next wave incoming", totalTime, 2.2f);
        }
        if (_nextWaveAt > 0 && totalTime >= _nextWaveAt)
        {
            Wave++;
            _nextWaveAt = 0;
            SpawnWave(totalTime);
            SetEvent($"WAVE {Wave}", totalTime, 2f);
        }
    }

    public void DrawHud(int width, int height)
    {
        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings |
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing;
        ImGui.SetNextWindowBgAlpha(0.62f);
        ImGui.SetNextWindowPos(new Vector2(18, height - 126), ImGuiCond.Always);
        ImGui.Begin("Arena Strike HUD", flags);
        ImGui.TextColored(new Vector4(0.25f, 0.86f, 1f, 1f), $"ARENA STRIKE  ·  WAVE {Wave}");
        ImGui.Text($"HEALTH {Health,3}     SCORE {Score,5}     KILLS {Kills}");
        ImGui.Text(_reloadEndsAt > 0 ? "RELOADING…" : $"AMMO {Ammo,2} / {ReserveAmmo,2}");
        if (!string.IsNullOrEmpty(_eventText))
            ImGui.TextColored(new Vector4(1f, 0.42f, 0.3f, 1f), _eventText);
        ImGui.TextDisabled("LMB fire · R reload · WASD move · RMB look");
        ImGui.End();

        ImGui.SetNextWindowBgAlpha(0f);
        ImGui.SetNextWindowPos(new Vector2(width * 0.5f - 5f, height * 0.5f - 11f), ImGuiCond.Always);
        ImGui.Begin("##ArenaCrosshair", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove |
                                      ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.TextColored(new Vector4(0.9f, 0.96f, 1f, 0.95f), "+");
        ImGui.End();
    }

    private void Fire(Camera camera, float totalTime)
    {
        _lastShotAt = totalTime;
        if (Ammo <= 0)
        {
            SetEvent(ReserveAmmo > 0 ? "Magazine empty — press R" : "Out of ammo", totalTime, 1.2f);
            return;
        }

        Ammo--;
        var origin = camera.Position;
        var direction = Vector3.Normalize(camera.Target - camera.Position);
        var blockerDistance = float.MaxValue;
        foreach (var blocker in _blockers)
        {
            if (RayAabb(origin, direction, blocker.Center, blocker.HalfExtent, out var distance))
                blockerDistance = Math.Min(blockerDistance, distance);
        }
        Target? hit = null;
        var nearest = float.MaxValue;
        foreach (var target in _targets)
        {
            if (!target.Entity.IsAlive()) continue;
            var position = target.Entity.Get<Transform>().Position;
            if (RaySphere(origin, direction, position, target.Radius, out var distance) && distance < nearest && distance < blockerDistance)
            {
                nearest = distance;
                hit = target;
            }
        }

        if (hit is null)
        {
            SetEvent(blockerDistance < float.MaxValue ? "Shot blocked by cover" : "Miss", totalTime, 0.55f);
            return;
        }

        var killed = hit;
        if (killed.Entity.IsAlive()) killed.Entity.Destruct();
        _targets.Remove(killed);
        Kills++;
        Score += killed.IsDrone ? 150 : 100;
        SetEvent(killed.IsDrone ? "Drone destroyed +150" : "Target destroyed +100", totalTime, 0.75f);
    }

    private void SpawnWave(float totalTime)
    {
        var positions = new[]
        {
            new Vector3(-11, 4.5f, 10), new Vector3(-6, 6.5f, 7), new Vector3(0, 5.3f, 11),
            new Vector3(6, 6.8f, 8), new Vector3(11, 4.7f, 10), new Vector3(-10, 3.2f, 3),
            new Vector3(10, 3.5f, 3), new Vector3(-3, 7.3f, 12), new Vector3(4, 7.6f, 12),
        };
        for (var i = 0; i < positions.Length; i++)
            SpawnTarget($"ArenaTarget_{Wave}_{i}", positions[i], isDrone: i % 3 == 0, i * 0.91f + Wave);
    }

    private void SpawnTarget(string name, Vector3 origin, bool isDrone, float phase)
    {
        var radius = isDrone ? 0.76f : 0.62f;
        var mesh = isDrone ? _droneMesh : _targetMesh;
        var entity = _world.Entity(name)
            .Set(new Transform(origin, Quaternion.Identity, new Vector3(isDrone ? 1.25f : 0.9f)))
            .Set(mesh);
        _targets.Add(new Target(entity, origin, radius, isDrone, phase, isDrone ? 0.82f : 0.56f, isDrone ? 1.4f : 0.75f));
    }

    private void CreateStaticBlock(string name, Vector3 position, Vector3 scale, Mesh mesh)
    {
        _world.Entity(name)
            .Set(new Transform(position, Quaternion.Identity, scale))
            .Set(mesh)
            .Set(RigidBody.StaticBox(scale * 0.5f));
        _blockers.Add(new Blocker(position, scale * 0.5f));
    }

    public Vector3 ConstrainPlayerPosition(Vector3 desiredPosition)
    {
        const float playerRadius = 0.42f;
        var constrained = desiredPosition;
        constrained.X = Math.Clamp(constrained.X, -16.5f, 16.5f);
        constrained.Z = Math.Clamp(constrained.Z, -17.5f, 14f);

        foreach (var blocker in _blockers)
        {
            if (constrained.Y > blocker.Center.Y + blocker.HalfExtent.Y + 0.25f)
                continue;
            if (MathF.Abs(constrained.X - blocker.Center.X) < blocker.HalfExtent.X + playerRadius &&
                MathF.Abs(constrained.Z - blocker.Center.Z) < blocker.HalfExtent.Z + playerRadius)
                return _lastSafePlayerPosition;
        }

        _lastSafePlayerPosition = constrained;
        return constrained;
    }

    private void SetEvent(string text, float totalTime, float duration)
    {
        _eventText = text;
        _eventEndsAt = totalTime + duration;
    }

    private static bool RaySphere(Vector3 origin, Vector3 direction, Vector3 center, float radius, out float distance)
    {
        var toCenter = center - origin;
        var projection = Vector3.Dot(toCenter, direction);
        var closestSquared = toCenter.LengthSquared() - projection * projection;
        var radiusSquared = radius * radius;
        if (projection < 0 || closestSquared > radiusSquared)
        {
            distance = 0;
            return false;
        }
        distance = projection - MathF.Sqrt(radiusSquared - closestSquared);
        return distance >= 0;
    }

    private static bool RayAabb(Vector3 origin, Vector3 direction, Vector3 center, Vector3 halfExtent, out float distance)
    {
        var minimum = center - halfExtent;
        var maximum = center + halfExtent;
        var enter = 0f;
        var exit = float.MaxValue;
        for (var axis = 0; axis < 3; axis++)
        {
            var originAxis = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            var directionAxis = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            var minAxis = axis == 0 ? minimum.X : axis == 1 ? minimum.Y : minimum.Z;
            var maxAxis = axis == 0 ? maximum.X : axis == 1 ? maximum.Y : maximum.Z;
            if (MathF.Abs(directionAxis) < 0.00001f)
            {
                if (originAxis < minAxis || originAxis > maxAxis) { distance = 0; return false; }
                continue;
            }
            var inverse = 1f / directionAxis;
            var first = (minAxis - originAxis) * inverse;
            var second = (maxAxis - originAxis) * inverse;
            if (first > second) (first, second) = (second, first);
            enter = Math.Max(enter, first);
            exit = Math.Min(exit, second);
            if (enter > exit) { distance = 0; return false; }
        }
        distance = enter;
        return exit >= 0;
    }

    private bool HasLineOfSight(Vector3 source, Vector3 destination)
    {
        var offset = destination - source;
        var playerDistance = offset.Length();
        if (playerDistance < 0.001f) return true;
        var direction = offset / playerDistance;
        foreach (var blocker in _blockers)
        {
            if (RayAabb(source, direction, blocker.Center, blocker.HalfExtent, out var blockerDistance) &&
                blockerDistance < playerDistance - 0.05f)
                return false;
        }
        return true;
    }

    private sealed class Target
    {
        public Entity Entity { get; }
        public Vector3 Origin { get; }
        public float Radius { get; }
        public bool IsDrone { get; }
        public float Phase { get; }
        public float Speed { get; }
        public float MotionRadius { get; }
        public float NextAttackAt { get; set; }

        public Target(Entity entity, Vector3 origin, float radius, bool isDrone, float phase, float speed, float motionRadius)
        {
            Entity = entity;
            Origin = origin;
            Radius = radius;
            IsDrone = isDrone;
            Phase = phase;
            Speed = speed;
            MotionRadius = motionRadius;
        }
    }

    private readonly record struct Blocker(Vector3 Center, Vector3 HalfExtent);
}
