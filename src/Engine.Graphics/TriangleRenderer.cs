using System;
using Vortice.Vulkan;

namespace Engine.Graphics;

/// <summary>
/// Renders a colored triangle using a vertex buffer and a simple graphics pipeline.
/// </summary>
public sealed unsafe class TriangleRenderer : IDisposable
{
    private readonly VulkanContext _context;
    private readonly Swapchain _swapchain;
    private readonly VulkanPipeline _pipeline;
    private readonly VertexBuffer _vertexBuffer;
    private VkCommandPool _commandPool;
    private VkCommandBuffer[] _commandBuffers = null!;
    private VkSemaphore[] _imageAvailableSemaphores = null!;
    private VkSemaphore[] _renderFinishedSemaphores = null!;
    private VkFence[] _inFlightFences = null!;
    private int _currentFrame;

    public TriangleRenderer(VulkanContext context, Swapchain swapchain)
    {
        _context = context;
        _swapchain = swapchain;

        _pipeline = new VulkanPipeline(context, swapchain);
        _vertexBuffer = CreateTriangleBuffer();

        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    private VertexBuffer CreateTriangleBuffer()
    {
        var vertices = new[]
        {
            // Position (vec2) + Color (vec3)
             0.0f, -0.5f,  1.0f, 0.0f, 0.0f,
             0.5f,  0.5f,  0.0f, 1.0f, 0.0f,
            -0.5f,  0.5f,  0.0f, 0.0f, 1.0f
        };

        var bytes = new byte[vertices.Length * sizeof(float)];
        fixed (byte* p = bytes)
        fixed (float* v = vertices)
        {
            Buffer.MemoryCopy(v, p, bytes.Length, vertices.Length * sizeof(float));
        }

        return new VertexBuffer(_context, bytes);
    }

    private void CreateCommandPool()
    {
        var createInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.CommandPoolCreateInfo,
            queueFamilyIndex = _context.GraphicsFamilyIndex,
            flags = VkCommandPoolCreateFlags.ResetCommandBuffer
        };

        var result = _context.DeviceApi.vkCreateCommandPool(&createInfo, null, out _commandPool);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateCommandPool failed: {result}");
    }

    private void CreateCommandBuffers()
    {
        _commandBuffers = new VkCommandBuffer[2];
        for (var i = 0; i < _commandBuffers.Length; i++)
        {
            var allocInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo,
                commandPool = _commandPool,
                level = VkCommandBufferLevel.Primary,
                commandBufferCount = 1
            };

            var result = _context.DeviceApi.vkAllocateCommandBuffer(&allocInfo, out _commandBuffers[i]);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkAllocateCommandBuffers failed: {result}");
        }
    }

    private void CreateSyncObjects()
    {
        _imageAvailableSemaphores = new VkSemaphore[2];
        _renderFinishedSemaphores = new VkSemaphore[2];
        _inFlightFences = new VkFence[2];

        var semaphoreInfo = new VkSemaphoreCreateInfo { sType = VkStructureType.SemaphoreCreateInfo };
        var fenceInfo = new VkFenceCreateInfo
        {
            sType = VkStructureType.FenceCreateInfo,
            flags = VkFenceCreateFlags.Signaled
        };

        for (var i = 0; i < 2; i++)
        {
            _context.DeviceApi.vkCreateSemaphore(&semaphoreInfo, null, out _imageAvailableSemaphores[i]);
            _context.DeviceApi.vkCreateSemaphore(&semaphoreInfo, null, out _renderFinishedSemaphores[i]);
            _context.DeviceApi.vkCreateFence(&fenceInfo, null, out _inFlightFences[i]);
        }
    }

    public void RenderFrame()
    {
        var frame = _currentFrame % 2;

        _context.DeviceApi.vkWaitForFences(_inFlightFences[frame], true, ulong.MaxValue);
        _context.DeviceApi.vkResetFences(_inFlightFences[frame]);

        var result = _context.DeviceApi.vkAcquireNextImageKHR(
            _swapchain.Handle,
            ulong.MaxValue,
            _imageAvailableSemaphores[frame],
            VkFence.Null,
            out var imageIndex);

        if (result == VkResult.ErrorOutOfDateKHR)
            return;

        var cmd = _commandBuffers[frame];
        _context.DeviceApi.vkResetCommandBuffer(cmd, VkCommandBufferResetFlags.None);

        var beginInfo = new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.CommandBufferBeginInfo,
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };
        _context.DeviceApi.vkBeginCommandBuffer(cmd, &beginInfo);

        var clearColor = new VkClearValue(0.0f, 0.0f, 0.0f, 1.0f);

        var renderPassInfo = new VkRenderPassBeginInfo
        {
            sType = VkStructureType.RenderPassBeginInfo,
            renderPass = _swapchain.RenderPass,
            framebuffer = _swapchain.Framebuffers[imageIndex],
            renderArea = new VkRect2D(0, 0, _swapchain.Extent.width, _swapchain.Extent.height),
            clearValueCount = 1,
            pClearValues = &clearColor
        };

        _context.DeviceApi.vkCmdBeginRenderPass(cmd, &renderPassInfo, VkSubpassContents.Inline);

        _context.DeviceApi.vkCmdBindPipeline(cmd, VkPipelineBindPoint.Graphics, _pipeline.Handle);

        var viewport = new VkViewport(0, 0, _swapchain.Extent.width, _swapchain.Extent.height, 0, 1);
        var scissor = new VkRect2D(0, 0, _swapchain.Extent.width, _swapchain.Extent.height);
        _context.DeviceApi.vkCmdSetViewport(cmd, 0, viewport);
        _context.DeviceApi.vkCmdSetScissor(cmd, 0, scissor);

        var vertexBuffer = _vertexBuffer.Buffer;
        var offset = 0ul;
        _context.DeviceApi.vkCmdBindVertexBuffers(cmd, 0, 1, &vertexBuffer, &offset);
        _context.DeviceApi.vkCmdDraw(cmd, 3, 1, 0, 0);

        _context.DeviceApi.vkCmdEndRenderPass(cmd);
        _context.DeviceApi.vkEndCommandBuffer(cmd);

        var waitSemaphore = _imageAvailableSemaphores[frame];
        var signalSemaphore = _renderFinishedSemaphores[frame];
        var stageMask = VkPipelineStageFlags.ColorAttachmentOutput;
        var submitInfo = new VkSubmitInfo
        {
            sType = VkStructureType.SubmitInfo,
            waitSemaphoreCount = 1,
            pWaitSemaphores = &waitSemaphore,
            pWaitDstStageMask = &stageMask,
            commandBufferCount = 1,
            pCommandBuffers = &cmd,
            signalSemaphoreCount = 1,
            pSignalSemaphores = &signalSemaphore
        };

        _context.DeviceApi.vkQueueSubmit(_context.GraphicsQueue, 1, &submitInfo, _inFlightFences[frame]);

        var swapchain = _swapchain.Handle;
        var presentInfo = new VkPresentInfoKHR
        {
            sType = VkStructureType.PresentInfoKHR,
            waitSemaphoreCount = 1,
            pWaitSemaphores = &signalSemaphore,
            swapchainCount = 1,
            pSwapchains = &swapchain,
            pImageIndices = &imageIndex
        };

        _context.DeviceApi.vkQueuePresentKHR(_context.PresentQueue, &presentInfo);
        _currentFrame++;
    }

    public void Dispose()
    {
        _context.DeviceApi.vkDeviceWaitIdle();

        for (var i = 0; i < 2; i++)
        {
            if (_renderFinishedSemaphores[i] != VkSemaphore.Null)
                _context.DeviceApi.vkDestroySemaphore(_renderFinishedSemaphores[i]);
            if (_imageAvailableSemaphores[i] != VkSemaphore.Null)
                _context.DeviceApi.vkDestroySemaphore(_imageAvailableSemaphores[i]);
            if (_inFlightFences[i] != VkFence.Null)
                _context.DeviceApi.vkDestroyFence(_inFlightFences[i]);
        }

        if (_commandPool != VkCommandPool.Null)
            _context.DeviceApi.vkDestroyCommandPool(_commandPool);

        _vertexBuffer.Dispose();
        _pipeline.Dispose();
    }
}
