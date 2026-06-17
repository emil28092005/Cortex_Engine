using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Engine.AI;
#if !RELEASE_AOT
using Engine.AI.Mcp;
using Microsoft.AspNetCore.Builder;
#endif
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Engine.Graphics.Loaders;
using Engine.Graphics.OpenTK;
using Engine.Graphics.RaylibBackend;
using Engine.Graphics.Vulkan;
using Engine.Physics;
using Flecs.NET.Core;

namespace CortexEngine.App;

class Program
{
    static async Task Main(string[] args)
    {
            Console.WriteLine("Cortex Engine — Materials, Grid, Lighting, FreeFly Camera...");

        try
        {
            if (args.Contains("--mcp-stdio"))
            {
                RunMcpStdioServer();
                return;
            }

            var cameraTour = args.Contains("--camera-tour");
            var testScene = args.Contains("--test-scene");
            if (testScene)
                cameraTour = true;

            using var world = World.Create();
            var timing = new Timing();
            using var physicsWorld = new PhysicsWorld();

            RaylibBackendRegistrar.EnsureRegistered();
            VulkanBackendRegistrar.EnsureRegistered();
            OpenTKBackendRegistrar.EnsureRegistered();
            using var renderContext = RenderBackendFactory.Create("raylib", 1280, 720, enableValidation: false);
            var window = renderContext.Window;
            var input = window.Input;
            using var renderer = renderContext.CreateRenderer();

            // ImGui editor layer (Raylib only)
            ImGuiLayer? imGuiLayer = null;
            var objectManipulator = new ObjectManipulator();
            objectManipulator.SetImGuiAvailable(false);
            if (!cameraTour && renderer is RaylibRenderer rlRenderer)
            {
                imGuiLayer = new ImGuiLayer();
                imGuiLayer.Initialize();
                rlRenderer.ImGuiLayer = imGuiLayer;
                objectManipulator.SetImGuiAvailable(true);
            }

            var (modelPath, mcpPort) = ParseArgs(args);
            var mesh = LoadModel(modelPath);

            var processor = new AiCommandProcessor(world, LoadModel, path => renderer.RequestScreenshot(path));
            var queue = new AiCommandQueue(processor, renderer.ScreenshotProvider);

            var cameraEntity = world.Entity("Camera")
                .Set(new Transform(new Vector3(0.0f, 0.75f, -30.0f), Quaternion.Identity, Vector3.One))
                .Set(new Camera(
                    new Vector3(0.0f, 0.75f, -30.0f),
                    new Vector3(0.0f, 0.5f, 0.0f),
                    Vector3.UnitY,
                    MathF.PI / 12.0f,
                    1280.0f / 720.0f,
                    0.1f,
                    100.0f));

            world.Entity("MainLight")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, Vector3.One))
                .Set(Light.Directional(new Vector3(0.4f, -1.0f, -0.3f), new Vector3(1.0f, 0.95f, 0.85f), 2.0f));

            ICameraController[] cameraControllers =
            {
                new FreeFlyCameraController(cameraEntity),
                new OrbitCameraController(cameraEntity, new Vector3(0.0f, 0.5f, 0.0f))
            };
            var activeControllerIndex = 0;
            var cameraController = cameraControllers[activeControllerIndex];
            Console.WriteLine($"Active camera controller: {cameraController.Name} (press F to toggle)");

            if (testScene)
            {
                Console.WriteLine("Calibration test scene enabled.");
                CreateCalibrationScene(world, mesh);
            }
            else
            {
                CreateDemoScene(world, mesh);
            }

#if !RELEASE_AOT
            WebApplication? mcpApp = null;
            Task? mcpTask = null;

            if (mcpPort > 0)
            {
                // Start the MCP server in the background so AI agents can connect via HTTP.
                mcpApp = McpEngineServerHost.Create(args, queue, port: mcpPort);
                mcpTask = mcpApp.RunAsync();
                _ = mcpTask.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        Console.WriteLine($"MCP server error: {t.Exception?.GetBaseException().Message}");
                    else if (t.IsCanceled)
                        Console.WriteLine("MCP server canceled.");
                    else
                        Console.WriteLine("MCP server stopped.");
                }, TaskScheduler.Default);

                Console.WriteLine($"MCP HTTP server listening on http://localhost:{mcpPort}/ (SSE)");
            }
            else
            {
                Console.WriteLine("MCP server disabled (--mcp-port 0).");
            }
#endif


            var frames = 0;
            var lastFpsTime = 0.0;
            var lastWidth = window.Width;
            var lastHeight = window.Height;
            var demoScreenshotRequested = false;
            var currentFps = 0;

            var tourPoses = testScene
                ? new CameraPose[]
                {
                    new("test_front", new Vector3(0.0f, 0.75f, -30.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_back", new Vector3(0.0f, 0.75f, 30.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_left", new Vector3(-30.0f, 0.75f, 0.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_right", new Vector3(30.0f, 0.75f, 0.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_top", new Vector3(0.0f, 30.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), -Vector3.UnitZ),
                    new("test_shifted", new Vector3(15.0f, 0.75f, -22.5f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_rotated", new Vector3(0.0f, 0.75f, -30.0f), new Vector3(2.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_yaw_15", new Vector3(7.76f, 0.75f, -28.98f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_yaw_30", new Vector3(15.0f, 0.75f, -25.98f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_yaw_45", new Vector3(21.21f, 0.75f, -21.21f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_yaw_90", new Vector3(30.0f, 0.75f, 0.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_pitch_45", new Vector3(0.0f, 21.96f, -21.21f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_close", new Vector3(0.0f, 0.75f, -15.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_far", new Vector3(0.0f, 0.75f, -60.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_farther", new Vector3(0.0f, 0.75f, -120.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("test_toward", new Vector3(0.0f, 0.75f, -20.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY)
                }
                : new CameraPose[]
                {
                    new("front", new Vector3(0.0f, 0.75f, -30.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("top", new Vector3(0.0f, 30.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), -Vector3.UnitZ),
                    new("side", new Vector3(30.0f, 0.75f, 4.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("close", new Vector3(1.0f, 0.75f, -5.0f), new Vector3(0.5f, 0.5f, 0.0f), Vector3.UnitY),
                    new("low", new Vector3(0.0f, 0.25f, -6.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY),
                    new("back", new Vector3(0.0f, 0.75f, 30.0f), new Vector3(0.0f, 0.5f, 0.0f), Vector3.UnitY)
                };
            var tourIndex = -1;
            var tourSettleFrames = 0;
            var tourScreenshotPending = false;
            var tourDone = false;

            while (!window.ShouldClose)
            {
                timing.Tick();
                window.PumpEvents();
                input.BeginFrame();

                // Drain any commands that arrived from the MCP server.
                var processed = queue.ProcessPending();
                if (processed > 0)
                    Console.WriteLine($"Processed {processed} AI command(s)");

                if (window.Width != lastWidth || window.Height != lastHeight)
                {
                    lastWidth = window.Width;
                    lastHeight = window.Height;
                    renderContext.Resize(lastWidth, lastHeight);
                    ref var camera = ref cameraEntity.Ensure<Camera>();
                    camera.AspectRatio = (float)lastWidth / lastHeight;
                }

                // Toggle camera controller on F key press.
                if (input.IsKeyPressed(Key.F))
                {
                    activeControllerIndex = (activeControllerIndex + 1) % cameraControllers.Length;
                    cameraController = cameraControllers[activeControllerIndex];
                    Console.WriteLine($"Active camera controller: {cameraController.Name}");
                }

                // Update the active camera controller from input, unless the camera tour is driving the pose.
                if (!cameraTour)
                    cameraController.Update(input, (float)timing.DeltaTime);

                if (cameraTour && !tourDone)
                {
                    if (tourIndex < 0)
                    {
                        tourIndex = 0;
                        SetCameraPose(cameraEntity, tourPoses[tourIndex]);
                        tourSettleFrames = 0;
                        tourScreenshotPending = true;
                    }

                    // Hold the pose for a few frames to let the GPU settle, then screenshot.
                    if (tourScreenshotPending)
                    {
                        tourSettleFrames++;
                        if (tourSettleFrames >= 5)
                        {
                            var path = $"Screenshots/tour_{tourPoses[tourIndex].Name}.png";
                            renderer.RequestScreenshot(path);
                            Console.WriteLine($"Tour screenshot: {path}");
                            tourScreenshotPending = false;
                        }
                    }

                    // After the screenshot has been saved, advance to the next pose.
                    if (!tourScreenshotPending && !renderer.IsScreenshotRequested)
                    {
                        tourIndex++;
                        if (tourIndex >= tourPoses.Length)
                        {
                            tourDone = true;
                            Console.WriteLine("Camera tour complete.");
                            window.Close();
                        }
                        else
                        {
                            SetCameraPose(cameraEntity, tourPoses[tourIndex]);
                            tourSettleFrames = 0;
                            tourScreenshotPending = true;
                        }
                    }
                }

                // Object manipulation (Unity-like drag)
                if (!cameraTour)
                {
                    var cam = cameraEntity.Get<Camera>();
                    objectManipulator.ProcessInput(world, cam, input);
                }

                // Physics: create bodies, step, sync transforms
                if (!cameraTour)
                {
                    var toInit = new List<(Entity, RigidBody, Transform)>();
                    world.Each((Entity e, ref RigidBody rb, ref Transform t) =>
                    {
                        if (!rb.IsInitialized)
                            toInit.Add((e, rb, t));
                    });

                    foreach (var (e, rbData, t) in toInit)
                    {
                        physicsWorld.CreateBody(e, rbData, t);
                        var rb = rbData;
                        rb.IsInitialized = true;
                        e.Set(rb);
                    }

                    // Sync dragged object to physics before stepping
                    objectManipulator.SyncToPhysics(physicsWorld);

                    physicsWorld.Update((float)timing.DeltaTime);

                    // Sync all transforms EXCEPT the dragged object
                    physicsWorld.SyncTransforms(world, objectManipulator.IsDragging ? objectManipulator.GetDraggedEntity() : null);
                }

                // Capture a demo screenshot after the scene warms up (non-tour mode only).
                if (!demoScreenshotRequested && !cameraTour && frames >= 15)
                {
                    renderer.RequestScreenshot("Screenshots/demo.png");
                    demoScreenshotRequested = true;
                }

                // Feed frame data to ImGui before rendering.
                if (imGuiLayer != null)
                    imGuiLayer.SetFrameData(world, timing, currentFps);

            objectManipulator.SetImGuiAvailable(imGuiLayer != null);

                renderer.RenderWorld(world);
                queue.CompletePendingScreenshots();

                // Swap buffers for OpenGL backend
                if (renderContext is OpenTKRenderContext otkCtx)
                    otkCtx.SwapBuffers();
                // Raylib handles swap internally in EndDrawing

                frames++;
                if (timing.TotalTime - lastFpsTime >= 1.0)
                {
                    currentFps = frames;
                    Console.WriteLine($"FPS: {frames}, Delta: {timing.DeltaTime * 1000.0:F2} ms");
                    frames = 0;
                    lastFpsTime = timing.TotalTime;
                }
            }

            Console.WriteLine("Shutting down...");
            imGuiLayer?.Dispose();
#if !RELEASE_AOT
            if (mcpApp != null)
                await mcpApp.StopAsync();
            if (mcpTask != null)
                await mcpTask;
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex}");
            Environment.Exit(1);
        }
    }

    private static void CreateDemoScene(World world, Mesh mesh)
    {
        var sphere = ProceduralMesh.CreateSphere(0.5f, 32, 16, new Vector3(0.8f, 0.8f, 0.8f));
        var torusKnot = ObjLoader.Load("Content/torusknot.obj", new Vector3(0.8f, 0.8f, 0.8f));

        var cubes = new (string name, Vector3 pos, Vector3 color, float scale, float rough, float metal)[]
        {
            ("CubeCenter", new Vector3(0, 5f, 0), new Vector3(0.9f, 0.6f, 0.3f), 0.5f, 0.3f, 0.1f),
            ("CubeRed", new Vector3(0.3f, 7f, 0.3f), new Vector3(0.85f, 0.15f, 0.15f), 0.5f, 0.4f, 0.2f),
            ("CubeGreen", new Vector3(-0.3f, 9f, -0.3f), new Vector3(0.2f, 0.8f, 0.3f), 0.5f, 0.5f, 0.0f),
            ("CubeBlue", new Vector3(0.1f, 11f, 0.1f), new Vector3(0.2f, 0.4f, 0.9f), 0.6f, 0.2f, 0.3f),
            ("CubeYellow", new Vector3(-0.2f, 13f, 0.2f), new Vector3(0.95f, 0.85f, 0.2f), 0.5f, 0.6f, 0.0f),
            ("CubeOrange", new Vector3(0.15f, 15f, -0.1f), new Vector3(0.95f, 0.5f, 0.1f), 0.45f, 0.5f, 0.1f),
        };

        foreach (var (name, pos, color, scale, rough, metal) in cubes)
        {
            world.Entity(name)
                .Set(new Transform(pos, Quaternion.Identity, new Vector3(scale)))
                .Set(mesh)
                .Set(new Material(color, roughness: rough, metallic: metal))
                .Set(RigidBody.DynamicBox(new Vector3(scale * 0.5f), mass: scale * 2f));
        }

        var spheres = new (string name, Vector3 pos, Vector3 color, float scale, float rough, float metal)[]
        {
            ("SphereGold",   new Vector3(3, 6f, -2), new Vector3(1.0f, 0.85f, 0.4f),  1.0f, 0.1f, 1.0f),
            ("SphereChrome", new Vector3(-3, 8f, 0),  new Vector3(0.9f, 0.9f, 0.95f), 1.0f, 0.05f, 1.0f),
            ("SphereRed",    new Vector3(3, 10f, 2),  new Vector3(0.9f, 0.1f, 0.1f),  1.0f, 0.4f, 0.0f),
        };

        foreach (var (name, pos, color, scale, rough, metal) in spheres)
        {
            world.Entity(name)
                .Set(new Transform(pos, Quaternion.Identity, new Vector3(scale)))
                .Set(sphere)
                .Set(new Material(color, roughness: rough, metallic: metal))
                .Set(RigidBody.DynamicSphere(scale * 0.5f, mass: scale));
        }

        world.Entity("TorusKnot")
            .Set(new Transform(new Vector3(0, 0.5f, -6), Quaternion.Identity, new Vector3(1.5f)))
            .Set(torusKnot)
            .Set(new Material(new Vector3(0.9f, 0.9f, 0.9f), roughness: 0.25f, metallic: 0.6f, texturePath: "Content/checker.png"));

        world.Entity("CubeTextured")
            .Set(new Transform(new Vector3(-4, 0.5f, -3), Quaternion.Identity, new Vector3(0.7f)))
            .Set(mesh)
            .Set(new Material(new Vector3(0.8f, 0.8f, 0.85f), roughness: 0.4f, metallic: 0.0f, texturePath: "Content/checker.png"));

        world.Entity("Floor")
            .Set(new Transform(new Vector3(0, -0.5f, 0), Quaternion.Identity, new Vector3(20, 0.5f, 20)))
            .Set(mesh)
            .Set(new Material(new Vector3(0.45f, 0.45f, 0.5f), roughness: 0.8f, metallic: 0.0f))
            .Set(RigidBody.StaticPlane(20f));

        world.Entity("Grid")
            .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, Vector3.One))
            .Set(ProceduralMesh.CreateGrid(20, 1.0f, new Vector3(0.5f, 0.5f, 0.55f)))
            .Set(new Material(new Vector3(0.5f, 0.5f, 0.55f), roughness: 0.9f, metallic: 0.0f));
    }

    private static void CreateCalibrationScene(World world, Mesh mesh)
    {
        // Colored cubes at known world positions for visual analysis of perspective and camera movement.
        var positions = new (string name, Vector3 pos, Vector3 color)[]
        {
            ("CubeOrigin", new Vector3(0.0f, 0.5f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f)),      // white at origin
            ("CubeRight", new Vector3(2.0f, 0.5f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f)),       // red +X
            ("CubeLeft", new Vector3(-2.0f, 0.5f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f)),       // green -X
            ("CubeFront", new Vector3(0.0f, 0.5f, 2.0f), new Vector3(0.0f, 0.0f, 1.0f)),       // blue +Z
            ("CubeBack", new Vector3(0.0f, 0.5f, -2.0f), new Vector3(1.0f, 1.0f, 0.0f)),       // yellow -Z
            ("CubeUp", new Vector3(0.0f, 2.5f, 0.0f), new Vector3(1.0f, 0.0f, 1.0f)),          // magenta +Y
            ("CubeFar", new Vector3(0.0f, 0.5f, 8.0f), new Vector3(0.0f, 1.0f, 1.0f)),        // cyan far +Z
            ("CubeFarLeft", new Vector3(-5.0f, 0.5f, 5.0f), new Vector3(0.5f, 0.5f, 1.0f))     // light blue far corner
        };

        foreach (var (name, pos, color) in positions)
        {
            world.Entity(name)
                .Set(new Transform(pos, Quaternion.Identity, new Vector3(0.5f)))
                .Set(mesh)
                .Set(new Material(color, roughness: 0.5f, metallic: 0.1f));
        }

        // A large reference grid at Y=0.
        world.Entity("Grid")
            .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, Vector3.One))
            .Set(ProceduralMesh.CreateGrid(20, 1.0f, new Vector3(0.5f, 0.5f, 0.55f)))
            .Set(new Material(new Vector3(0.5f, 0.5f, 0.55f), roughness: 0.9f, metallic: 0.0f));
    }

    private static Mesh LoadModel(string path)
    {
        return path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
                ? GltfLoader.Load(path, new Vector3(0.7f, 0.6f, 0.5f))
                : ObjLoader.Load(path, new Vector3(0.7f, 0.6f, 0.5f));
    }

    private static (string modelPath, int mcpPort) ParseArgs(string[] args)
    {
        var modelPath = FindModelPath(args);
        var mcpPort = 5000;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--mcp-port" && i + 1 < args.Length && int.TryParse(args[i + 1], out var port))
            {
                mcpPort = port;
                break;
            }
        }

        return (modelPath, mcpPort);
    }

    private static string FindModelPath(string[] args)
    {
        // Skip recognized flags so they are not treated as a model path.
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg == "--mcp-port")
            {
                i++; // skip the value
                continue;
            }

            if (File.Exists(arg))
                return arg;
        }

        var candidates = new[]
        {
            "Content/cube.obj",
            "Models/cube.obj",
            "cube.obj"
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        throw new FileNotFoundException("No model file found. Pass a .obj/.gltf/.glb path as argument or place Content/cube.obj next to the executable.");
    }

    private static void RunMcpStdioServer()
    {
        Console.WriteLine("Starting headless stdio MCP server...");
        using var world = World.Create();
        var processor = new AiCommandProcessor(world, LoadModel, _ => { });
        var server = new Engine.AI.Stdio.McpStdioServer(processor);
        server.Run();
    }

    private readonly record struct CameraPose(string Name, Vector3 Position, Vector3 Target, Vector3 Up, float Fov = MathF.PI / 12.0f);

    private static void SetCameraPose(Entity cameraEntity, CameraPose pose)
    {
        ref var camera = ref cameraEntity.Ensure<Camera>();
        camera.Position = pose.Position;
        camera.Target = pose.Target;
        camera.Up = pose.Up;
        camera.FieldOfView = pose.Fov;
        cameraEntity.Set(camera);
        Console.WriteLine($"Camera pose '{pose.Name}': pos={pose.Position}, target={pose.Target}, up={pose.Up}, fov={pose.Fov * 180f / MathF.PI:F0}°");
    }
}
