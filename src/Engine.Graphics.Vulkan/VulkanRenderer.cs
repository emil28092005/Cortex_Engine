using System.Numerics;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics.Loaders;
using Flecs.NET.Core;
using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

public sealed unsafe class VulkanRenderer : IRenderer, Engine.Graphics.IScreenshotProvider
{
    private readonly VulkanContext _ctx;
    private readonly VulkanSwapchain _swapchain;
    private readonly VulkanPipeline _pipeline;
    private readonly VulkanFrameResources _frameResources;
    private readonly VulkanShadowMap _shadowMap;
    internal readonly VulkanImGui? _imGui;

    private readonly Dictionary<ulong, (VulkanVertexBuffer vb, VulkanIndexBuffer ib, uint indexCount)> _meshCache = new();

    private VkBuffer _screenshotBuffer;
    private VkDeviceMemory _screenshotMemory;
    private ulong _screenshotBufferSize;
    private bool _screenshotInitialized;

    private int _frameIndex;
    private bool _disposed;
    private bool _screenshotRequested;
    private string? _screenshotPath;
    private float _totalTime;

    public float ShadowBias { get; set; } = 0.0001f;
    public float ShadowSampleRadius { get; set; } = 0.015f;
    public float ShadowFarPlane { get; set; } = 60.0f;

    public bool IsRecording { get; set; }
    public byte[]? CapturedFrame { get; private set; }

    public bool IsScreenshotRequested => _screenshotRequested;
    public IScreenshotProvider ScreenshotProvider => this;

    internal VulkanRenderer(VulkanContext ctx, VulkanSwapchain swapchain)
    {
        _ctx = ctx;
        _swapchain = swapchain;

        var vertSpv = LoadShader("Shaders/triangle.vert.spv");
        var fragSpv = LoadShader("Shaders/triangle.frag.spv");

        _pipeline = new VulkanPipeline(ctx.Device, swapchain.Format, swapchain.DepthFormat, vertSpv, fragSpv);

        _frameResources = new VulkanFrameResources(ctx.Device, ctx.GraphicsQueueFamilyIndex,
            swapchain.ImageCount, ctx, _pipeline.DescriptorSetLayout);

        _shadowMap = new VulkanShadowMap(ctx.Device, ctx, _pipeline.DescriptorSetLayout);

        for (int i = 0; i < VulkanFrameResources.MaxFramesInFlight; i++)
            _frameResources.UpdateShadowDescriptor(i, _shadowMap.ShadowSampler, _shadowMap.CubeArrayColorView);

        _imGui = new VulkanImGui(ctx, _frameResources.CommandPool, swapchain.Format, swapchain.DepthFormat);
    }

    internal VulkanImGui? ImGuiLayer => _imGui;

    private void InitScreenshotBuffer()
    {
        if (_screenshotInitialized) return;
        _screenshotInitialized = true;

        var w = _swapchain.Extent.Width;
        var h = _swapchain.Extent.Height;
        _screenshotBufferSize = (ulong)(w * h * 4);

        var info = new VkBufferCreateInfo
        {
            sType = VkStructureType.BufferCreateInfo,
            size = _screenshotBufferSize,
            usage = VkBufferUsageFlags.TransferDst,
            sharingMode = VkSharingMode.Exclusive,
        };

        var buf = VkBuffer.Null;
        Vk.vkCreateBuffer(_ctx.Device, &info, 0, &buf);
        _screenshotBuffer = buf;

        var reqs = new VkMemoryRequirements();
        Vk.vkGetBufferMemoryRequirements(_ctx.Device, _screenshotBuffer, &reqs);
        var memType = _ctx.FindMemoryType(reqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

        var allocInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = reqs.size,
            memoryTypeIndex = memType,
        };

        var mem = VkDeviceMemory.Null;
        Vk.vkAllocateMemory(_ctx.Device, &allocInfo, 0, &mem);
        _screenshotMemory = mem;
        Vk.vkBindBufferMemory(_ctx.Device, _screenshotBuffer, _screenshotMemory, 0);
    }

    public byte[]? CaptureFrame(VkCommandBuffer cmd, uint imageIndex)
    {
        InitScreenshotBuffer();

        var w = _swapchain.Extent.Width;
        var h = _swapchain.Extent.Height;

        // Transition: ColorAttachmentOptimal → TransferSrcOptimal
        TransitionImageLayout(cmd, _swapchain.Images[imageIndex],
            VkImageLayout.ColorAttachmentOptimal, VkImageLayout.TransferSrcOptimal,
            0x400, 0x100, 0x10000, 0x1000);

        // Copy image to buffer
        var region = new VkBufferImageCopyRegion
        {
            bufferOffset = 0,
            bufferRowLength = w,
            bufferImageHeight = h,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1,
            },
            imageOffset = new VkOffset3D { X = 0, Y = 0, Z = 0 },
            imageExtent = new VkExtent3D { Width = w, Height = h, Depth = 1 },
        };

        Vk.vkCmdCopyImageToBuffer(cmd, _swapchain.Images[imageIndex], VkImageLayout.TransferSrcOptimal,
            _screenshotBuffer, 1, &region);

        // Transition back: TransferSrcOptimal → ColorAttachmentOptimal
        TransitionImageLayout(cmd, _swapchain.Images[imageIndex],
            VkImageLayout.TransferSrcOptimal, VkImageLayout.ColorAttachmentOptimal,
            0x10000, 0x1000, 0x400, 0x100);

        return null; // Data will be read next frame after GPU completes
    }

    private byte[]? ReadCapturedBuffer()
    {
        if (!_screenshotInitialized) return null;

        void* pData = null;
        Vk.vkMapMemory(_ctx.Device, _screenshotMemory, 0, _screenshotBufferSize, 0, &pData);

        var result = new byte[(int)_screenshotBufferSize];
        Marshal.Copy((nint)pData, result, 0, (int)_screenshotBufferSize);
        Vk.vkUnmapMemory(_ctx.Device, _screenshotMemory);

        // Vulkan data is BGRA, convert to RGBA
        for (int i = 0; i < result.Length; i += 4)
        {
            (result[i], result[i + 2]) = (result[i + 2], result[i]);
        }

        return result;
    }

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

        // Collect all point lights from ECS
        var lights = new List<(Vector3 pos, float intensity, Vector3 color, float range)>();
        world.Each((Entity e, ref Light l, ref Transform lt) =>
        {
            if (l.IsPoint)
                lights.Add((lt.Position, l.Intensity, l.Color, l.Range));
        });
        if (lights.Count == 0)
        {
            world.Each((Entity e, ref Light l) =>
            {
                if (l.IsPoint)
                    lights.Add((l.Position, l.Intensity, l.Color, l.Range));
            });
        }

        int numLights = Math.Min(lights.Count, 8);
        int numShadowLights = Math.Min(numLights, VulkanShadowMap.MaxShadowLights);

        // Pack SceneUBO: mat4 vp(64) + int numLights(4) + int numShadowLights(4) + vec2 pad(8) + LightData[8](256) + vec4 shadowParams[4](64) = 400B → pad to 448
        var uboData = stackalloc byte[448];
        var vpCopy = vp;
        System.Buffer.MemoryCopy(&vpCopy, uboData, 64, 64);
        *(int*)(uboData + 64) = numLights;
        *(int*)(uboData + 68) = numShadowLights;

        for (int i = 0; i < numLights; i++)
        {
            var lp = new Vector4(lights[i].pos, lights[i].intensity);
            var lc = new Vector4(lights[i].color, lights[i].range);
            var lightOffset = 80 + i * 32;
            System.Buffer.MemoryCopy(&lp, uboData + lightOffset, 16, 16);
            System.Buffer.MemoryCopy(&lc, uboData + lightOffset + 16, 16, 16);
        }

        // Shadow params per shadow light
        for (int i = 0; i < numShadowLights; i++)
        {
            var sp = new Vector4(ShadowBias, ShadowSampleRadius, ShadowFarPlane, 0);
            System.Buffer.MemoryCopy(&sp, uboData + 336 + i * 16, 16, 16);
        }

        // Compute light view-proj for each shadow light
        var lightViewProjs = new Matrix4x4[numShadowLights];
        for (int i = 0; i < numShadowLights; i++)
        {
            var lightView = Matrix4x4.CreateLookAt(lights[i].pos, Vector3.Zero, Vector3.UnitY);
            var lightProj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1.0f, 0.1f, ShadowFarPlane);
            lightViewProjs[i] = lightView * lightProj;
        }

        var drawCalls = new List<(VkBuffer vertexBuf, VkBuffer indexBuf, uint indexCount, Matrix4x4 model, bool castShadow)>();

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

            var name = e.Name();
            var castShadow = name != "Floor";
            drawCalls.Add((entry.vb.Buffer, entry.ib.Buffer, entry.indexCount, t.GetMatrix(), castShadow));
        });

        Render(vp, drawCalls, uboData, lights, lightViewProjs, numShadowLights);
    }

    private void Render(Matrix4x4 vp, List<(VkBuffer vertexBuf, VkBuffer indexBuf, uint indexCount, Matrix4x4 model, bool castShadow)> drawCalls, byte* uboData, List<(Vector3 pos, float intensity, Vector3 color, float range)> lights, Matrix4x4[] lightViewProjs, int numShadowLights)
    {
        _frameResources.WaitFrame(_frameIndex);

        // Read captured frame from previous render (GPU has finished by now)
        if (IsRecording)
        {
            CapturedFrame = ReadCapturedBuffer();
        }

        uint imageIndex;
        var acquireResult = Vk.vkAcquireNextImageKHR(_ctx.Device, _swapchain.Swapchain,
            ulong.MaxValue, _frameResources.AcquireSemaphores[_frameIndex], VkFence.Null, &imageIndex);

        if (acquireResult == VkResult.ErrorOutOfDateKHR || acquireResult == VkResult.SuboptimalKHR)
        {
            _swapchain.Recreate(_ctx.SurfaceExtent.Width == 0 ? 1280 : (int)_ctx.SurfaceExtent.Width,
                                _ctx.SurfaceExtent.Height == 0 ? 720 : (int)_ctx.SurfaceExtent.Height);
            Render(vp, drawCalls, uboData, lights, lightViewProjs, numShadowLights);
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

        // === SHADOW CUBEMAP ARRAY PASSES (numShadowLights × 6 faces) ===
        int totalLayers = numShadowLights * 6;
        // Always transition ALL layers (24) to avoid UNDEFINED layout errors for unused layers
        TransitionImageLayout(cmd, _shadowMap.ColorImage,
            VkImageLayout.Undefined, VkImageLayout.ColorAttachmentOptimal,
            0, 0, 0x400, 0x100, (uint)(VulkanShadowMap.MaxShadowLights * 6));
        TransitionImageLayoutDepth(cmd, _shadowMap.DepthImage,
            VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal,
            0, 0, 0x100, 0x200, (uint)(VulkanShadowMap.MaxShadowLights * 6));

        for (int lightIdx = 0; lightIdx < numShadowLights; lightIdx++)
        {
            var lightPosVec = lights[lightIdx].pos;

            for (int face = 0; face < 6; face++)
            {
                var (faceView, faceProj) = VulkanShadowMap.GetFaceViewProj(lightPosVec, face);
                var faceViewProj = faceView * faceProj;
                int faceIndex = lightIdx * 6 + face;

                var colorClear = new VkClearValue
                {
                    Color = new VkClearColorValue { Float0 = 1.0f, Float1 = 0, Float2 = 0, Float3 = 0 },
                };

                var shadowColorAttachment = new VkRenderingAttachmentInfo
                {
                    sType = VkStructureType.RenderingAttachmentInfo,
                    imageView = _shadowMap.FaceColorViews[faceIndex],
                    imageLayout = VkImageLayout.ColorAttachmentOptimal,
                    loadOp = VkAttachmentLoadOp.Clear,
                    storeOp = VkAttachmentStoreOp.Store,
                    clearValue = colorClear,
                };

                var shadowDepthClear = new VkClearValue
                {
                    DepthStencil = new VkClearDepthStencilValue { Depth = 1.0f, Stencil = 0 },
                };

                var shadowDepthAttachment = new VkRenderingAttachmentInfo
                {
                    sType = VkStructureType.RenderingAttachmentInfo,
                    imageView = _shadowMap.FaceDepthViews[faceIndex],
                    imageLayout = VkImageLayout.DepthStencilAttachmentOptimal,
                    loadOp = VkAttachmentLoadOp.Clear,
                    storeOp = VkAttachmentStoreOp.Store,
                    clearValue = shadowDepthClear,
                };

                var shadowRenderingInfo = new VkRenderingInfo
                {
                    sType = VkStructureType.RenderingInfo,
                    renderArea = new VkRect2D
                    {
                        Offset = new VkOffset2D { X = 0, Y = 0 },
                        Extent = new VkExtent2D { Width = VulkanShadowMap.ShadowMapSize, Height = VulkanShadowMap.ShadowMapSize },
                    },
                    layerCount = 1,
                    colorAttachmentCount = 1,
                    pColorAttachments = &shadowColorAttachment,
                    pDepthAttachment = &shadowDepthAttachment,
                };

                Vk.vkCmdBeginRendering(cmd, &shadowRenderingInfo);
                Vk.vkCmdBindPipeline(cmd, VkPipelineBindPoint.Graphics, _shadowMap.Pipeline);

                var shadowViewport = new VkViewport
                {
                    X = 0, Y = 0,
                    Width = VulkanShadowMap.ShadowMapSize,
                    Height = VulkanShadowMap.ShadowMapSize,
                    MinDepth = 0, MaxDepth = 1,
                };
                Vk.vkCmdSetViewport(cmd, 0, 1, &shadowViewport);

                var shadowScissor = new VkRect2D
                {
                    Offset = new VkOffset2D { X = 0, Y = 0 },
                    Extent = new VkExtent2D { Width = VulkanShadowMap.ShadowMapSize, Height = VulkanShadowMap.ShadowMapSize },
                };
                Vk.vkCmdSetScissor(cmd, 0, 1, &shadowScissor);
                Vk.vkCmdSetDepthBias(cmd, 1.75f, 0.0f, 2.5f);

                var lpVec4 = new Vector4(lightPosVec, lights[lightIdx].intensity);
                var sp = new Vector4(ShadowBias, ShadowSampleRadius, ShadowFarPlane, 0);

                foreach (var dc in drawCalls)
                {
                    if (!dc.castShadow) continue;

                    var vertexBuf = dc.vertexBuf;
                    ulong offset = 0;
                    Vk.vkCmdBindVertexBuffers(cmd, 0, 1, &vertexBuf, &offset);
                    Vk.vkCmdBindIndexBuffer(cmd, dc.indexBuf, 0, 1);

                    // Pack shadow push constants: model(64) + lightViewProj(64) + lightPos(16) + shadowParams(16) = 160
                    var pcData = stackalloc byte[160];
                    var modelCopy = dc.model;
                    System.Buffer.MemoryCopy(&modelCopy, pcData, 64, 64);
                    var lvpCopy = faceViewProj;
                    System.Buffer.MemoryCopy(&lvpCopy, pcData + 64, 64, 64);
                    System.Buffer.MemoryCopy(&lpVec4, pcData + 128, 16, 16);
                    System.Buffer.MemoryCopy(&sp, pcData + 144, 16, 16);

                    Vk.vkCmdPushConstants(cmd, _shadowMap.PipelineLayout, VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, 0, 160, pcData);

                    Vk.vkCmdDrawIndexed(cmd, dc.indexCount, 1, 0, 0, 0);
                }

                Vk.vkCmdEndRendering(cmd);
            }
        }

        // Transition shadow color array: ColorAttachmentOptimal → ShaderReadOnlyOptimal
        TransitionImageLayout(cmd, _shadowMap.ColorImage,
            VkImageLayout.ColorAttachmentOptimal, VkImageLayout.ShaderReadOnlyOptimal,
            0x400, 0x100, 0x8, 0x20, (uint)(VulkanShadowMap.MaxShadowLights * 6));

        // === MAIN PASS ===
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

            // Main pass push constants: model only (64B)
            var model = dc.model;
            Vk.vkCmdPushConstants(cmd, _pipeline.PipelineLayout, VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, 0, 64, &model);

            Vk.vkCmdDrawIndexed(cmd, dc.indexCount, 1, 0, 0, 0);
        }

        _imGui?.Render(cmd, _swapchain.Extent.Width, _swapchain.Extent.Height);

        Vk.vkCmdEndRendering(cmd);

        // Capture frame AFTER render pass ends, BEFORE present transition
        if (IsRecording)
        {
            CapturedFrame = CaptureFrame(cmd, imageIndex);
        }

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
        ulong dstStage, ulong dstAccess, uint layerCount = 1)
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
                LayerCount = layerCount,
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
        ulong dstStage, ulong dstAccess, uint layerCount = 1)
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
                LayerCount = layerCount,
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

        if (_screenshotBuffer.Handle != 0) Vk.vkDestroyBuffer(_ctx.Device, _screenshotBuffer, 0);
        if (_screenshotMemory.Handle != 0) Vk.vkFreeMemory(_ctx.Device, _screenshotMemory, 0);
        _shadowMap?.Dispose();
        _imGui?.Dispose();
        _frameResources?.Dispose();
        _pipeline?.Dispose();
    }
}
