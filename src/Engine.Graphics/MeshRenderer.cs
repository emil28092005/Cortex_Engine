using System;
using System.Collections.Generic;
using System.Numerics;
using Flecs.NET.Core;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Engine.Core;
using Engine.Core.Components;

namespace Engine.Graphics;

/// <summary>
/// Renders indexed meshes attached to ECS entities.
/// Uses Silk.NET.Vulkan and reads Mesh + Transform components from the ECS world.
/// </summary>
public sealed unsafe class MeshRenderer : IDisposable
{
    private readonly VulkanContext _context;
    private readonly Swapchain _swapchain;
    private readonly VulkanPipeline _pipeline;
    private readonly ScreenshotCapture _screenshot;
    private readonly Dictionary<Entity, MeshBuffers> _buffers = new();
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers = null!;
    private Silk.NET.Vulkan.Semaphore[] _imageAvailableSemaphores = null!;
    private Silk.NET.Vulkan.Semaphore[] _renderFinishedSemaphores = null!;
    private Silk.NET.Vulkan.Fence[] _inFlightFences = null!;
    private int _currentFrame;

    private sealed class MeshBuffers : IDisposable
    {
        public VertexBuffer VertexBuffer;
        public IndexBuffer IndexBuffer;

        public MeshBuffers(VertexBuffer vertexBuffer, IndexBuffer indexBuffer)
        {
            VertexBuffer = vertexBuffer;
            IndexBuffer = indexBuffer;
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
        }
    }

    public MeshRenderer(VulkanContext context, Swapchain swapchain)
    {
        _context = context;
        _swapchain = swapchain;
        _screenshot = new ScreenshotCapture(context, swapchain);

        _pipeline = new VulkanPipeline(context, swapchain);
        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    private void CreateCommandPool()
    {
        var createInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _context.GraphicsFamilyIndex,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };

        CommandPool commandPool;
        var result = _context.Vk.CreateCommandPool(_context.Device, &createInfo, null, &commandPool);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateCommandPool failed: {result}");
        _commandPool = commandPool;
    }

    private void CreateCommandBuffers()
    {
        _commandBuffers = new CommandBuffer[2];
        for (var i = 0; i < _commandBuffers.Length; i++)
        {
            var allocInfo = new CommandBufferAllocateInfo
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = _commandPool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = 1
            };

            CommandBuffer cmd;
            var result = _context.Vk.AllocateCommandBuffers(_context.Device, &allocInfo, &cmd);
            if (result != Result.Success)
                throw new InvalidOperationException($"vkAllocateCommandBuffers failed: {result}");
            _commandBuffers[i] = cmd;
        }
    }

    private void CreateSyncObjects()
    {
        _imageAvailableSemaphores = new Silk.NET.Vulkan.Semaphore[2];
        _renderFinishedSemaphores = new Silk.NET.Vulkan.Semaphore[2];
        _inFlightFences = new Silk.NET.Vulkan.Fence[2];

        var semaphoreInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
        var fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (var i = 0; i < 2; i++)
        {
            Silk.NET.Vulkan.Semaphore imageAvailable, renderFinished;
            Silk.NET.Vulkan.Fence fence;
            _context.Vk.CreateSemaphore(_context.Device, &semaphoreInfo, null, &imageAvailable);
            _context.Vk.CreateSemaphore(_context.Device, &semaphoreInfo, null, &renderFinished);
            _context.Vk.CreateFence(_context.Device, &fenceInfo, null, &fence);
            _imageAvailableSemaphores[i] = imageAvailable;
            _renderFinishedSemaphores[i] = renderFinished;
            _inFlightFences[i] = fence;
        }
    }

    public void RequestScreenshot(string outputPath) => _screenshot.Request(outputPath);

    public bool IsScreenshotRequested => _screenshot.IsRequested;

    public void RenderWorld(World world)
    {
        var frame = _currentFrame % 2;

        var fence = _inFlightFences[frame];
        _context.Vk.WaitForFences(_context.Device, 1, &fence, true, ulong.MaxValue);
        _context.Vk.ResetFences(_context.Device, 1, &fence);

        uint imageIndex;
        var result = _context.KhrSwapchain!.AcquireNextImage(_context.Device, _swapchain.Handle, ulong.MaxValue, _imageAvailableSemaphores[frame], new Silk.NET.Vulkan.Fence(), &imageIndex);
        if (result == Result.ErrorOutOfDateKhr)
            return;

        var cmd = _commandBuffers[frame];
        _context.Vk.ResetCommandBuffer(cmd, CommandBufferResetFlags.None);

        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _context.Vk.BeginCommandBuffer(cmd, &beginInfo);

        var clearValues = new[]
        {
            new ClearValue(new ClearColorValue(0.0f, 0.0f, 0.0f, 1.0f)),
            new ClearValue { DepthStencil = new ClearDepthStencilValue(1.0f, 0) }
        };

        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _swapchain.RenderPass,
            Framebuffer = _swapchain.Framebuffers[imageIndex],
            RenderArea = new Rect2D(new Offset2D(0, 0), _swapchain.Extent),
            ClearValueCount = (uint)clearValues.Length
        };

        fixed (ClearValue* pClearValues = clearValues)
        {
            renderPassInfo.PClearValues = pClearValues;
        }

        _context.Vk.CmdBeginRenderPass(cmd, &renderPassInfo, SubpassContents.Inline);
        _context.Vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _pipeline.Handle);

        var viewport = new Viewport(0, 0, _swapchain.Extent.Width, _swapchain.Extent.Height, 0, 1);
        var scissor = new Rect2D(new Offset2D(0, 0), _swapchain.Extent);
        _context.Vk.CmdSetViewport(cmd, 0, 1, &viewport);
        _context.Vk.CmdSetScissor(cmd, 0, 1, &scissor);

        var camera = GetCamera(world);
        var view = camera.GetViewMatrix();
        var proj = camera.GetProjectionMatrix();
        var drawCmd = cmd;

        world.Each((Entity e, ref Mesh mesh, ref Transform transform) =>
        {
            if (!_buffers.TryGetValue(e, out var buffers))
            {
                buffers = CreateMeshBuffers(mesh);
                _buffers[e] = buffers;
            }

            var bytes = BuildMeshVertices(mesh, transform);
            buffers.VertexBuffer.Update(bytes);

            var model = transform.GetMatrix();
            var mvp = Matrix4x4.Multiply(Matrix4x4.Multiply(model, view), proj);
            var mvpT = Matrix4x4.Transpose(mvp);
            _context.Vk.CmdPushConstants(drawCmd, _pipeline.Layout, ShaderStageFlags.VertexBit, 0, 64, &mvpT);

            var vertexBuffer = buffers.VertexBuffer.Buffer;
            var offset = 0ul;
            _context.Vk.CmdBindVertexBuffers(drawCmd, 0, 1, &vertexBuffer, &offset);
            _context.Vk.CmdBindIndexBuffer(drawCmd, buffers.IndexBuffer.Buffer, 0, IndexType.Uint32);
            _context.Vk.CmdDrawIndexed(drawCmd, buffers.IndexBuffer.Count, 1, 0, 0, 0);
        });

        _context.Vk.CmdEndRenderPass(cmd);

        var swapchainImage = _swapchain.GetImage(imageIndex);
        _screenshot.RecordReadback(cmd, swapchainImage, _swapchain.Extent.Width, _swapchain.Extent.Height, _swapchain.SurfaceFormat);

        _context.Vk.EndCommandBuffer(cmd);

        var waitSemaphore = _imageAvailableSemaphores[frame];
        var signalSemaphore = _renderFinishedSemaphores[frame];
        var stageMask = PipelineStageFlags.ColorAttachmentOutputBit;
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = &stageMask,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore
        };

        _context.Vk.QueueSubmit(_context.GraphicsQueue, 1, &submitInfo, _inFlightFences[frame]);

        var swapchain = _swapchain.Handle;
        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex
        };

        _context.KhrSwapchain!.QueuePresent(_context.PresentQueue, &presentInfo);

        // If a screenshot was requested, wait for the GPU to finish the readback and save the file.
        if (_screenshot.IsRequested)
        {
            _context.Vk.WaitForFences(_context.Device, 1, &fence, true, ulong.MaxValue);
            _screenshot.Save(_swapchain.Extent.Width, _swapchain.Extent.Height, _swapchain.SurfaceFormat);
        }

        _currentFrame++;
    }

    private MeshBuffers CreateMeshBuffers(Mesh mesh)
    {
        var vertexBytes = new byte[mesh.Vertices.Length * 6 * sizeof(float)];
        fixed (byte* p = vertexBytes)
        {
            var dst = (float*)p;
            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                var v = mesh.Vertices[i];
                dst[i * 6 + 0] = v.Position.X;
                dst[i * 6 + 1] = v.Position.Y;
                dst[i * 6 + 2] = v.Position.Z;
                dst[i * 6 + 3] = v.Color.X;
                dst[i * 6 + 4] = v.Color.Y;
                dst[i * 6 + 5] = v.Color.Z;
            }
        }

        var indexBytes = new byte[mesh.Indices.Length * sizeof(uint)];
        fixed (byte* p = indexBytes)
        fixed (uint* src = mesh.Indices)
        {
            global::System.Buffer.MemoryCopy(src, p, indexBytes.Length, mesh.Indices.Length * sizeof(uint));
        }

        return new MeshBuffers(
            new VertexBuffer(_context, vertexBytes),
            new IndexBuffer(_context, indexBytes, (uint)mesh.Indices.Length));
    }

    private byte[] BuildMeshVertices(Mesh mesh, Transform transform)
    {
        var matrix = transform.GetMatrix();
        var bytes = new byte[mesh.Vertices.Length * 6 * sizeof(float)];
        fixed (byte* p = bytes)
        {
            var dst = (float*)p;
            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                var transformed = Vector3.Transform(mesh.Vertices[i].Position, matrix);
                dst[i * 6 + 0] = transformed.X;
                dst[i * 6 + 1] = transformed.Y;
                dst[i * 6 + 2] = transformed.Z;
                dst[i * 6 + 3] = mesh.Vertices[i].Color.X;
                dst[i * 6 + 4] = mesh.Vertices[i].Color.Y;
                dst[i * 6 + 5] = mesh.Vertices[i].Color.Z;
            }
        }
        return bytes;
    }

    private Camera GetCamera(World world)
    {
        var camera = new Camera(
            new Vector3(0.0f, 0.0f, -2.0f),
            Vector3.Zero,
            Vector3.UnitY,
            MathF.PI / 4.0f,
            (float)_swapchain.Extent.Width / _swapchain.Extent.Height,
            0.1f,
            100.0f);

        world.Each((Entity e, ref Camera cam) =>
        {
            camera = cam;
        });

        // Always keep the aspect ratio in sync with the swapchain.
        camera.AspectRatio = (float)_swapchain.Extent.Width / _swapchain.Extent.Height;
        return camera;
    }

    public void Dispose()
    {
        _context.Vk.DeviceWaitIdle(_context.Device);

        _screenshot.Dispose();

        foreach (var buffers in _buffers.Values)
            buffers.Dispose();
        _buffers.Clear();

        for (var i = 0; i < 2; i++)
        {
            _context.Vk.DestroySemaphore(_context.Device, _renderFinishedSemaphores[i], null);
            _context.Vk.DestroySemaphore(_context.Device, _imageAvailableSemaphores[i], null);
            _context.Vk.DestroyFence(_context.Device, _inFlightFences[i], null);
        }

        _context.Vk.DestroyCommandPool(_context.Device, _commandPool, null);

        _pipeline.Dispose();
    }
}
