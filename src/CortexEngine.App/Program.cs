using System;
using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Flecs.NET.Core;

namespace CortexEngine.App;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Cortex Engine Step 3 — Starting up...");

        try
        {
            using var world = World.Create();
            using var window = new Sdl3Window("Cortex Engine — Step 3", 1280, 720);
            var timing = new Timing();
            var input = new InputMapping();
            using var vulkan = new VulkanContext(window, enableValidation: false);
            using var swapchain = new Swapchain(vulkan);
            using var renderer = new TriangleRenderer(vulkan, swapchain);

            var triangle = world.Entity("Triangle")
                .Set(new Transform(new Vector3(0.0f, 0.0f, 0.0f)));

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

                // Animate the entity transform.
                ref var transform = ref triangle.Ensure<Transform>();
                transform.Position = new Vector3(
                    MathF.Sin((float)timing.TotalTime) * 0.5f,
                    0.0f,
                    0.0f);
                transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)timing.TotalTime);
                transform.Scale = Vector3.One * (1.0f + MathF.Sin((float)timing.TotalTime * 2.0f) * 0.2f);

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
}
