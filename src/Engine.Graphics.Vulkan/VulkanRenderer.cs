using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics.Loaders;
using Flecs.NET.Core;
using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanRenderer : IRenderer, Engine.Graphics.IScreenshotProvider
{
    private readonly VulkanContext _ctx;
    private readonly VulkanSwapchain _swapchain;
    private readonly VulkanPipeline _pipeline;
    private readonly VulkanFrameResources _frameResources;
    private readonly VulkanVertexBuffer _vertexBuffer;
    private readonly VulkanIndexBuffer _indexBuffer;
    private readonly uint _indexCount;

    private int _frameIndex;
    private bool _disposed;
    private bool _screenshotRequested;
    private string? _screenshotPath;
    private float _totalTime;

    public bool IsScreenshotRequested => _screenshotRequested;
    public IScreenshotProvider ScreenshotProvider => this;

    public VulkanRenderer(VulkanContext ctx, VulkanSwapchain swapchain)
    {
        _ctx = ctx;
        _swapchain = swapchain;

        var vertSpv = LoadShader("Shaders/triangle.vert.spv");
        var fragSpv = LoadShader("Shaders/triangle.frag.spv");

        _pipeline = new VulkanPipeline(ctx.Device, swapchain.Format, swapchain.DepthFormat, vertSpv, fragSpv);

        _frameResources = new VulkanFrameResources(ctx.Device, ctx.GraphicsQueueFamilyIndex,
            swapchain.ImageCount, ctx, _pipeline.DescriptorSetLayout);

        var mesh = LoadMesh("Content/cube.obj");
        _indexCount = (uint)mesh.Indices.Length;

        _vertexBuffer = new VulkanVertexBuffer(ctx.Device, ctx.PhysicalDevice,
            _frameResources.CommandPool, ctx.GraphicsQueue, ctx, mesh.Vertices);

        _indexBuffer = new VulkanIndexBuffer(ctx.Device, _frameResources.CommandPool,
            ctx.GraphicsQueue, ctx, mesh.Indices);
    }

    private static Mesh LoadMesh(string path)
    {
        if (!File.Exists(path))
        {
            var altPath = Path.Combine(AppContext.BaseDirectory, path);
            if (!File.Exists(altPath))
            {
                altPath = Path.Combine(AppContext.BaseDirectory, "Content", Path.GetFileName(path));
                if (!File.Exists(altPath))
                    throw new FileNotFoundException($"Mesh file not found: {path}");
            }
            return ObjLoader.Load(altPath, new Vector3(0.8f, 0.6f, 0.3f));
        }
        return ObjLoader.Load(path, new Vector3(0.8f, 0.6f, 0.3f));
    }

    public void RenderWorld(World world)
    {
        Render();
    }

    private void Render()
    {
        _frameResources.WaitFrame(_frameIndex);

        uint imageIndex;
        var acquireResult = Vk.vkAcquireNextImageKHR(_ctx.Device, _swapchain.Swapchain,
            ulong.MaxValue, _frameResources.AcquireSemaphores[_frameIndex], VkFence.Null, &imageIndex);

        if (acquireResult == VkResult.ErrorOutOfDateKHR || acquireResult == VkResult.SuboptimalKHR)
        {
            _swapchain.Recreate(_ctx.SurfaceExtent.Width == 0 ? 1280 : (int)_ctx.SurfaceExtent.Width,
                                _ctx.SurfaceExtent.Height == 0 ? 720 : (int)_ctx.SurfaceExtent.Height);
            Render();
            return;
        }

        if (acquireResult != VkResult.Success)
            throw new InvalidOperationException($"vkAcquireNextImageKHR failed: {acquireResult}");

        _totalTime += 0.016f;

        var vp = ComputeViewProjection();
        _frameResources.UpdateUbo(_frameIndex, &vp, VulkanFrameResources.UboSize);

        var cmd = _frameResources.CommandBuffers[_frameIndex];
        Vk.vkResetCommandBuffer(cmd, 0);

        var beginInfo = new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.CommandBufferBeginInfo,
            flags = VkCommandBufferUsageFlags.OneTimeSubmit,
        };
        Vk.vkBeginCommandBuffer(cmd, &beginInfo);

        TransitionImageLayout(cmd, _swapchain.Images[imageIndex],
            VkImageLayout.Undefined, VkImageLayout.ColorAttachmentOptimal,
            0, 0,
            0x400, 0x100);

        TransitionImageLayoutDepth(cmd, _swapchain.DepthImage,
            VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal,
            0, 0,
            0x100, 0x200);

        var clearValue = new VkClearValue
        {
            Color = new VkClearColorValue { Float0 = 0.02f, Float1 = 0.02f, Float2 = 0.02f, Float3 = 1.0f },
        };

        var depthClearValue = new VkClearValue
        {
            DepthStencil = new VkClearDepthStencilValue { Depth = 1.0f, Stencil = 0 },
        };

        var colorAttachment = new VkRenderingAttachmentInfo
        {
            sType = VkStructureType.RenderingAttachmentInfo,
            imageView = _swapchain.ImageViews[imageIndex],
            imageLayout = VkImageLayout.ColorAttachmentOptimal,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            clearValue = clearValue,
        };

        var depthAttachment = new VkRenderingAttachmentInfo
        {
            sType = VkStructureType.RenderingAttachmentInfo,
            imageView = _swapchain.DepthImageView,
            imageLayout = VkImageLayout.DepthStencilAttachmentOptimal,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            clearValue = depthClearValue,
        };

        var renderingInfo = new VkRenderingInfo
        {
            sType = VkStructureType.RenderingInfo,
            renderArea = new VkRect2D
            {
                Offset = new VkOffset2D { X = 0, Y = 0 },
                Extent = _swapchain.Extent,
            },
            layerCount = 1,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachment,
            pDepthAttachment = &depthAttachment,
        };

        Vk.vkCmdBeginRendering(cmd, &renderingInfo);

        Vk.vkCmdBindPipeline(cmd, VkPipelineBindPoint.Graphics, _pipeline.Pipeline);

        var viewport = new VkViewport
        {
            X = 0, Y = 0,
            Width = _swapchain.Extent.Width,
            Height = _swapchain.Extent.Height,
            MinDepth = 0, MaxDepth = 1,
        };
        Vk.vkCmdSetViewport(cmd, 0, 1, &viewport);

        var scissor = new VkRect2D
        {
            Offset = new VkOffset2D { X = 0, Y = 0 },
            Extent = _swapchain.Extent,
        };
        Vk.vkCmdSetScissor(cmd, 0, 1, &scissor);

        var descSet = _frameResources.DescriptorSets[_frameIndex];
        Vk.vkCmdBindDescriptorSets(cmd, VkPipelineBindPoint.Graphics, _pipeline.PipelineLayout,
            0, 1, &descSet, 0, null);

        var bufferHandle = _vertexBuffer.Buffer;
        ulong offset = 0;
        Vk.vkCmdBindVertexBuffers(cmd, 0, 1, &bufferHandle, &offset);

        Vk.vkCmdBindIndexBuffer(cmd, _indexBuffer.Buffer, 0, 0);

        var angle = _totalTime;
        var model = Matrix4x4.CreateRotationY(angle) * Matrix4x4.CreateRotationX(angle * 0.5f);
        Vk.vkCmdPushConstants(cmd, _pipeline.PipelineLayout, VkShaderStageFlags.Vertex, 0, 64, &model);

        Vk.vkCmdDrawIndexed(cmd, _indexCount, 1, 0, 0, 0);

        Vk.vkCmdEndRendering(cmd);

        TransitionImageLayout(cmd, _swapchain.Images[imageIndex],
            VkImageLayout.ColorAttachmentOptimal, VkImageLayout.PresentSrcKHR,
            0x400, 0x100,
            0x8000, 0);

        Vk.vkEndCommandBuffer(cmd);

        var waitInfo = new VkSemaphoreSubmitInfo
        {
            sType = VkStructureType.SemaphoreSubmitInfo,
            semaphore = _frameResources.AcquireSemaphores[_frameIndex],
            stageMask = 0x400,
        };

        var cmdInfo = new VkCommandBufferSubmitInfo
        {
            sType = VkStructureType.CommandBufferSubmitInfo,
            commandBuffer = cmd,
        };

        var signalInfo = new VkSemaphoreSubmitInfo
        {
            sType = VkStructureType.SemaphoreSubmitInfo,
            semaphore = _frameResources.SubmitSemaphores[imageIndex],
            stageMask = 0x8000,
        };

        var submitInfo = new VkSubmitInfo2
        {
            sType = VkStructureType.SubmitInfo2,
            waitSemaphoreInfoCount = 1,
            pWaitSemaphoreInfos = &waitInfo,
            commandBufferInfoCount = 1,
            pCommandBufferInfos = &cmdInfo,
            signalSemaphoreInfoCount = 1,
            pSignalSemaphoreInfos = &signalInfo,
        };

        var submitResult = Vk.vkQueueSubmit2(_ctx.GraphicsQueue, 1, &submitInfo,
            _frameResources.FrameFences[_frameIndex]);

        if (submitResult != VkResult.Success)
            throw new InvalidOperationException($"vkQueueSubmit2 failed: {submitResult}");

        var presentSwapchain = _swapchain.Swapchain;
        var presentSemaphore = _frameResources.SubmitSemaphores[imageIndex];
        var presentInfo = new VkPresentInfoKHR
        {
            sType = VkStructureType.PresentInfoKHR,
            waitSemaphoreCount = 1,
            pWaitSemaphores = &presentSemaphore,
            swapchainCount = 1,
            pSwapchains = &presentSwapchain,
            pImageIndices = &imageIndex,
        };

        var presentResult = Vk.vkQueuePresentKHR(_ctx.GraphicsQueue, &presentInfo);

        if (presentResult == VkResult.ErrorOutOfDateKHR || presentResult == VkResult.SuboptimalKHR)
        {
            _swapchain.Recreate(_ctx.SurfaceExtent.Width == 0 ? 1280 : (int)_ctx.SurfaceExtent.Width,
                                _ctx.SurfaceExtent.Height == 0 ? 720 : (int)_ctx.SurfaceExtent.Height);
        }

        _frameIndex = (_frameIndex + 1) % VulkanFrameResources.MaxFramesInFlight;
    }

    private Matrix4x4 ComputeViewProjection()
    {
        var aspect = (float)_swapchain.Extent.Width / (float)_swapchain.Extent.Height;
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspect, 0.1f, 100f);
        var view = Matrix4x4.CreateLookAt(new Vector3(0, 0, -2), Vector3.Zero, Vector3.UnitY);
        return view * proj;
    }

    private static void TransitionImageLayout(VkCommandBuffer cmd, VkImage image,
        VkImageLayout oldLayout, VkImageLayout newLayout,
        ulong srcStage, ulong srcAccess,
        ulong dstStage, ulong dstAccess)
    {
        var barrier = new VkImageMemoryBarrier2
        {
            sType = VkStructureType.ImageMemoryBarrier2,
            srcStageMask = srcStage,
            srcAccessMask = srcAccess,
            dstStageMask = dstStage,
            dstAccessMask = dstAccess,
            oldLayout = oldLayout,
            newLayout = newLayout,
            image = image,
            subresourceRange = new VkImageSubresourceRange
            {
                AspectMask = VkImageAspectFlags.Color,
                LevelCount = 1,
                LayerCount = 1,
            },
        };

        var depInfo = new VkDependencyInfo
        {
            sType = VkStructureType.DependencyInfo,
            imageMemoryBarrierCount = 1,
            pImageMemoryBarriers = &barrier,
        };

        Vk.vkCmdPipelineBarrier2(cmd, &depInfo);
    }

    private static void TransitionImageLayoutDepth(VkCommandBuffer cmd, VkImage image,
        VkImageLayout oldLayout, VkImageLayout newLayout,
        ulong srcStage, ulong srcAccess,
        ulong dstStage, ulong dstAccess)
    {
        var barrier = new VkImageMemoryBarrier2
        {
            sType = VkStructureType.ImageMemoryBarrier2,
            srcStageMask = srcStage,
            srcAccessMask = srcAccess,
            dstStageMask = dstStage,
            dstAccessMask = dstAccess,
            oldLayout = oldLayout,
            newLayout = newLayout,
            image = image,
            subresourceRange = new VkImageSubresourceRange
            {
                AspectMask = VkImageAspectFlags.Depth,
                LevelCount = 1,
                LayerCount = 1,
            },
        };

        var depInfo = new VkDependencyInfo
        {
            sType = VkStructureType.DependencyInfo,
            imageMemoryBarrierCount = 1,
            pImageMemoryBarriers = &barrier,
        };

        Vk.vkCmdPipelineBarrier2(cmd, &depInfo);
    }

    private static byte[] LoadShader(string path)
    {
        if (!File.Exists(path))
        {
            var altPath = Path.Combine(AppContext.BaseDirectory, path);
            if (!File.Exists(altPath))
            {
                altPath = Path.Combine(AppContext.BaseDirectory, "Shaders", Path.GetFileName(path));
                if (!File.Exists(altPath))
                    throw new FileNotFoundException($"Shader file not found: {path}");
            }
            return File.ReadAllBytes(altPath);
        }
        return File.ReadAllBytes(path);
    }

    public void RequestScreenshot(string path)
    {
        _screenshotRequested = true;
        _screenshotPath = path;
    }

    public Task<byte[]> CaptureAsync(string outputPath)
    {
        _screenshotRequested = false;
        return Task.FromResult(Array.Empty<byte>());
    }

    public string? TryTakeScreenshotPath()
    {
        var path = _screenshotRequested ? _screenshotPath : null;
        _screenshotRequested = false;
        return path;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Vk.vkDeviceWaitIdle(_ctx.Device);

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _frameResources?.Dispose();
        _pipeline?.Dispose();
    }
}
