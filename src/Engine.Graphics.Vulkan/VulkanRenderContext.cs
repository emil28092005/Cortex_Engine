using Engine.Core;
using Engine.Graphics;

namespace Engine.Graphics.Vulkan;

/// <summary>
/// Vulkan implementation of the render HAL context.
/// Creates and owns an <see cref="Sdl3Window"/> for the Vulkan surface.
/// </summary>
public sealed class VulkanRenderContext : IRenderContext
{
    private readonly Sdl3Window _window;
    private readonly VulkanContext _context;
    private readonly Swapchain _swapchain;

    public IWindow Window => _window;

    public VulkanRenderContext(int width, int height, bool enableValidation)
    {
        _window = new Sdl3Window("Cortex Engine", width, height, vulkanSurface: true);
        _context = new VulkanContext(_window, enableValidation);
        _swapchain = new Swapchain(_context);
    }

    public IRenderer CreateRenderer() => new VulkanRenderer(_context, _swapchain);

    public void Resize(int width, int height) => _swapchain.Recreate(width, height);

    public void Dispose()
    {
        _swapchain.Dispose();
        _context.Dispose();
        _window.Dispose();
    }
}
