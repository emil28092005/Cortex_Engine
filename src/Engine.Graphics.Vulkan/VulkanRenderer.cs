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
    internal readonly VulkanImGui? _imGui;

    private readonly Dictionary<ulong, (VulkanVertexBuffer vb, VulkanIndexBuffer ib, uint indexCount)> _meshCache = new();

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

        _imGui = new VulkanImGui(ctx, _frameResources.CommandPool, swapchain.Format, swapchain.DepthFormat);
    }

    internal VulkanImGui? ImGuiLayer => _imGui;

    public void BeginImGuiFrame()
    {
        _imGui?.NewFrame();
    }

    public void EndImGuiFrame()
    {
        if (_imGui == null) return;
        ImGuiNET.ImGui.Render();
    }

    public void RenderWorld(World world)
    {
        var vp = Matrix4x4.Identity;
        var found = false;
        world.Each((Entity e, ref Camera cam) =>
        {
            var aspect = (float)_swapchain.Extent.Width / (float)_swapchain.Extent.Height;
            cam.AspectRatio = aspect;
            var proj = cam.GetProjectionMatrix();
            proj.M22 *= -1;
            vp = cam.GetViewMatrix() * proj;
            found = true;
        });

        if (!found)
        {
            var aspect = (float)_swapchain.Extent.Width / (float)_swapchain.Extent.Height;
            var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, aspect, 0.1f, 100f);
            proj.M22 *= -1;
            vp = Matrix4x4.CreateLookAt(new Vector3(0, 0, -6), Vector3.Zero, Vector3.UnitY) * proj;
        }

        // Collect point light from ECS — prefer light attached to a moving entity
        var lightPos = new Vector4(0, 5, 0, 0);
        var lightColor = new Vector4(1, 1, 1, 0);
        world.Each((Entity e, ref Light l, ref Transform lt) =>
        {
            if (l.IsPoint && lightPos.W == 0)
            {
                lightPos = new Vector4(lt.Position, l.Intensity);
                lightColor = new Vector4(l.Color, l.Range);
            }
        });
        if (lightPos.W == 0)
        {
            world.Each((Entity e, ref Light l) =>
            {
                if (l.IsPoint && lightPos.W == 0)
                {
                    lightPos = new Vector4(l.Position, l.Intensity);
                    lightColor = new Vector4(l.Color, l.Range);
                }
            });
        }

        // Pack UBO: mat4 vp (64 bytes) + vec4 lightPos (16) + vec4 lightColor (16) = 96 bytes
        var uboData = stackalloc byte[128];
        var pFloat = (float*)uboData;
        pFloat[0] = vp.M11; pFloat[1] = vp.M12; pFloat[2] = vp.M13; pFloat[3] = vp.M14;
        pFloat[4] = vp.M21; pFloat[5] = vp.M22; pFloat[6] = vp.M23; pFloat[7] = vp.M24;
        pFloat[8] = vp.M31; pFloat[9] = vp.M32; pFloat[10] = vp.M33; pFloat[11] = vp.M34;
        pFloat[12] = vp.M41; pFloat[13] = vp.M42; pFloat[14] = vp.M43; pFloat[15] = vp.M44;
        pFloat[16] = lightPos.X; pFloat[17] = lightPos.Y; pFloat[18] = lightPos.Z; pFloat[19] = lightPos.W;
        pFloat[20] = lightColor.X; pFloat[21] = lightColor.Y; pFloat[22] = lightColor.Z; pFloat[23] = lightColor.W;

        var drawCalls = new List<(VkBuffer vertexBuf, VkBuffer indexBuf, uint indexCount, Matrix4x4 model)>();

        world.Each((Entity e, ref Transform t, ref Mesh m) =>
        {
            var eid = (ulong)e.Id;
            if (!_meshCache.TryGetValue(eid, out var entry))
            {
                var vb = new VulkanVertexBuffer(_ctx.Device, _ctx.PhysicalDevice,
                    _frameResources.CommandPool, _ctx.GraphicsQueue, _ctx, m.Vertices);
                var ib = new VulkanIndexBuffer(_ctx.Device, _frameResources.CommandPool,
                    _ctx.GraphicsQueue, _ctx, m.Indices);
                entry = (vb, ib, (uint)m.Indices.Length);
                _meshCache[eid] = entry;
            }

            drawCalls.Add((entry.vb.Buffer, entry.ib.Buffer, entry.indexCount, t.GetMatrix()));
        });

        Render(vp, drawCalls, uboData);
    }

    private void Render(Matrix4x4 vp, List<(VkBuffer vertexBuf, VkBuffer indexBuf, uint indexCount, Matrix4x4 model)> drawCalls, byte* uboData)
    {
        _frameResources.WaitFrame(_frameIndex);

        uint imageIndex;
        var acquireResult = Vk.vkAcquireNextImageKHR(_ctx.Device, _swapchain.Swapchain,
            ulong.MaxValue, _frameResources.AcquireSemaphores[_frameIndex], VkFence.Null, &imageIndex);

        if (acquireResult == VkResult.ErrorOutOfDateKHR || acquireResult == VkResult.SuboptimalKHR)
        {
            _swapchain.Recreate(_ctx.SurfaceExtent.Width == 0 ? 1280 : (int)_ctx.SurfaceExtent.Width,
                                _ctx.SurfaceExtent.Height == 0 ? 720 : (int)_ctx.SurfaceExtent.Height);
            Render(vp, drawCalls, uboData);
            return;
        }

        if (acquireResult != VkResult.Success)
            throw new InvalidOperationException($"vkAcquireNextImageKHR failed: {acquireResult}");

        _totalTime += 0.016f;

        _frameResources.UpdateUbo(_frameIndex, uboData, VulkanFrameResources.UboSize);

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

        foreach (var dc in drawCalls)
        {
            var vertexBuf = dc.vertexBuf;
            ulong offset = 0;
            Vk.vkCmdBindVertexBuffers(cmd, 0, 1, &vertexBuf, &offset);
            Vk.vkCmdBindIndexBuffer(cmd, dc.indexBuf, 0, 1);

            var model = dc.model;
            Vk.vkCmdPushConstants(cmd, _pipeline.PipelineLayout, VkShaderStageFlags.Vertex, 0, 64, &model);
            Vk.vkCmdDrawIndexed(cmd, dc.indexCount, 1, 0, 0, 0);
        }

        _imGui?.Render(cmd, _swapchain.Extent.Width, _swapchain.Extent.Height);

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

        foreach (var (vb, ib, _) in _meshCache.Values)
        {
            vb.Dispose();
            ib.Dispose();
        }
        _meshCache.Clear();

        _imGui?.Dispose();
        _frameResources?.Dispose();
        _pipeline?.Dispose();
    }
}
