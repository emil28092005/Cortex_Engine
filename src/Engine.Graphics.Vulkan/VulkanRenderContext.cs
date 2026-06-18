using Engine.Core;

namespace Engine.Graphics.Vulkan;

internal sealed class VulkanRenderContext : IRenderContext{
    private readonly VulkanContext _ctx;
    private readonly VulkanSwapchain _swapchain;
    private readonly IWindow _window;
    private bool _disposed;

    public IWindow Window => _window;

    public VulkanRenderContext(IWindow window, bool enableValidation)
    {
        _window = window;
        _ctx = new VulkanContext(window, enableValidation);

        var surfaceFormat = new VkSurfaceFormatKHR
        {
            format = _ctx.SurfaceFormat,
            colorSpace = _ctx.SurfaceColorSpace,
        };

        _swapchain = new VulkanSwapchain(_ctx.Device, _ctx.PhysicalDevice, _ctx.Surface,
            surfaceFormat, window.Width, window.Height, _ctx);
    }

    public IRenderer CreateRenderer()
    {
        return new VulkanRenderer(_ctx, _swapchain);
    }

    public void Resize(int width, int height)
    {
        _swapchain.Recreate(width, height);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _swapchain?.Dispose();
        _ctx?.Dispose();
        _window?.Dispose();
    }
}
