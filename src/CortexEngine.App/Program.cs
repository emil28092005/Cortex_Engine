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
        Console.WriteLine("Cortex Engine — Vulkan ECS Scene (pure P/Invoke)...");

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
                    new Vector3(0, 2, -8),
                    new Vector3(0, 0, 0),
                    Vector3.UnitY,
                    MathF.PI / 4f,
                    1280f / 720f,
                    0.1f,
                    100f));

            var cameraController = new FreeFlyCameraController(cameraEntity);
            Console.WriteLine("Camera: FreeFly (WASD + right-click mouse look, Q/E up/down, Shift boost)");

            var torusKnot = LoadMesh("Content/torusknot.obj", new Vector3(0.8f, 0.6f, 0.3f));
            var cube = LoadMesh("Content/cube.obj", new Vector3(0.5f, 0.7f, 0.9f));

            world.Entity("TorusKnot")
                .Set(new Transform(Vector3.Zero, Quaternion.Identity, new Vector3(1.5f)))
                .Set(torusKnot);

            var cubePositions = new Vector3[]
            {
                new(-4, 0, 0),
                new(4, 0, 0),
                new(0, -3, 0),
                new(0, 3, 0),
                new(-3, 2, 3),
                new(3, -2, -3),
            };

            for (var i = 0; i < cubePositions.Length; i++)
            {
                world.Entity($"Cube{i}")
                    .Set(new Transform(cubePositions[i], Quaternion.Identity, new Vector3(1.5f)))
                    .Set(cube);
            }

            Console.WriteLine($"[Scene] 1 torus knot + {cubePositions.Length} cubes");

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

                for (var i = 0; i < cubePositions.Length; i++)
                {
                    var cubeEntity = world.Lookup($"Cube{i}");
                    if ((ulong)cubeEntity.Id != 0)
                    {
                        var t = cubeEntity.Get<Transform>();
                        t.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle * (1f + i * 0.2f));
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
