using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Engine.AI;
using SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
#if !RELEASE_AOT
using Engine.AI.Mcp;
using Microsoft.AspNetCore.Builder;
#endif
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Engine.Graphics.Loaders;
using Flecs.NET.Core;

namespace CortexEngine.App;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Cortex Engine — Materials, Grid, Lighting, Orbit Camera...");

        try
        {
            if (args.Contains("--mcp-stdio"))
            {
                RunMcpStdioServer();
                return;
            }

            using var world = World.Create();
            using var window = new Sdl3Window("Cortex Engine", 1280, 720);
            var timing = new Timing();
            var input = new InputMapping();
            using var vulkan = new VulkanContext(window, enableValidation: false);
            using var swapchain = new Swapchain(vulkan);
            using var renderer = new MeshRenderer(vulkan, swapchain);

            var (modelPath, mcpPort) = ParseArgs(args);
            var mesh = LoadModel(modelPath);

            var processor = new AiCommandProcessor(world, LoadModel, path => renderer.RequestScreenshot(path));
            var queue = new AiCommandQueue(processor);

            var cameraEntity = world.Entity("Camera")
                .Set(new Transform(new Vector3(0.0f, 2.5f, -4.0f), Quaternion.Identity, Vector3.One))
                .Set(new Camera(
                    new Vector3(0.0f, 2.5f, -4.0f),
                    new Vector3(0.0f, 0.5f, 0.0f),
                    Vector3.UnitY,
                    MathF.PI / 4.0f,
                    1280.0f / 720.0f,
                    0.1f,
                    100.0f));

            world.Entity("MainLight")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, Vector3.One))
                .Set(new Light(new Vector3(0.5f, -1.0f, -0.5f), new Vector3(1.0f, 0.95f, 0.8f), 1.0f));

            world.Entity("FillLight")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, Vector3.One))
                .Set(new Light(new Vector3(-0.8f, -0.6f, 0.3f), new Vector3(0.3f, 0.4f, 0.6f), 0.6f));

            world.Entity("FrontLight")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, Vector3.One))
                .Set(new Light(new Vector3(0.0f, -0.3f, -1.0f), new Vector3(0.8f, 0.8f, 0.9f), 0.4f));

            world.Entity("GroundLight")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, Vector3.One))
                .Set(new Light(new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.15f, 0.15f, 0.2f), 0.3f));

            ICameraController[] cameraControllers =
            [
                new OrbitCameraController(cameraEntity, new Vector3(0.0f, 0.5f, 0.0f)),
                new FreeFlyCameraController(cameraEntity)
            ];
            var activeControllerIndex = 0;

            var texturePath = GenerateCheckerboardTexture("Content/checkerboard.png", 256);

            var model = world.Entity("Model")
                .Set(new Transform(new Vector3(0.0f, 0.5f, 0.0f), Quaternion.Identity, new Vector3(0.5f)))
                .Set(mesh)
                .Set(new Material(new Vector3(0.9f, 0.6f, 0.3f), roughness: 0.4f, metallic: 0.1f));

            var floor = world.Entity("Floor")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, Vector3.One))
                .Set(CreateFloorMesh(20.0f, new Vector3(0.8f, 0.8f, 0.85f)))
                .Set(new Material(new Vector3(0.8f, 0.8f, 0.85f), roughness: 0.9f, metallic: 0.0f, texturePath: texturePath));

            var grid = world.Entity("Grid")
                .Set(new Transform(new Vector3(0.0f, 0.01f, 0.0f), Quaternion.Identity, Vector3.One))
                .Set(CreateGridMesh(20, 0.5f, new Vector3(0.5f, 0.5f, 0.55f)))
                .Set(new Material(new Vector3(0.5f, 0.5f, 0.55f), roughness: 0.9f, metallic: 0.0f));

            // Demo: local AI commands processed on the main thread.
            Console.WriteLine("AI demo commands:");
            Console.WriteLine(processor.Process("""{ "type": "list_entities" }""").Message);
            Console.WriteLine(processor.Process("""{ "type": "spawn_model", "name": "SecondCube", "modelPath": "Content/cube.obj", "position": [0.8, 0, 0], "scale": [0.3, 0.3, 0.3] }""").Message);
            Console.WriteLine(processor.Process("""{ "type": "set_transform", "name": "SecondCube", "position": [0.8, 0.5, 0], "rotation": [0, 0, 0, 1], "scale": [0.3, 0.3, 0.3] }""").Message);

            var secondCube = world.Lookup("SecondCube");
            if ((ulong)secondCube.Id != 0)
                secondCube.Set(new Material(new Vector3(0.3f, 0.7f, 0.9f), roughness: 0.3f, metallic: 0.2f));

            Console.WriteLine(processor.Process("""{ "type": "list_entities" }""").Message);
            Console.WriteLine(processor.Process("""{ "type": "get_world_state" }""").Message);
            Console.WriteLine(processor.Process("""{ "type": "capture_screenshot", "outputPath": "Screenshots/demo.png" }""").Message);

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

            while (!window.ShouldClose)
            {
                timing.Tick();
                window.PumpEvents(input);
                input.BeginFrame();

                // Drain any commands that arrived from the MCP server.
                var processed = queue.ProcessPending();
                if (processed > 0)
                    Console.WriteLine($"Processed {processed} AI command(s)");

                if (window.Width != lastWidth || window.Height != lastHeight)
                {
                    lastWidth = window.Width;
                    lastHeight = window.Height;
                    swapchain.Recreate(lastWidth, lastHeight);
                    ref var camera = ref cameraEntity.Ensure<Camera>();
                    camera.AspectRatio = (float)lastWidth / lastHeight;
                }

                // Toggle camera controller with F.
                if (input.IsKeyPressed(SDL_Keycode.SDLK_F))
                {
                    activeControllerIndex = (activeControllerIndex + 1) % cameraControllers.Length;
                    Console.WriteLine($"Camera controller: {cameraControllers[activeControllerIndex].Name}");
                }

                // Update active camera controller from input.
                cameraControllers[activeControllerIndex].Update(input, (float)timing.DeltaTime);

                // Slowly rotate the model so we can see it in 3D.
                ref var modelTransform = ref model.Ensure<Transform>();
                modelTransform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)timing.TotalTime * 0.5f)
                                      * Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)timing.TotalTime * 0.25f);

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

    private static Mesh LoadModel(string path)
    {
        return path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
                ? GltfLoader.Load(path, new Vector3(0.7f, 0.6f, 0.5f))
                : ObjLoader.Load(path, new Vector3(0.7f, 0.6f, 0.5f));
    }

    private static Mesh CreateGridMesh(int lines, float spacing, Vector3 color)
    {
        var vertices = new List<Vertex>();
        var indices = new List<uint>();
        var extent = lines * spacing;
        var normal = Vector3.UnitY;
        var halfWidth = 0.02f;

        for (var i = -lines; i <= lines; i++)
        {
            var offset = i * spacing;

            // Line parallel to X axis as a thin quad.
            var baseIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(-extent, 0, offset - halfWidth), color, normal));
            vertices.Add(new Vertex(new Vector3(extent, 0, offset - halfWidth), color, normal));
            vertices.Add(new Vertex(new Vector3(extent, 0, offset + halfWidth), color, normal));
            vertices.Add(new Vertex(new Vector3(-extent, 0, offset + halfWidth), color, normal));
            indices.Add(baseIndex); indices.Add(baseIndex + 1); indices.Add(baseIndex + 2);
            indices.Add(baseIndex); indices.Add(baseIndex + 2); indices.Add(baseIndex + 3);

            // Line parallel to Z axis as a thin quad.
            baseIndex = (uint)vertices.Count;
            vertices.Add(new Vertex(new Vector3(offset - halfWidth, 0, -extent), color, normal));
            vertices.Add(new Vertex(new Vector3(offset + halfWidth, 0, -extent), color, normal));
            vertices.Add(new Vertex(new Vector3(offset + halfWidth, 0, extent), color, normal));
            vertices.Add(new Vertex(new Vector3(offset - halfWidth, 0, extent), color, normal));
            indices.Add(baseIndex); indices.Add(baseIndex + 1); indices.Add(baseIndex + 2);
            indices.Add(baseIndex); indices.Add(baseIndex + 2); indices.Add(baseIndex + 3);
        }

        return new Mesh(vertices.ToArray(), indices.ToArray());
    }

    private static Mesh CreateFloorMesh(float size, Vector3 color)
    {
        var half = size / 2.0f;
        var normal = Vector3.UnitY;

        var vertices = new Vertex[]
        {
            new(new Vector3(-half, 0, -half), color, normal),
            new(new Vector3(half, 0, -half), color, normal),
            new(new Vector3(half, 0, half), color, normal),
            new(new Vector3(-half, 0, half), color, normal)
        };

        var indices = new uint[] { 0, 1, 2, 0, 2, 3 };
        return new Mesh(vertices, indices);
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

    private static string GenerateCheckerboardTexture(string path, int size)
    {
        var tileSize = size / 8;
        using var image = new Image<Rgba32>(size, size);
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var tileX = x / tileSize;
                var tileY = y / tileSize;
                var isDark = (tileX + tileY) % 2 == 0;
                image[x, y] = isDark
                    ? new Rgba32(60, 60, 70, 255)
                    : new Rgba32(160, 160, 170, 255);
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        image.SaveAsPng(path);
        return path;
    }

    private static void RunMcpStdioServer()
    {
        Console.WriteLine("Starting headless stdio MCP server...");
        using var world = World.Create();
        var processor = new AiCommandProcessor(world, LoadModel, _ => { });
        var server = new Engine.AI.Stdio.McpStdioServer(processor);
        server.Run();
    }
}
