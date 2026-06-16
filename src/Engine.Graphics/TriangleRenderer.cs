using System;
using System.Numerics;
using Flecs.NET.Core;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Engine.Core.Components;

namespace Engine.Graphics;

/// <summary>
/// Renders a colored triangle using a vertex buffer and a simple graphics pipeline.
/// Uses Silk.NET.Vulkan and reads entity transforms from the ECS world.
/// </summary>
public sealed unsafe class TriangleRenderer : IDisposable
{
    private readonly VulkanContext _context;
    private readonly Swapchain _swapchain;
    private readonly VulkanPipeline _pipeline;
    private readonly VertexBuffer _vertexBuffer;
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers = null!;
    private Silk.NET.Vulkan.Semaphore[] _imageAvailableSemaphores = null!;
    private Silk.NET.Vulkan.Semaphore[] _renderFinishedSemaphores = null!;
    private Silk.NET.Vulkan.Fence[] _inFlightFences = null!;
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
             0.0f, -0.5f,  1.0f, 0.0f, 0.0f,
             0.5f,  0.5f,  0.0f, 1.0f, 0.0f,
            -0.5f,  0.5f,  0.0f, 0.0f, 1.0f
        };

        var bytes = new byte[vertices.Length * sizeof(float)];
        fixed (byte* p = bytes)
        fixed (float* v = vertices)
        {
            global::System.Buffer.MemoryCopy(v, p, bytes.Length, vertices.Length * sizeof(float));
        }

        return new VertexBuffer(_context, bytes);
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

        var clearColor = new ClearValue(new ClearColorValue(0.0f, 0.0f, 0.0f, 1.0f));
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _swapchain.RenderPass,
            Framebuffer = _swapchain.Framebuffers[imageIndex],
            RenderArea = new Rect2D(new Offset2D(0, 0), _swapchain.Extent),
            ClearValueCount = 1,
            PClearValues = &clearColor
        };

        _context.Vk.CmdBeginRenderPass(cmd, &renderPassInfo, SubpassContents.Inline);
        _context.Vk.CmdBindPipeline(cmd, PipelineBindPoint.Graphics, _pipeline.Handle);

        var viewport = new Viewport(0, 0, _swapchain.Extent.Width, _swapchain.Extent.Height, 0, 1);
        var scissor = new Rect2D(new Offset2D(0, 0), _swapchain.Extent);
        _context.Vk.CmdSetViewport(cmd, 0, 1, &viewport);
        _context.Vk.CmdSetScissor(cmd, 0, 1, &scissor);

        var vertexBuffer = _vertexBuffer.Buffer;
        var offset = 0ul;
        _context.Vk.CmdBindVertexBuffers(cmd, 0, 1, &vertexBuffer, &offset);

        var drawCmd = cmd;
        world.Each((Entity e, ref Transform transform) =>
        {
            var bytes = BuildTriangleVertices(transform);
            _vertexBuffer.Update(bytes);
            _context.Vk.CmdDraw(drawCmd, 3, 1, 0, 0);
        });

        _context.Vk.CmdEndRenderPass(cmd);
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
        _currentFrame++;
    }

    private byte[] BuildTriangleVertices(Transform transform)
    {
        var matrix = transform.GetMatrix();
        var positions = new Vector3[]
        {
            new Vector3(0.0f, -0.5f, 0.0f),
            new Vector3(0.5f, 0.5f, 0.0f),
            new Vector3(-0.5f, 0.5f, 0.0f)
        };
        var colors = new[]
        {
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f)
        };

        var bytes = new byte[3 * 5 * sizeof(float)];
        fixed (byte* p = bytes)
        {
            var dst = (float*)p;
            for (var i = 0; i < 3; i++)
            {
                var transformed = Vector3.Transform(positions[i], matrix);
                dst[i * 5 + 0] = transformed.X;
                dst[i * 5 + 1] = transformed.Y;
                dst[i * 5 + 2] = colors[i].X;
                dst[i * 5 + 3] = colors[i].Y;
                dst[i * 5 + 4] = colors[i].Z;
            }
        }
        return bytes;
    }

    public void Dispose()
    {
        _context.Vk.DeviceWaitIdle(_context.Device);

        for (var i = 0; i < 2; i++)
        {
            _context.Vk.DestroySemaphore(_context.Device, _renderFinishedSemaphores[i], null);
            _context.Vk.DestroySemaphore(_context.Device, _imageAvailableSemaphores[i], null);
            _context.Vk.DestroyFence(_context.Device, _inFlightFences[i], null);
        }

        _context.Vk.DestroyCommandPool(_context.Device, _commandPool, null);

        _vertexBuffer.Dispose();
        _pipeline.Dispose();
    }
}
