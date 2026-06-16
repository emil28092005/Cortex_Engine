using System;
using Engine.Core;
using Engine.Graphics;

namespace CortexEngine.App;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Cortex Engine Step 2 — Starting up...");

        try
        {
            using var window = new Sdl3Window("Cortex Engine — Step 1", 1280, 720);
            var timing = new Timing();
            var input = new InputMapping();
            using var vulkan = new VulkanContext(window, enableValidation: true);
            using var swapchain = new Swapchain(vulkan);
            using var renderer = new TriangleRenderer(vulkan, swapchain);

            var frames = 0;
            var lastFpsTime = 0.0;

            while (!window.ShouldClose)
            {
                timing.Tick();
                window.PumpEvents();
                input.BeginFrame();
                // Note: SDL events are already polled in PumpEvents.
                // In a real engine, the window would expose an event iterator.

                renderer.RenderFrame();

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
