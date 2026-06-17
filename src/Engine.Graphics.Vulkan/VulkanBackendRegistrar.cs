using Engine.Graphics;

namespace Engine.Graphics.Vulkan;

public static class VulkanBackendRegistrar
{
    private static int _registered;

    public static void EnsureRegistered()
    {
        if (System.Threading.Interlocked.Exchange(ref _registered, 1) == 1) return;

        RenderBackendFactory.Register("vulkan", (width, height, validation) =>
            new VulkanRenderContext(width, height, validation));
    }
}
