using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Core;
using Engine.Core.Components;
using Engine.Graphics;
using Flecs.NET.Core;

namespace Engine.Graphics.Vulkan;

public sealed unsafe class VulkanRenderer : IRenderer, IScreenshotProvider
{
    public readonly VulkanContext _ctx;
    public readonly VulkanSwapchain _swapchain;
    private readonly VulkanPipeline _pipeline;
    public VulkanImGui? ImGuiLayer;

    private VkCommandPool _commandPool;
    private VkCommandBuffer[] _commandBuffers = Array.Empty<VkCommandBuffer>();
    private VkSemaphore[] _imageAvailableSemaphores = Array.Empty<VkSemaphore>();
    private VkSemaphore[] _renderFinishedSemaphores = Array.Empty<VkSemaphore>();
    private VkFence[] _inFlightFences = Array.Empty<VkFence>();

    private VkDescriptorPool _descriptorPool;
    private VkDescriptorSet[] _descriptorSets = Array.Empty<VkDescriptorSet>();
    private VulkanBuffer[] _uboBuffers = Array.Empty<VulkanBuffer>();

    private const int MaxFramesInFlight = 2;
    private int _currentFrame;
    private uint _imageIndex;
    private bool _resized;

    private readonly Dictionary<ulong, (VulkanBuffer vertex, VulkanBuffer index, uint indexCount)> _meshCache = new();

    private bool _screenshotRequested;
    private string _screenshotPath = "";
    private TaskCompletionSource<byte[]>? _screenshotTcs;

    private VulkanBuffer? _screenshotStaging;
    private uint _screenshotImageIndex;
    private bool _screenshotPending;

    public bool IsScreenshotRequested => _screenshotRequested;
    public IScreenshotProvider ScreenshotProvider => this;

    public VulkanRenderer(VulkanContext ctx, VulkanSwapchain swapchain, VulkanPipeline pipeline)
    {
        _ctx = ctx;
        _swapchain = swapchain;
        _pipeline = pipeline;

        CreateCommandPool();
        CreateSyncObjects();
        CreateDescriptorPool();
        CreateDescriptorSets();
        CreateCommandBuffers();
    }

    private unsafe void CreateCommandPool()
    {
        VkCommandPoolCreateInfo createInfo;
        createInfo.sType = VkStructureType.CommandPoolCreateInfo;
        createInfo.pNext = null;
        createInfo.flags = 0x00000002;
        createInfo.queueFamilyIndex = _ctx.GraphicsFamily;

        VkCommandPool pool;
        VkResult result = Vk.vkCreateCommandPool(_ctx.Device, &createInfo, null, &pool);
        Vk.CheckResult(result, "vkCreateCommandPool");
        _commandPool = pool;
    }

    private unsafe void CreateSyncObjects()
    {
        _imageAvailableSemaphores = new VkSemaphore[MaxFramesInFlight];
        _renderFinishedSemaphores = new VkSemaphore[MaxFramesInFlight];
        _inFlightFences = new VkFence[MaxFramesInFlight];

        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            VkSemaphoreCreateInfo semInfo;
            semInfo.sType = VkStructureType.SemaphoreCreateInfo;
            semInfo.pNext = null;
            semInfo.flags = 0;

            VkSemaphore sem1, sem2;
            Vk.CheckResult(Vk.vkCreateSemaphore(_ctx.Device, &semInfo, null, &sem1), "vkCreateSemaphore");
            Vk.CheckResult(Vk.vkCreateSemaphore(_ctx.Device, &semInfo, null, &sem2), "vkCreateSemaphore");
            _imageAvailableSemaphores[i] = sem1;
            _renderFinishedSemaphores[i] = sem2;

            VkFenceCreateInfo fenceInfo;
            fenceInfo.sType = VkStructureType.FenceCreateInfo;
            fenceInfo.pNext = null;
            fenceInfo.flags = VkFenceCreateFlags.Signaled;

            VkFence fence;
            Vk.CheckResult(Vk.vkCreateFence(_ctx.Device, &fenceInfo, null, &fence), "vkCreateFence");
            _inFlightFences[i] = fence;
        }
    }

    private unsafe void CreateDescriptorPool()
    {
        var poolSize = new VkDescriptorPoolSize
        {
            type = VkDescriptorType.UniformBuffer,
            descriptorCount = (uint)MaxFramesInFlight
        };

        VkDescriptorPoolCreateInfo createInfo;
        createInfo.sType = VkStructureType.DescriptorPoolCreateInfo;
        createInfo.pNext = null;
        createInfo.flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet;
        createInfo.maxSets = (uint)MaxFramesInFlight;
        createInfo.poolSizeCount = 1;
        createInfo.pPoolSizes = &poolSize;

        VkDescriptorPool pool;
        Vk.CheckResult(Vk.vkCreateDescriptorPool(_ctx.Device, &createInfo, null, &pool), "vkCreateDescriptorPool");
        _descriptorPool = pool;
    }

    private unsafe void CreateDescriptorSets()
    {
        _uboBuffers = new VulkanBuffer[MaxFramesInFlight];
        _descriptorSets = new VkDescriptorSet[MaxFramesInFlight];

        var layouts = new VkDescriptorSetLayout[MaxFramesInFlight];
        for (var i = 0; i < MaxFramesInFlight; i++)
            layouts[i] = _pipeline.DescriptorSetLayout;

        VkDescriptorSetAllocateInfo allocInfo;
        allocInfo.sType = VkStructureType.DescriptorSetAllocateInfo;
        allocInfo.pNext = null;
        allocInfo.descriptorPool = _descriptorPool;
        allocInfo.descriptorSetCount = (uint)MaxFramesInFlight;

        fixed (VkDescriptorSetLayout* pLayouts = layouts)
        {
            allocInfo.pSetLayouts = pLayouts;

            fixed (VkDescriptorSet* pSets = _descriptorSets)
            {
                Vk.CheckResult(Vk.vkAllocateDescriptorSets(_ctx.Device, &allocInfo, pSets), "vkAllocateDescriptorSets");
            }
        }

        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            _uboBuffers[i] = new VulkanBuffer(_ctx, (ulong)VulkanPipeline.FrameUboSize,
                VkBufferUsageFlags.UniformBuffer,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

            var bufferInfo = new VkDescriptorBufferInfo
            {
                buffer = _uboBuffers[i].Buffer,
                offset = 0,
                range = (ulong)VulkanPipeline.FrameUboSize
            };

            VkWriteDescriptorSet writeInfo;
            writeInfo.sType = VkStructureType.WriteDescriptorSet;
            writeInfo.pNext = null;
            writeInfo.dstSet = _descriptorSets[i];
            writeInfo.dstBinding = 0;
            writeInfo.dstArrayElement = 0;
            writeInfo.descriptorCount = 1;
            writeInfo.descriptorType = VkDescriptorType.UniformBuffer;
            writeInfo.pImageInfo = null;
            writeInfo.pBufferInfo = &bufferInfo;
            writeInfo.pTexelBufferView = null;

            Vk.vkUpdateDescriptorSets(_ctx.Device, 1, &writeInfo, 0, null);
        }
    }

    private unsafe void CreateCommandBuffers()
    {
        _commandBuffers = new VkCommandBuffer[MaxFramesInFlight];

        VkCommandBufferAllocateInfo allocInfo;
        allocInfo.sType = VkStructureType.CommandBufferAllocateInfo;
        allocInfo.pNext = null;
        allocInfo.commandPool = _commandPool;
        allocInfo.level = VkCommandBufferLevel.Primary;
        allocInfo.commandBufferCount = (uint)MaxFramesInFlight;

        fixed (VkCommandBuffer* pCmds = _commandBuffers)
        {
            Vk.CheckResult(Vk.vkAllocateCommandBuffers(_ctx.Device, &allocInfo, pCmds), "vkAllocateCommandBuffers");
        }
    }

    public unsafe void RenderWorld(World world)
    {
        VkFence fence = _inFlightFences[_currentFrame];
        Vk.CheckResult(Vk.vkWaitForFences(_ctx.Device, 1, &fence, 1, ulong.MaxValue), "vkWaitForFences");

        if (_screenshotPending && _screenshotStaging != null)
        {
            FinishScreenshot();
        }

        uint imageIndex = 0;
        VkSemaphore imgAvailSem = _imageAvailableSemaphores[_currentFrame];
        var acquireResult = Vk.vkAcquireNextImageKHR(_ctx.Device, _swapchain.Swapchain, ulong.MaxValue,
            imgAvailSem, default, &imageIndex);

        if (acquireResult == VkResult.ErrorOutOfDateKHR || _resized)
        {
            _resized = false;
            _swapchain.Recreate(_swapchain.Extent.width, _swapchain.Extent.height);
            RecreateCommandBuffers();
            return;
        }
        Vk.CheckResult(acquireResult, "vkAcquireNextImageKHR");

        _imageIndex = imageIndex;

        VkFence fenceReset = _inFlightFences[_currentFrame];
        Vk.CheckResult(Vk.vkResetFences(_ctx.Device, 1, &fenceReset), "vkResetFences");

        var cmd = _commandBuffers[_currentFrame];
        Vk.CheckResult(Vk.vkResetCommandBuffer(cmd, 0), "vkResetCommandBuffer");

        UpdateFrameUbo(world);

        RecordCommandBuffer(cmd, imageIndex, world);

        VkSemaphore renderDoneSem = _renderFinishedSemaphores[_currentFrame];
        VkSemaphore imgAvailSem2 = _imageAvailableSemaphores[_currentFrame];
        VkFence submitFence = _inFlightFences[_currentFrame];

        VkSubmitInfo submitInfo;
        submitInfo.sType = VkStructureType.SubmitInfo;
        submitInfo.pNext = null;
        submitInfo.waitSemaphoreCount = 1;
        submitInfo.pWaitSemaphores = &imgAvailSem2;

        var waitStage = (ulong)VkPipelineStageFlags.ColorAttachmentOutput;
        submitInfo.pWaitDstStageMask = &waitStage;
        submitInfo.commandBufferCount = 1;
        submitInfo.pCommandBuffers = &cmd;
        submitInfo.signalSemaphoreCount = 1;
        submitInfo.pSignalSemaphores = &renderDoneSem;

        Vk.CheckResult(Vk.vkQueueSubmit(_ctx.GraphicsQueue, 1, &submitInfo, submitFence), "vkQueueSubmit");

        VkSemaphore renderDoneSem2 = _renderFinishedSemaphores[_currentFrame];
        VkSwapchainKHR swapchain = _swapchain.Swapchain;

        VkPresentInfoKHR presentInfo;
        presentInfo.sType = VkStructureType.PresentInfoKHR;
        presentInfo.pNext = null;
        presentInfo.waitSemaphoreCount = 1;
        presentInfo.pWaitSemaphores = &renderDoneSem2;
        presentInfo.swapchainCount = 1;
        presentInfo.pSwapchains = &swapchain;
        presentInfo.pImageIndices = &imageIndex;
        presentInfo.pResults = null;

        var presentResult = Vk.vkQueuePresentKHR(_ctx.GraphicsQueue, &presentInfo);
        if (presentResult == VkResult.ErrorOutOfDateKHR || presentResult == VkResult.SuboptimalKHR || _resized)
        {
            _resized = false;
            _swapchain.Recreate(_swapchain.Extent.width, _swapchain.Extent.height);
            RecreateCommandBuffers();
        }
        else
        {
            Vk.CheckResult(presentResult, "vkQueuePresentKHR");
        }

        _currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
    }

    private unsafe void FinishScreenshot()
    {
        try
        {
            var width = _swapchain.Extent.width;
            var height = _swapchain.Extent.height;

            var dir = Path.GetDirectoryName(_screenshotPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var src = (byte*)_screenshotStaging!.MappedData;
            if (src == null) throw new InvalidOperationException("Screenshot staging buffer not mapped");

            var srcFormat = _swapchain.ImageFormat;
            var rowSize = width * 4;
            var pixelDataSize = rowSize * height;
            var fileSize = 54u + (uint)pixelDataSize;

            using (var fs = new FileStream(_screenshotPath, FileMode.Create))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)'B');
                bw.Write((byte)'M');
                bw.Write(fileSize);
                bw.Write(0u);
                bw.Write(54u);
                bw.Write(40u);
                bw.Write((uint)width);
                bw.Write((uint)height);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write((uint)pixelDataSize);
                bw.Write(0u);
                bw.Write(0u);
                bw.Write(0u);
                bw.Write(0u);

                var rowBuf = new byte[rowSize];
                for (var y = 0; y < height; y++)
                {
                    var srcRow = (height - 1 - y) * width * 4;
                    if (srcFormat == VkFormat.B8G8R8A8Srgb || srcFormat == VkFormat.B8G8R8A8Unorm)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            rowBuf[x * 4 + 0] = src[srcRow + x * 4 + 0];
                            rowBuf[x * 4 + 1] = src[srcRow + x * 4 + 1];
                            rowBuf[x * 4 + 2] = src[srcRow + x * 4 + 2];
                            rowBuf[x * 4 + 3] = src[srcRow + x * 4 + 3];
                        }
                    }
                    else
                    {
                        for (var x = 0; x < width; x++)
                        {
                            rowBuf[x * 4 + 0] = src[srcRow + x * 4 + 2];
                            rowBuf[x * 4 + 1] = src[srcRow + x * 4 + 1];
                            rowBuf[x * 4 + 2] = src[srcRow + x * 4 + 0];
                            rowBuf[x * 4 + 3] = src[srcRow + x * 4 + 3];
                        }
                    }
                    bw.Write(rowBuf, 0, rowSize);
                }
            }

            _screenshotStaging.Dispose();
            _screenshotStaging = null;
            _screenshotPending = false;

            Console.WriteLine($"Screenshot saved: {_screenshotPath} ({fileSize} bytes, {width}x{height})");

            _screenshotTcs?.TrySetResult(Array.Empty<byte>());
            _screenshotTcs = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Screenshot capture failed: {ex}");
            _screenshotStaging?.Dispose();
            _screenshotStaging = null;
            _screenshotPending = false;
            _screenshotTcs?.TrySetException(ex);
            _screenshotTcs = null;
        }
    }

    private unsafe void UpdateFrameUbo(World world)
    {
        Vector3 camPos = Vector3.Zero;
        var view = Matrix4x4.Identity;
        var proj = Matrix4x4.Identity;

        world.Each((Entity e, ref Camera cam) =>
        {
            camPos = cam.Position;
            view = cam.GetViewMatrix();
            proj = cam.GetProjectionMatrix();
        });

        var lights = new List<(Light light, Transform transform)>();
        world.Each((Entity e, ref Light light, ref Transform transform) =>
        {
            lights.Add((light, transform));
        });

        var uboData = new byte[VulkanPipeline.FrameUboSize];
        fixed (byte* pUbo = uboData)
        {
            var p = (float*)pUbo;

            p[0] = camPos.X; p[1] = camPos.Y; p[2] = camPos.Z;
            p[3] = (uint)Math.Min(lights.Count, 16);

            p[4] = 0.15f; p[5] = 0.15f; p[6] = 0.2f; p[7] = 0f;

            for (var i = 0; i < Math.Min(lights.Count, 16); i++)
            {
                var (light, _) = lights[i];
                var baseIdx = 8 + i * 8;

                if (light.IsDirectional)
                {
                    p[baseIdx + 0] = light.Direction.X;
                    p[baseIdx + 1] = light.Direction.Y;
                    p[baseIdx + 2] = light.Direction.Z;
                    p[baseIdx + 3] = -light.Intensity;

                    p[baseIdx + 4] = light.Color.X;
                    p[baseIdx + 5] = light.Color.Y;
                    p[baseIdx + 6] = light.Color.Z;
                    p[baseIdx + 7] = 0f;
                }
                else
                {
                    p[baseIdx + 0] = light.Position.X;
                    p[baseIdx + 1] = light.Position.Y;
                    p[baseIdx + 2] = light.Position.Z;
                    p[baseIdx + 3] = light.Intensity;

                    p[baseIdx + 4] = light.Color.X;
                    p[baseIdx + 5] = light.Color.Y;
                    p[baseIdx + 6] = light.Color.Z;
                    p[baseIdx + 7] = light.Range;
                }
            }

            _uboBuffers[_currentFrame].Write(pUbo, (ulong)VulkanPipeline.FrameUboSize);
        }
    }

    private unsafe void RecordCommandBuffer(VkCommandBuffer cmd, uint imageIndex, World world)
    {
        VkCommandBufferBeginInfo beginInfo;
        beginInfo.sType = VkStructureType.CommandBufferBeginInfo;
        beginInfo.pNext = null;
        beginInfo.flags = 0;
        beginInfo.pInheritanceInfo = null;

        Vk.CheckResult(Vk.vkBeginCommandBuffer(cmd, &beginInfo), "vkBeginCommandBuffer");

        VkRenderPassBeginInfo rpBegin;
        rpBegin.sType = VkStructureType.RenderPassBeginInfo;
        rpBegin.pNext = null;
        rpBegin.renderPass = _swapchain.RenderPass;
        rpBegin.framebuffer = _swapchain.Framebuffers[imageIndex];
        rpBegin.renderArea = new VkRect2D
        {
            offset = new VkOffset2D { x = 0, y = 0 },
            extent = _swapchain.Extent
        };

        var clearValues = new VkClearValue[2];
        clearValues[0] = new VkClearValue { color = new VkClearColorValue { r = 0.05f, g = 0.05f, b = 0.08f, a = 1.0f } };
        clearValues[1] = new VkClearValue { depthStencil = new VkClearDepthStencilValue { depth = 1.0f, stencil = 0 } };

        fixed (VkClearValue* pClear = clearValues)
        {
            rpBegin.clearValueCount = 2;
            rpBegin.pClearValues = pClear;

            Vk.vkCmdBeginRenderPass(cmd, &rpBegin, VkSubpassContents.Inline);
        }

        Vk.vkCmdBindPipeline(cmd, 0, _pipeline.Pipeline);

        var viewport = new VkViewport
        {
            x = 0, y = 0,
            width = _swapchain.Extent.width,
            height = _swapchain.Extent.height,
            minDepth = 0, maxDepth = 1
        };
        Vk.vkCmdSetViewport(cmd, 0, 1, &viewport);

        var scissor = new VkRect2D
        {
            offset = new VkOffset2D { x = 0, y = 0 },
            extent = _swapchain.Extent
        };
        Vk.vkCmdSetScissor(cmd, 0, 1, &scissor);

        var ds = _descriptorSets[_currentFrame];
        Vk.vkCmdBindDescriptorSets(cmd, 0, _pipeline.PipelineLayout, 0, 1, &ds, 0, null);

        world.Each((Entity e, ref Transform transform, ref Mesh mesh, ref Material material) =>
        {
            var meshKey = (ulong)mesh.GetHashCode();
            if (!_meshCache.TryGetValue(meshKey, out var meshBuffers))
            {
                if (mesh.Vertices.Length == 0 || mesh.Indices.Length == 0) return;

                var vertexBuffer = VulkanBuffer.CreateDeviceLocal(_ctx, _commandPool, mesh.Vertices, VkBufferUsageFlags.VertexBuffer);
                var indexBuffer = VulkanBuffer.CreateDeviceLocal(_ctx, _commandPool, mesh.Indices, VkBufferUsageFlags.IndexBuffer);

                meshBuffers = (vertexBuffer, indexBuffer, (uint)mesh.Indices.Length);
                _meshCache[meshKey] = meshBuffers;
            }

            var model = transform.GetMatrix();
            var view = Matrix4x4.Identity;
            var proj = Matrix4x4.Identity;

            world.Each((Entity camE, ref Camera cam) =>
            {
                view = cam.GetViewMatrix();
                proj = cam.GetProjectionMatrix();
            });

            var mvp = proj * view * model;

            var pushData = new byte[VulkanPipeline.PushConstantSize];
            fixed (byte* pPush = pushData)
            {
                var p = (float*)pPush;

                p[0] = mvp.M11; p[1] = mvp.M12; p[2] = mvp.M13; p[3] = mvp.M14;
                p[4] = mvp.M21; p[5] = mvp.M22; p[6] = mvp.M23; p[7] = mvp.M24;
                p[8] = mvp.M31; p[9] = mvp.M32; p[10] = mvp.M33; p[11] = mvp.M34;
                p[12] = mvp.M41; p[13] = mvp.M42; p[14] = mvp.M43; p[15] = mvp.M44;

                p[16] = model.M11; p[17] = model.M12; p[18] = model.M13; p[19] = model.M14;
                p[20] = model.M21; p[21] = model.M22; p[22] = model.M23; p[23] = model.M24;
                p[24] = model.M31; p[25] = model.M32; p[26] = model.M33; p[27] = model.M34;
                p[28] = model.M41; p[29] = model.M42; p[30] = model.M43; p[31] = model.M44;

                p[32] = material.Albedo.X;
                p[33] = material.Albedo.Y;
                p[34] = material.Albedo.Z;
                p[35] = material.Roughness;

                var buf = meshBuffers.vertex.Buffer;
                var offset = 0ul;
                Vk.vkCmdBindVertexBuffers(cmd, 0, 1, &buf, &offset);
                Vk.vkCmdBindIndexBuffer(cmd, meshBuffers.index.Buffer, 0, VkIndexType.Uint32);

                Vk.vkCmdPushConstants(cmd, _pipeline.PipelineLayout,
                    VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, 0,
                    (uint)VulkanPipeline.PushConstantSize, pPush);

                Vk.vkCmdDrawIndexed(cmd, meshBuffers.indexCount, 1, 0, 0, 0);
            }
        });

        if (ImGuiLayer != null)
        {
            ImGuiLayer.Render(cmd);
        }

        Vk.vkCmdEndRenderPass(cmd);

        if (_screenshotRequested)
        {
            var width = _swapchain.Extent.width;
            var height = _swapchain.Extent.height;
            var bufferSize = (ulong)(width * height * 4);

            _screenshotStaging = new VulkanBuffer(_ctx, bufferSize,
                VkBufferUsageFlags.TransferDst,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
            _screenshotImageIndex = imageIndex;
            _screenshotPending = true;
            _screenshotRequested = false;

            var barrier = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier,
                pNext = null,
                srcAccessMask = VkAccessFlags.ColorAttachmentWrite,
                dstAccessMask = VkAccessFlags.TransferRead,
                oldLayout = VkImageLayout.PresentSrcKHR,
                newLayout = VkImageLayout.TransferSrcOptimal,
                srcQueueFamilyIndex = ~0u,
                dstQueueFamilyIndex = ~0u,
                image = _swapchain.SwapchainImages[imageIndex],
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.Color,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            Vk.vkCmdPipelineBarrier(cmd,
                VkPipelineStageFlags.ColorAttachmentOutput,
                VkPipelineStageFlags.Transfer,
                0, 0, null, 0, null, 1, &barrier);

            var region = new VkBufferImageCopy
            {
                bufferOffset = 0,
                bufferRowLength = (uint)width,
                bufferImageHeight = (uint)height,
                imageSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.Color,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                imageOffset = new VkOffset3D { x = 0, y = 0, z = 0 },
                imageExtent = new VkExtent3D { width = width, height = height, depth = 1 }
            };

            var stagingBuf = _screenshotStaging.Buffer;
            Vk.vkCmdCopyImageToBuffer(cmd,
                _swapchain.SwapchainImages[imageIndex],
                (int)VkImageLayout.TransferSrcOptimal,
                stagingBuf, 1, &region);

            var barrier2 = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier,
                pNext = null,
                srcAccessMask = VkAccessFlags.TransferRead,
                dstAccessMask = VkAccessFlags.MemoryRead,
                oldLayout = VkImageLayout.TransferSrcOptimal,
                newLayout = VkImageLayout.PresentSrcKHR,
                srcQueueFamilyIndex = ~0u,
                dstQueueFamilyIndex = ~0u,
                image = _swapchain.SwapchainImages[imageIndex],
                subresourceRange = new VkImageSubresourceRange
                {
                    aspectMask = VkImageAspectFlags.Color,
                    baseMipLevel = 0,
                    levelCount = 1,
                    baseArrayLayer = 0,
                    layerCount = 1
                }
            };

            Vk.vkCmdPipelineBarrier(cmd,
                VkPipelineStageFlags.Transfer,
                VkPipelineStageFlags.BottomOfPipe,
                0, 0, null, 0, null, 1, &barrier2);
        }

        Vk.CheckResult(Vk.vkEndCommandBuffer(cmd), "vkEndCommandBuffer");
    }

    private unsafe void RecreateCommandBuffers()
    {
        fixed (VkCommandBuffer* pCmds = _commandBuffers)
        {
            Vk.vkFreeCommandBuffers(_ctx.Device, _commandPool, (uint)_commandBuffers.Length, pCmds);
        }

        VkCommandBufferAllocateInfo allocInfo;
        allocInfo.sType = VkStructureType.CommandBufferAllocateInfo;
        allocInfo.pNext = null;
        allocInfo.commandPool = _commandPool;
        allocInfo.level = VkCommandBufferLevel.Primary;
        allocInfo.commandBufferCount = (uint)_commandBuffers.Length;

        fixed (VkCommandBuffer* pCmds = _commandBuffers)
        {
            Vk.CheckResult(Vk.vkAllocateCommandBuffers(_ctx.Device, &allocInfo, pCmds), "vkAllocateCommandBuffers (recreate)");
        }
    }

    public void RequestScreenshot(string outputPath)
    {
        _screenshotPath = outputPath;
        _screenshotRequested = true;
    }

    public Task<byte[]> CaptureAsync(string outputPath)
    {
        _screenshotPath = outputPath;
        _screenshotTcs = new TaskCompletionSource<byte[]>();
        _screenshotRequested = true;
        return _screenshotTcs.Task;
    }
    public void OnResize() => _resized = true;

    public unsafe void Dispose()
    {
        Vk.vkQueueWaitIdle(_ctx.GraphicsQueue);

        foreach (var mesh in _meshCache.Values)
        {
            mesh.vertex.Dispose();
            mesh.index.Dispose();
        }
        _meshCache.Clear();

        foreach (var ubo in _uboBuffers)
            ubo?.Dispose();

        if (_descriptorPool.Value != 0)
            Vk.vkDestroyDescriptorPool(_ctx.Device, _descriptorPool, null);

        fixed (VkCommandBuffer* pCmds = _commandBuffers)
        {
            if (_commandPool.Value != 0 && _commandBuffers.Length > 0)
                Vk.vkFreeCommandBuffers(_ctx.Device, _commandPool, (uint)_commandBuffers.Length, pCmds);
        }
        if (_commandPool.Value != 0) Vk.vkDestroyCommandPool(_ctx.Device, _commandPool, null);

        for (var i = 0; i < MaxFramesInFlight; i++)
        {
            if (_imageAvailableSemaphores[i].Value != 0) Vk.vkDestroySemaphore(_ctx.Device, _imageAvailableSemaphores[i], null);
            if (_renderFinishedSemaphores[i].Value != 0) Vk.vkDestroySemaphore(_ctx.Device, _renderFinishedSemaphores[i], null);
            if (_inFlightFences[i].Value != 0) Vk.vkDestroyFence(_ctx.Device, _inFlightFences[i], null);
        }
    }
}
