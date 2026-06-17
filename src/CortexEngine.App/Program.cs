using Engine.Core;
using Engine.Graphics;
using Engine.Graphics.Vulkan;
using Flecs.NET.Core;

namespace CortexEngine.App;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Cortex Engine — Vulkan Triangle (pure P/Invoke)...");

        try
        {
            VulkanBackendRegistrar.EnsureRegistered();
            using var renderContext = RenderBackendFactory.Create("vulkan", 1280, 720, enableValidation: true);
            var window = renderContext.Window;
            using var renderer = renderContext.CreateRenderer();

            using var world = World.Create();

            var lastWidth = window.Width;
            var lastHeight = window.Height;
            var frames = 0;
            var lastFpsTime = 0.0;
            var timing = new Timing();

            while (!window.ShouldClose)
            {
                timing.Tick();
                window.PumpEvents();

                if (window.Width != lastWidth || window.Height != lastHeight)
                {
                    lastWidth = window.Width;
                    lastHeight = window.Height;
                    renderContext.Resize(lastWidth, lastHeight);
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
}
