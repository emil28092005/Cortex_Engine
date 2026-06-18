using System.Numerics;
using Engine.AI;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Engine.Graphics.Loaders;
using Engine.Graphics.Vulkan;
using Engine.Physics;
using Flecs.NET.Core;
using ImGuiNET;
#if !RELEASE_AOT
using Engine.AI.Mcp;
using Microsoft.AspNetCore.Builder;
#endif

namespace CortexEngine.App;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Cortex Engine — Vulkan + Physics + AI/MCP (pure P/Invoke)...");

        try
        {
            var mcpPort = ParseMcpPort(args);

            using var world = World.Create();
            using var physicsWorld = new PhysicsWorld();

            VulkanBackendRegistrar.EnsureRegistered();
            using var renderContext = RenderBackendFactory.Create("vulkan", 1280, 720, enableValidation: true);
            var window = renderContext.Window;
            var input = window.Input;
            using var renderer = renderContext.CreateRenderer();

            var processor = new AiCommandProcessor(world, p => LoadMesh(p, new Vector3(0.7f, 0.6f, 0.5f)), path => renderer.RequestScreenshot(path));
            var queue = new AiCommandQueue(processor, new DummyScreenshotProvider());

#if !RELEASE_AOT
            WebApplication? mcpApp = null;
            var mcpStarted = false;
            var frameCount = 0;
#endif

            var cameraEntity = world.Entity("Camera")
                .Set(new Camera(
                    new Vector3(0, 5, -15),
                    new Vector3(0, 0.5f, 0),
                    Vector3.UnitY,
                    MathF.PI / 4f,
                    1280f / 720f,
                    0.1f,
                    100f));

            var cameraController = new FreeFlyCameraController(cameraEntity);
            Console.WriteLine("Camera: FreeFly (WASD + right-click mouse look, Q/E up/down, Shift boost)");

            CreateScene(world);

            var hasImGui = renderer.GetType().Name == "VulkanRenderer";
            if (hasImGui)
            {
                ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(window.Width, window.Height);
                Console.WriteLine("[App] ImGui initialized.");
            }

            var lastWidth = window.Width;
            var lastHeight = window.Height;
            var frames = 0;
            var lastFpsTime = 0.0;
            var timing = new Timing();

            while (!window.ShouldClose)
            {
                timing.Tick();
                window.PumpEvents();
                input.BeginFrame();

#if !RELEASE_AOT
                frameCount++;
                if (mcpPort > 0 && !mcpStarted && frameCount > 3)
                {
                    mcpStarted = true;
                    var port = mcpPort;
                    var q = queue;
                    new Thread(() =>
                    {
                        try
                        {
                            mcpApp = McpEngineServerHost.Create(Array.Empty<string>(), q, port: port);
                            Console.WriteLine($"[App] MCP HTTP server listening on http://localhost:{port}/ (SSE)");
                            mcpApp.Run();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[App] MCP server error: {ex.Message}");
                        }
                    }) { IsBackground = true }.Start();
                }
#endif

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

                var toInit = new List<(Entity, RigidBody, Transform)>();
                world.Each((Entity e, ref RigidBody rb, ref Transform t) =>
                {
                    if (!rb.IsInitialized)
                        toInit.Add((e, rb, t));
                });

                foreach (var (e, rb, t) in toInit)
                {
                    var rbInit = rb;
                    rbInit.IsInitialized = true;
                    e.Set(rbInit);
                    physicsWorld.CreateBody(e, rbInit, t);
                }

                physicsWorld.Update((float)timing.DeltaTime);
                physicsWorld.SyncTransforms(world);

                var processed = queue.ProcessPending();
                if (processed > 0)
                    Console.WriteLine($"[App] Processed {processed} AI command(s)");

                if (hasImGui)
                {
                    renderer.BeginImGuiFrame();

                    ImGui.Begin("Cortex Engine Debug");
                    ImGui.Text($"FPS: {frames}");
                    ImGui.Text($"Delta: {timing.DeltaTime * 1000.0:F2} ms");

                    var cam = cameraEntity.Get<Camera>();
                    ImGui.Separator();
                    ImGui.Text($"Camera Pos: ({cam.Position.X:F2}, {cam.Position.Y:F2}, {cam.Position.Z:F2})");
                    ImGui.Text($"Camera Target: ({cam.Target.X:F2}, {cam.Target.Y:F2}, {cam.Target.Z:F2})");
                    ImGui.Separator();

                    var entityCount = 0;
                    world.Each((Entity e, ref Transform _) => entityCount++);
                    ImGui.Text($"Entities: {entityCount}");
                    ImGui.Text($"MCP: {(mcpPort > 0 ? $"port {mcpPort}" : "disabled")}");
                    ImGui.Text("WASD: move | RMB: look | Q/E: up/down | Shift: boost");
                    ImGui.End();

                    renderer.EndImGuiFrame();
                }

                renderer.RenderWorld(world);
                queue.CompletePendingScreenshots();

                frames++;
                if (timing.TotalTime - lastFpsTime >= 1.0)
                {
                    Console.WriteLine($"FPS: {frames}, Delta: {timing.DeltaTime * 1000.0:F2} ms");
                    frames = 0;
                    lastFpsTime = timing.TotalTime;
                }
            }

            Console.WriteLine("Shutting down...");
#if !RELEASE_AOT
            if (mcpApp != null)
                await mcpApp.StopAsync();
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex}");
            Environment.Exit(1);
        }

        await Task.CompletedTask;
    }

    static int ParseMcpPort(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--mcp-port" && i + 1 < args.Length && int.TryParse(args[i + 1], out var port))
                return port;
        }
        return 0;
    }

    static void CreateScene(World world)
    {
        var torusKnot = LoadMesh("Content/torusknot.obj", new Vector3(0.9f, 0.7f, 0.3f));
        var sphere = ProceduralMesh.CreateSphere(0.7f, 32, 16, new Vector3(0.3f, 0.6f, 0.9f));
        var grid = ProceduralMesh.CreateGrid(20, 1.0f, new Vector3(0.4f, 0.4f, 0.45f));

        world.Entity("TorusKnot")
            .Set(new Transform(new Vector3(0, 4f, 0), Quaternion.Identity, new Vector3(1.5f)))
            .Set(torusKnot);

        var cubes = new (string name, Vector3 pos, float scale, Vector3 color)[]
        {
            ("CubeLeft",   new(-5, 5, 0),   1.5f, new(0.8f, 0.2f, 0.2f)),
            ("CubeRight",  new(5, 8, 0),    1.5f, new(0.2f, 0.8f, 0.3f)),
            ("CubeFront",  new(0, 6, 5),    1.5f, new(0.2f, 0.4f, 0.9f)),
            ("CubeBack",   new(0, 10, -5),  1.5f, new(0.9f, 0.9f, 0.2f)),
        };

        foreach (var (name, pos, scale, color) in cubes)
        {
            var c = LoadMesh("Content/cube.obj", color);
            world.Entity(name)
                .Set(new Transform(pos, Quaternion.Identity, new Vector3(scale)))
                .Set(c)
                .Set(RigidBody.DynamicBox(new Vector3(scale * 0.5f), mass: scale * 2f));
        }

        var spheres = new (string name, Vector3 pos)[]
        {
            ("Sphere1", new(-3, 7, -2)),
            ("Sphere2", new(3, 9, 2)),
            ("Sphere3", new(-2, 12, 3)),
        };

        foreach (var (name, pos) in spheres)
        {
            world.Entity(name)
                .Set(new Transform(pos, Quaternion.Identity, Vector3.One))
                .Set(sphere)
                .Set(RigidBody.DynamicSphere(0.7f, mass: 1.5f));
        }

        var rand = new Random(42);
        for (var i = 0; i < 10; i++)
        {
            var x = (float)(rand.NextDouble() * 16 - 8);
            var y = (float)(rand.NextDouble() * 15 + 5);
            var z = (float)(rand.NextDouble() * 16 - 8);
            var r = 0.3f + (float)rand.NextDouble() * 0.5f;
            var color = new Vector3(
                0.3f + (float)rand.NextDouble() * 0.7f,
                0.3f + (float)rand.NextDouble() * 0.7f,
                0.3f + (float)rand.NextDouble() * 0.7f);
            var ballMesh = ProceduralMesh.CreateSphere(r, 16, 8, color);
            world.Entity($"Ball{i}")
                .Set(new Transform(new Vector3(x, y, z), Quaternion.Identity, Vector3.One))
                .Set(ballMesh)
                .Set(RigidBody.DynamicSphere(r, mass: r * 2f));
        }

        world.Entity("Floor")
            .Set(new Transform(new Vector3(0, -0.5f, 0), Quaternion.Identity, new Vector3(20, 0.5f, 20)))
            .Set(LoadMesh("Content/cube.obj", new Vector3(0.3f, 0.3f, 0.35f)))
            .Set(RigidBody.StaticBox(new Vector3(20, 0.5f, 20)));

        world.Entity("Grid")
            .Set(new Transform(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Set(grid);

        var entityCount = 0;
        world.Each((Entity e, ref Transform _) => entityCount++);
        Console.WriteLine($"[Scene] {entityCount} entities: torus knot + 4 dynamic cubes + 3 dynamic spheres + static floor + grid");
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

    class DummyScreenshotProvider : Engine.Core.IScreenshotProvider
    {
        public Task<byte[]> CaptureAsync(string outputPath) => Task.FromResult(Array.Empty<byte>());
    }
}
