using Engine.Core;
using Engine.Graphics;

namespace Engine.Graphics.Vulkan;

public sealed class VulkanRenderContext : IRenderContext
{
    private readonly VulkanContext _context;
    private readonly VulkanSwapchain _swapchain;
    private readonly VulkanPipeline _pipeline;
    private readonly VulkanRenderer _renderer;
    private readonly Sdl3Window _window;

    public IWindow Window => _window;

    public VulkanRenderContext(int width, int height, bool enableValidation)
    {
        _window = new Sdl3Window("Cortex Engine", width, height, vulkanSurface: true);
        _context = new VulkanContext(_window, enableValidation);
        _swapchain = new VulkanSwapchain(_context, width, height);
        _pipeline = new VulkanPipeline(_context, _swapchain.RenderPass);
        _renderer = new VulkanRenderer(_context, _swapchain, _pipeline);
    }

    public IRenderer CreateRenderer() => _renderer;

    public void Resize(int width, int height)
    {
        _renderer.OnResize();
    }

    public void Dispose()
    {
        _renderer.Dispose();
        _pipeline.Dispose();
        _swapchain.Dispose();
        _context.Dispose();
        _window.Dispose();
    }
}
