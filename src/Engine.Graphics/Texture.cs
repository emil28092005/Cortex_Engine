using System;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphics;

/// <summary>
/// A Vulkan texture: image, device memory, image view, and sampler.
/// </summary>
public sealed unsafe class Texture : IDisposable
{
    private readonly VulkanContext _context;
    public Silk.NET.Vulkan.Image Image { get; }
    public DeviceMemory Memory { get; }
    public ImageView View { get; }
    public Sampler Sampler { get; }
    public uint Width { get; }
    public uint Height { get; }

    public Texture(VulkanContext context, string path)
    {
        _context = context;

        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(path);
        Width = (uint)image.Width;
        Height = (uint)image.Height;

        var pixels = new byte[Width * Height * 4];
        image.CopyPixelDataTo(pixels);

        Image = CreateImage(Width, Height);
        var memoryRequirements = GetImageMemoryRequirements(Image);
        Memory = AllocateMemory(memoryRequirements, MemoryPropertyFlags.DeviceLocalBit);

        var bindResult = _context.Vk.BindImageMemory(_context.Device, Image, Memory, 0);
        if (bindResult != Result.Success)
            throw new InvalidOperationException($"vkBindImageMemory failed: {bindResult}");

        UploadPixels(pixels);

        View = CreateImageView(Image);
        Sampler = CreateSampler();
    }

    private Silk.NET.Vulkan.Image CreateImage(uint width, uint height)
    {
        var createInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(width, height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Format = Format.R8G8B8A8Srgb,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            SharingMode = SharingMode.Exclusive,
            Samples = SampleCountFlags.Count1Bit
        };

        Silk.NET.Vulkan.Image image;
        var result = _context.Vk.CreateImage(_context.Device, &createInfo, null, &image);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateImage failed: {result}");
        return image;
    }

    private MemoryRequirements GetImageMemoryRequirements(Silk.NET.Vulkan.Image image)
    {
        MemoryRequirements requirements;
        _context.Vk.GetImageMemoryRequirements(_context.Device, image, &requirements);
        return requirements;
    }

    private DeviceMemory AllocateMemory(MemoryRequirements requirements, MemoryPropertyFlags properties)
    {
        var memoryTypeIndex = FindMemoryType(requirements.MemoryTypeBits, properties);
        var allocateInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = memoryTypeIndex
        };

        DeviceMemory memory;
        var result = _context.Vk.AllocateMemory(_context.Device, &allocateInfo, null, &memory);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkAllocateMemory failed: {result}");
        return memory;
    }

    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        PhysicalDeviceMemoryProperties memoryProperties;
        _context.Vk.GetPhysicalDeviceMemoryProperties(_context.PhysicalDevice, &memoryProperties);
        for (var i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 &&
                (memoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }
        throw new InvalidOperationException("Failed to find suitable memory type for texture.");
    }

    private void UploadPixels(byte[] pixels)
    {
        var imageSize = (ulong)pixels.Length;

        var stagingBuffer = CreateBuffer(imageSize, BufferUsageFlags.TransferSrcBit);
        var stagingMemory = AllocateStagingMemory(stagingBuffer);

        var bindResult = _context.Vk.BindBufferMemory(_context.Device, stagingBuffer, stagingMemory, 0);
        if (bindResult != Result.Success)
            throw new InvalidOperationException($"vkBindBufferMemory for staging failed: {bindResult}");

        void* mappedData;
        var mapResult = _context.Vk.MapMemory(_context.Device, stagingMemory, 0, imageSize, MemoryMapFlags.None, &mappedData);
        if (mapResult != Result.Success)
            throw new InvalidOperationException($"vkMapMemory failed: {mapResult}");

        fixed (byte* src = pixels)
        {
            global::System.Buffer.MemoryCopy(src, mappedData, (long)imageSize, pixels.Length);
        }

        _context.Vk.UnmapMemory(_context.Device, stagingMemory);

        ExecuteOneTimeCommand(cmd =>
        {
            TransitionImageLayout(cmd, Image, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);

            var bufferCopy = new BufferImageCopy
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new ImageSubresourceLayers
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                },
                ImageOffset = new Offset3D(0, 0, 0),
                ImageExtent = new Extent3D(Width, Height, 1)
            };

            _context.Vk.CmdCopyBufferToImage(cmd, stagingBuffer, Image, ImageLayout.TransferDstOptimal, 1, &bufferCopy);

            TransitionImageLayout(cmd, Image, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);
        });

        _context.Vk.FreeMemory(_context.Device, stagingMemory, null);
        _context.Vk.DestroyBuffer(_context.Device, stagingBuffer, null);
    }

    private Silk.NET.Vulkan.Buffer CreateBuffer(ulong size, BufferUsageFlags usage)
    {
        var createInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        Silk.NET.Vulkan.Buffer buffer;
        var result = _context.Vk.CreateBuffer(_context.Device, &createInfo, null, &buffer);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateBuffer failed: {result}");
        return buffer;
    }

    private DeviceMemory AllocateStagingMemory(Silk.NET.Vulkan.Buffer buffer)
    {
        var requirements = GetBufferMemoryRequirements(buffer);
        return AllocateMemory(requirements, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
    }

    private MemoryRequirements GetBufferMemoryRequirements(Silk.NET.Vulkan.Buffer buffer)
    {
        MemoryRequirements requirements;
        _context.Vk.GetBufferMemoryRequirements(_context.Device, buffer, &requirements);
        return requirements;
    }

    private void TransitionImageLayout(CommandBuffer cmd, Silk.NET.Vulkan.Image image, ImageLayout oldLayout, ImageLayout newLayout)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = uint.MaxValue,
            DstQueueFamilyIndex = uint.MaxValue,
            Image = image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        var srcStage = PipelineStageFlags.TopOfPipeBit;
        var dstStage = PipelineStageFlags.TransferBit;
        AccessFlags srcAccessMask = 0;
        AccessFlags dstAccessMask = AccessFlags.TransferWriteBit;

        if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.FragmentShaderBit;
            srcAccessMask = AccessFlags.TransferWriteBit;
            dstAccessMask = AccessFlags.ShaderReadBit;
        }

        barrier.SrcAccessMask = srcAccessMask;
        barrier.DstAccessMask = dstAccessMask;

        _context.Vk.CmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrier);
    }

    private void ExecuteOneTimeCommand(Action<CommandBuffer> action)
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _context.CommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        CommandBuffer commandBuffer;
        var result = _context.Vk.AllocateCommandBuffers(_context.Device, &allocInfo, &commandBuffer);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkAllocateCommandBuffers failed: {result}");

        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _context.Vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        action(commandBuffer);

        _context.Vk.EndCommandBuffer(commandBuffer);

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        _context.Vk.QueueSubmit(_context.GraphicsQueue, 1, &submitInfo, new Fence());
        _context.Vk.QueueWaitIdle(_context.GraphicsQueue);

        _context.Vk.FreeCommandBuffers(_context.Device, _context.CommandPool, 1, &commandBuffer);
    }

    private ImageView CreateImageView(Silk.NET.Vulkan.Image image)
    {
        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = Format.R8G8B8A8Srgb,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        ImageView view;
        var result = _context.Vk.CreateImageView(_context.Device, &createInfo, null, &view);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateImageView failed: {result}");
        return view;
    }

    private Sampler CreateSampler()
    {
        var createInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = false,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            MipmapMode = SamplerMipmapMode.Linear,
            MipLodBias = 0.0f,
            MinLod = 0.0f,
            MaxLod = 1.0f
        };

        Sampler sampler;
        var result = _context.Vk.CreateSampler(_context.Device, &createInfo, null, &sampler);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateSampler failed: {result}");
        return sampler;
    }

    public void Dispose()
    {
        _context.Vk.DeviceWaitIdle(_context.Device);
        _context.Vk.DestroySampler(_context.Device, Sampler, null);
        _context.Vk.DestroyImageView(_context.Device, View, null);
        _context.Vk.DestroyImage(_context.Device, Image, null);
        _context.Vk.FreeMemory(_context.Device, Memory, null);
    }
}
