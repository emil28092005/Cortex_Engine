using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Flecs.NET.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
    private readonly UniformBuffer _frameConstantsBuffer;
    private DescriptorPool _frameDescriptorPool;
    private DescriptorSet _frameDescriptorSet;
    private DescriptorPool _textureDescriptorPool;
    private readonly Dictionary<string, Texture> _textures = new();
    private readonly Dictionary<Texture, DescriptorSet> _textureDescriptorSets = new();
    private Texture? _defaultTexture;
    private readonly Dictionary<Entity, MeshBuffers> _buffers = new();
    private CommandPool _commandPool;
    private CommandBuffer[] _commandBuffers = null!;
    private Silk.NET.Vulkan.Semaphore[] _imageAvailableSemaphores = null!;
    private Silk.NET.Vulkan.Semaphore[] _renderFinishedSemaphores = null!;
    private Silk.NET.Vulkan.Fence[] _inFlightFences = null!;
    private int _currentFrame;

    [StructLayout(LayoutKind.Sequential, Size = 96)]
    private struct PushConstants
    {
        public Matrix4x4 Mvp;
        public Vector3 MaterialAlbedo;
        public float MaterialRoughness;
        public float MaterialMetallic;
        public uint UseTexture;
        public uint TextureIndex;
        public uint Pad0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct GpuLight
    {
        public Vector3 Direction;
        public float Intensity;
        public Vector3 Color;
        public float Padding;
    }

    [StructLayout(LayoutKind.Sequential, Size = 224)]
    private struct FrameConstants
    {
        public Vector3 CameraPosition;
        public uint LightCount;
        public Vector3 AmbientColor;
        public float AmbientPadding;
        public GpuLight Light0;
        public GpuLight Light1;
        public GpuLight Light2;
        public GpuLight Light3;
    }

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
        _frameConstantsBuffer = new UniformBuffer(context, (ulong)sizeof(FrameConstants));
        CreateFrameDescriptorPool();
        CreateFrameDescriptorSet();
        CreateTextureDescriptorPool();
        CreateDefaultTexture();
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

    private void CreateFrameDescriptorPool()
    {
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = 1
        };

        var createInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = 1,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize
        };

        DescriptorPool descriptorPool;
        var result = _context.Vk.CreateDescriptorPool(_context.Device, &createInfo, null, &descriptorPool);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateDescriptorPool failed: {result}");
        _frameDescriptorPool = descriptorPool;
    }

    private void CreateFrameDescriptorSet()
    {
        var layout = _pipeline.FrameDescriptorSetLayout;
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _frameDescriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };

        DescriptorSet descriptorSet;
        var result = _context.Vk.AllocateDescriptorSets(_context.Device, &allocInfo, &descriptorSet);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkAllocateDescriptorSets failed: {result}");
        _frameDescriptorSet = descriptorSet;

        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = _frameConstantsBuffer.Buffer,
            Offset = 0,
            Range = (ulong)sizeof(FrameConstants)
        };

        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = _frameDescriptorSet,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo
        };

        _context.Vk.UpdateDescriptorSets(_context.Device, 1, &write, 0, null);
    }

    private void CreateTextureDescriptorPool()
    {
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.CombinedImageSampler,
            DescriptorCount = 16
        };

        var createInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            MaxSets = 16,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize
        };

        DescriptorPool descriptorPool;
        var result = _context.Vk.CreateDescriptorPool(_context.Device, &createInfo, null, &descriptorPool);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateDescriptorPool (texture) failed: {result}");
        _textureDescriptorPool = descriptorPool;
    }

    private void CreateDefaultTexture()
    {
        var whitePixel = new byte[] { 255, 255, 255, 255 };
        _defaultTexture = CreateTextureFromBytes("__default__", whitePixel, 1, 1);
    }

    private Texture CreateTextureFromBytes(string key, byte[] rgbaPixels, uint width, uint height)
    {
        var path = $"/tmp/cortex_texture_{key}.png";
        System.IO.File.WriteAllBytes(path, EncodePng(rgbaPixels, width, height));
        var texture = new Texture(_context, path);
        try
        {
            System.IO.File.Delete(path);
        }
        catch
        {
            // Ignore cleanup failure.
        }
        return texture;
    }

    private static byte[] EncodePng(byte[] rgbaPixels, uint width, uint height)
    {
        using var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(rgbaPixels, (int)width, (int)height);
        using var stream = new System.IO.MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
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
        var frameDescriptorSet = _frameDescriptorSet;
        _context.Vk.CmdBindDescriptorSets(cmd, PipelineBindPoint.Graphics, _pipeline.Layout, 0, 1, &frameDescriptorSet, 0, null);

        var viewport = new Viewport(0, 0, _swapchain.Extent.Width, _swapchain.Extent.Height, 0, 1);
        var scissor = new Rect2D(new Offset2D(0, 0), _swapchain.Extent);
        _context.Vk.CmdSetViewport(cmd, 0, 1, &viewport);
        _context.Vk.CmdSetScissor(cmd, 0, 1, &scissor);

        var camera = GetCamera(world);
        var view = camera.GetViewMatrix();
        var proj = camera.GetProjectionMatrix();
        var drawCmd = cmd;

        var frameConstants = BuildFrameConstants(world, camera);
        var frameConstantsBytes = new byte[sizeof(FrameConstants)];
        fixed (byte* p = frameConstantsBytes)
        {
            *(FrameConstants*)p = frameConstants;
        }
        _frameConstantsBuffer.Update(frameConstantsBytes);

        world.Each((Entity e, ref Mesh mesh, ref Transform transform) =>
        {
            if (!_buffers.TryGetValue(e, out var buffers))
            {
                buffers = CreateMeshBuffers(mesh);
                _buffers[e] = buffers;
            }

            var material = e.Has<Material>() ? e.Get<Material>() : Material.Default;
            var bytes = BuildMeshVertices(mesh, transform, material);
            buffers.VertexBuffer.Update(bytes);

            var mvp = Matrix4x4.Transpose(Matrix4x4.Multiply(view, proj));
            var texture = GetTexture(material);
            var textureDescriptorSet = GetTextureDescriptorSet(texture);
            var textureSet = textureDescriptorSet;
            _context.Vk.CmdBindDescriptorSets(drawCmd, PipelineBindPoint.Graphics, _pipeline.Layout, 1, 1, &textureSet, 0, null);

            var push = new PushConstants
            {
                Mvp = mvp,
                MaterialAlbedo = material.Albedo,
                MaterialRoughness = material.Roughness,
                MaterialMetallic = material.Metallic,
                UseTexture = material.HasTexture ? 1u : 0u,
                TextureIndex = 0,
                Pad0 = 0
            };

            var pushSize = (uint)sizeof(PushConstants);
            _context.Vk.CmdPushConstants(drawCmd, _pipeline.Layout, ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit, 0, pushSize, &push);

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
        var vertexBytes = new byte[mesh.Vertices.Length * 9 * sizeof(float)];
        fixed (byte* p = vertexBytes)
        {
            var dst = (float*)p;
            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                var v = mesh.Vertices[i];
                dst[i * 9 + 0] = v.Position.X;
                dst[i * 9 + 1] = v.Position.Y;
                dst[i * 9 + 2] = v.Position.Z;
                dst[i * 9 + 3] = v.Color.X;
                dst[i * 9 + 4] = v.Color.Y;
                dst[i * 9 + 5] = v.Color.Z;
                dst[i * 9 + 6] = v.Normal.X;
                dst[i * 9 + 7] = v.Normal.Y;
                dst[i * 9 + 8] = v.Normal.Z;
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

    private byte[] BuildMeshVertices(Mesh mesh, Transform transform, Material material)
    {
        var matrix = transform.GetMatrix();
        var bytes = new byte[mesh.Vertices.Length * 9 * sizeof(float)];
        fixed (byte* p = bytes)
        {
            var dst = (float*)p;
            for (var i = 0; i < mesh.Vertices.Length; i++)
            {
                var v = mesh.Vertices[i];
                var worldPos = Vector3.Transform(v.Position, matrix);
                var normal = transform.TransformNormal(v.Normal);
                var color = v.Color * material.Albedo;
                dst[i * 9 + 0] = worldPos.X;
                dst[i * 9 + 1] = worldPos.Y;
                dst[i * 9 + 2] = worldPos.Z;
                dst[i * 9 + 3] = color.X;
                dst[i * 9 + 4] = color.Y;
                dst[i * 9 + 5] = color.Z;
                dst[i * 9 + 6] = normal.X;
                dst[i * 9 + 7] = normal.Y;
                dst[i * 9 + 8] = normal.Z;
            }
        }
        return bytes;
    }

    private Texture GetTexture(Material material)
    {
        if (!material.HasTexture)
            return _defaultTexture!;

        if (_textures.TryGetValue(material.TexturePath!, out var texture))
            return texture;

        if (!System.IO.File.Exists(material.TexturePath!))
            return _defaultTexture!;

        texture = new Texture(_context, material.TexturePath!);
        _textures[material.TexturePath!] = texture;
        return texture;
    }

    private DescriptorSet GetTextureDescriptorSet(Texture texture)
    {
        if (_textureDescriptorSets.TryGetValue(texture, out var descriptorSet))
            return descriptorSet;

        var layout = _pipeline.TextureDescriptorSetLayout;
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _textureDescriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };

        DescriptorSet set;
        var result = _context.Vk.AllocateDescriptorSets(_context.Device, &allocInfo, &set);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkAllocateDescriptorSets (texture) failed: {result}");

        var imageInfo = new DescriptorImageInfo
        {
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = texture.View,
            Sampler = texture.Sampler
        };

        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            PImageInfo = &imageInfo
        };

        _context.Vk.UpdateDescriptorSets(_context.Device, 1, &write, 0, null);
        _textureDescriptorSets[texture] = set;
        return set;
    }

    private FrameConstants BuildFrameConstants(World world, Camera camera)
    {
        var frameConstants = new FrameConstants
        {
            CameraPosition = camera.Position,
            LightCount = 0,
            AmbientColor = new Vector3(0.4f, 0.4f, 0.45f),
            AmbientPadding = 0
        };

        world.Each((Entity e, ref Light light) =>
        {
            if (frameConstants.LightCount >= 4)
                return;

            var index = (int)frameConstants.LightCount;
            frameConstants.LightCount++;
            SetLight(ref frameConstants, index, new GpuLight
            {
                Direction = light.Direction,
                Intensity = light.Intensity,
                Color = light.Color,
                Padding = 0
            });
        });

        // Fallback: if no light components exist, add a default directional light.
        if (frameConstants.LightCount == 0)
        {
            frameConstants.LightCount = 1;
            SetLight(ref frameConstants, 0, new GpuLight
            {
                Direction = new Vector3(0.5f, -1.0f, -0.5f),
                Intensity = 1.0f,
                Color = new Vector3(1.0f, 0.95f, 0.8f),
                Padding = 0
            });
        }

        return frameConstants;
    }

    private static void SetLight(ref FrameConstants frameConstants, int index, GpuLight light)
    {
        switch (index)
        {
            case 0: frameConstants.Light0 = light; break;
            case 1: frameConstants.Light1 = light; break;
            case 2: frameConstants.Light2 = light; break;
            case 3: frameConstants.Light3 = light; break;
        }
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
        _context.Vk.DestroyDescriptorPool(_context.Device, _textureDescriptorPool, null);
        _context.Vk.DestroyDescriptorPool(_context.Device, _frameDescriptorPool, null);

        foreach (var texture in _textures.Values)
            texture.Dispose();
        _textures.Clear();

        _defaultTexture?.Dispose();

        _frameConstantsBuffer.Dispose();

        _pipeline.Dispose();
    }
}
