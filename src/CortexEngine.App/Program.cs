using System;
using System.IO;
using System.Numerics;
using Engine.AI;
#if !RELEASE_AOT
using Engine.AI.Mcp;
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
        Console.WriteLine("Cortex Engine Step 6 — MCP integration...");

        try
        {
            using var world = World.Create();
            using var window = new Sdl3Window("Cortex Engine — Step 6", 1280, 720);
            var timing = new Timing();
            var input = new InputMapping();
            using var vulkan = new VulkanContext(window, enableValidation: false);
            using var swapchain = new Swapchain(vulkan);
            using var renderer = new MeshRenderer(vulkan, swapchain);

            var (modelPath, mcpPort) = ParseArgs(args);
            var mesh = LoadModel(modelPath);

            var processor = new AiCommandProcessor(world, LoadModel, path => renderer.RequestScreenshot(path));
            var queue = new AiCommandQueue(processor);

            var model = world.Entity("Model")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, new Vector3(0.5f)))
                .Set(mesh);

            var camera = world.Entity("Camera")
                .Set(new Camera(
                    new Vector3(0.0f, 0.0f, -2.0f),
                    Vector3.Zero,
                    Vector3.UnitY,
                    MathF.PI / 4.0f,
                    1280.0f / 720.0f,
                    0.1f,
                    100.0f));

            // Demo: local AI commands processed on the main thread.
            Console.WriteLine("AI demo commands:");
            Console.WriteLine(processor.Process("""{ "type": "list_entities" }""").Message);
            Console.WriteLine(processor.Process("""{ "type": "spawn_model", "name": "SecondCube", "modelPath": "Content/cube.obj", "position": [0.8, 0, 0], "scale": [0.3, 0.3, 0.3] }""").Message);
            Console.WriteLine(processor.Process("""{ "type": "list_entities" }""").Message);
            Console.WriteLine(processor.Process("""{ "type": "set_transform", "name": "SecondCube", "position": [0.8, 0.5, 0], "rotation": [0, 0, 0, 1], "scale": [0.3, 0.3, 0.3] }""").Message);
            Console.WriteLine(processor.Process("""{ "type": "capture_screenshot", "outputPath": "Screenshots/demo.png" }""").Message);

#if !RELEASE_AOT
            // Start the MCP server in the background so AI agents can connect via HTTP.
            var mcpApp = McpEngineServerHost.Create(args, queue, port: mcpPort);
            var mcpTask = mcpApp.RunAsync();
            _ = mcpTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                    Console.WriteLine($"MCP server error: {t.Exception?.GetBaseException().Message}");
                else if (t.IsCanceled)
                    Console.WriteLine("MCP server canceled.");
                else
                    Console.WriteLine("MCP server stopped.");
            }, TaskScheduler.Default);
            Console.WriteLine($"MCP server starting on http://localhost:{mcpPort}");
#endif

            var frames = 0;
            var lastFpsTime = 0.0;
            var lastWidth = window.Width;
            var lastHeight = window.Height;

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
                    swapchain.Recreate(lastWidth, lastHeight);
                }

                // Slowly rotate the model so we can see it in 3D.
                ref var transform = ref model.Ensure<Transform>();
                transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)timing.TotalTime * 0.5f)
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
            await mcpApp.StopAsync();
            await mcpTask;
#else
            await Task.CompletedTask;
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
}
