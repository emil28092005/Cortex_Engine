using Engine.Graphics;

namespace Engine.Graphics.Vulkan;

/// <summary>
/// Triggers registration of the Vulkan backend with the HAL factory.
/// </summary>
public static class VulkanBackendRegistrar
{
    static VulkanBackendRegistrar()
    {
        RenderBackendFactory.Register("vulkan", (width, height, enableValidation) => new VulkanRenderContext(width, height, enableValidation));
    }

    /// <summary>
    /// No-op method that forces the static constructor to run.
    /// Call this before using <see cref="RenderBackendFactory.Create"/>.
    /// </summary>
    public static void EnsureRegistered() { }
}
