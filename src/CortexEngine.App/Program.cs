using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Engine.Graphics.Loaders;
using Engine.Graphics.Vulkan;
using Flecs.NET.Core;

namespace CortexEngine.App;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Cortex Engine — Vulkan Scene (pure P/Invoke)...");

        try
        {
            VulkanBackendRegistrar.EnsureRegistered();
            using var renderContext = RenderBackendFactory.Create("vulkan", 1280, 720, enableValidation: true);
            var window = renderContext.Window;
            var input = window.Input;
            using var renderer = renderContext.CreateRenderer();

            using var world = World.Create();

            var cameraEntity = world.Entity("Camera")
                .Set(new Camera(
                    new Vector3(0, 3, -12),
                    new Vector3(0, 0.5f, 0),
                    Vector3.UnitY,
                    MathF.PI / 4f,
                    1280f / 720f,
                    0.1f,
                    100f));

            var cameraController = new FreeFlyCameraController(cameraEntity);
            Console.WriteLine("Camera: FreeFly (WASD + right-click mouse look, Q/E up/down, Shift boost)");

            CreateScene(world);

            var lastWidth = window.Width;
            var lastHeight = window.Height;
            var frames = 0;
            var lastFpsTime = 0.0;
            var timing = new Timing();
            var totalTime = 0.0f;

            while (!window.ShouldClose)
            {
                timing.Tick();
                window.PumpEvents();
                input.BeginFrame();

                if (window.Width != lastWidth || window.Height != lastHeight)
                {
                    lastWidth = window.Width;
                    lastHeight = window.Height;
                    renderContext.Resize(lastWidth, lastHeight);
                    ref var cam = ref cameraEntity.Ensure<Camera>();
                    cam.AspectRatio = (float)lastWidth / lastHeight;
                    cameraEntity.Set(cam);
                }

                cameraController.Update(input, (float)timing.DeltaTime);

                totalTime += (float)timing.DeltaTime;
                var angle = totalTime * 0.3f;

                var torusEntity = world.Lookup("TorusKnot");
                if ((ulong)torusEntity.Id != 0)
                {
                    var t = torusEntity.Get<Transform>();
                    t.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
                    torusEntity.Set(t);
                }

                for (var i = 0; i < 4; i++)
                {
                    var cubeEntity = world.Lookup($"Cube{(i == 0 ? "Left" : i == 1 ? "Right" : i == 2 ? "Front" : "Back")}");
                    if ((ulong)cubeEntity.Id != 0)
                    {
                        var t = cubeEntity.Get<Transform>();
                        t.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle * (1f + i * 0.3f));
                        cubeEntity.Set(t);
                    }
                }

                renderer.RenderWorld(world);

                frames++;
                if (timing.TotalTime - lastFpsTime >= 1.0)
                {
                    Console.WriteLine($"FPS: {frames}, Delta: {timing.DeltaTime * 1000.0:F2} ms");
                    frames = 0;
                    lastFpsTime = timing.TotalTime;
                }
            }

            Console.WriteLine("Shutting down...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex}");
            Environment.Exit(1);
        }

        await Task.CompletedTask;
    }

    static void CreateScene(World world)
    {
        var torusKnot = LoadMesh("Content/torusknot.obj", new Vector3(0.9f, 0.7f, 0.3f));
        var cube = LoadMesh("Content/cube.obj", new Vector3(0.8f, 0.5f, 0.2f));
        var sphere = ProceduralMesh.CreateSphere(0.7f, 32, 16, new Vector3(0.3f, 0.6f, 0.9f));
        var grid = ProceduralMesh.CreateGrid(20, 1.0f, new Vector3(0.4f, 0.4f, 0.45f));

        world.Entity("TorusKnot")
            .Set(new Transform(new Vector3(0, 1.5f, 0), Quaternion.Identity, new Vector3(1.5f)))
            .Set(torusKnot);

        var cubes = new (string name, Vector3 pos, float scale, Vector3 color)[]
        {
            ("CubeLeft",   new(-5, 1, 0),   1.5f, new(0.8f, 0.2f, 0.2f)),
            ("CubeRight",  new(5, 1, 0),    1.5f, new(0.2f, 0.8f, 0.3f)),
            ("CubeFront",  new(0, 1, 5),    1.5f, new(0.2f, 0.4f, 0.9f)),
            ("CubeBack",   new(0, 1, -5),   1.5f, new(0.9f, 0.9f, 0.2f)),
        };

        foreach (var (name, pos, scale, color) in cubes)
        {
            var c = LoadMesh("Content/cube.obj", color);
            world.Entity(name)
                .Set(new Transform(pos, Quaternion.Identity, new Vector3(scale)))
                .Set(c);
        }

        var spheres = new (string name, Vector3 pos)[]
        {
            ("Sphere1", new(-3, 3, -2)),
            ("Sphere2", new(3, 3, 2)),
            ("Sphere3", new(-2, 4, 3)),
        };

        foreach (var (name, pos) in spheres)
        {
            world.Entity(name)
                .Set(new Transform(pos, Quaternion.Identity, Vector3.One))
                .Set(sphere);
        }

        world.Entity("Floor")
            .Set(new Transform(new Vector3(0, -0.5f, 0), Quaternion.Identity, new Vector3(20, 0.5f, 20)))
            .Set(LoadMesh("Content/cube.obj", new Vector3(0.3f, 0.3f, 0.35f)));

        world.Entity("Grid")
            .Set(new Transform(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Set(grid);

        var entityCount = 0;
        world.Each((Entity e, ref Transform _) => entityCount++);
        Console.WriteLine($"[Scene] {entityCount} entities: torus knot + 4 cubes + 3 spheres + floor + grid");
    }

    static Mesh LoadMesh(string path, Vector3 color)
    {
        if (!File.Exists(path))
        {
            var altPath = Path.Combine(AppContext.BaseDirectory, path);
            if (!File.Exists(altPath))
            {
                altPath = Path.Combine(AppContext.BaseDirectory, "Content", Path.GetFileName(path));
                if (!File.Exists(altPath))
                    throw new FileNotFoundException($"Mesh file not found: {path}");
            }
            return ObjLoader.Load(altPath, color);
        }
        return ObjLoader.Load(path, color);
    }
}
