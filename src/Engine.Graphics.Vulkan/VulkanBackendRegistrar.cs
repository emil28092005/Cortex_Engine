using Engine.Core;
using Engine.Graphics;

namespace Engine.Graphics.Vulkan;

public static class VulkanBackendRegistrar
{
    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered) return;
        _registered = true;

        RenderBackendFactory.Register("vulkan", (width, height, enableValidation) =>
        {
            var window = new Sdl3Window("Cortex Engine — Vulkan", width, height, vulkanSurface: true);
            return new VulkanRenderContext(window, enableValidation);
        });

        Console.WriteLine("[Vulkan] Backend registered as 'vulkan'");
    }
}
