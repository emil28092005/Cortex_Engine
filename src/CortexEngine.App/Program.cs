using System;
using System.IO;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Engine.Graphics.Loaders;
using Flecs.NET.Core;

namespace CortexEngine.App;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Cortex Engine Step 4 — Starting up...");

        try
        {
            using var world = World.Create();
            using var window = new Sdl3Window("Cortex Engine — Step 4", 1280, 720);
            var timing = new Timing();
            var input = new InputMapping();
            using var vulkan = new VulkanContext(window, enableValidation: false);
            using var swapchain = new Swapchain(vulkan);
            using var renderer = new MeshRenderer(vulkan, swapchain);

            var modelPath = FindModelPath(args);
            var mesh = modelPath.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)
                || modelPath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
                    ? GltfLoader.Load(modelPath, new Vector3(0.7f, 0.6f, 0.5f))
                    : ObjLoader.Load(modelPath, new Vector3(0.7f, 0.6f, 0.5f));

            var model = world.Entity("Model")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f), Quaternion.Identity, new Vector3(0.5f)))
                .Set(mesh);

            var frames = 0;
            var lastFpsTime = 0.0;
            var lastWidth = window.Width;
            var lastHeight = window.Height;

            while (!window.ShouldClose)
            {
                timing.Tick();
                window.PumpEvents();
                input.BeginFrame();
                // Note: SDL events are already polled in PumpEvents.
                // In a real engine, the window would expose an event iterator.

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex}");
            Environment.Exit(1);
        }
    }

    private static string FindModelPath(string[] args)
    {
        if (args.Length > 0 && File.Exists(args[0]))
            return args[0];

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
