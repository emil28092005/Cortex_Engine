using System;
using System.IO;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.Graphics;

/// <summary>
/// Captures the current swapchain image to a PNG file on disk.
/// Used by AI agents to visually inspect the running engine.
/// </summary>
public sealed unsafe class ScreenshotCapture : IDisposable
{
    private readonly VulkanContext _context;
    private readonly Swapchain _swapchain;
    private Silk.NET.Vulkan.Buffer _stagingBuffer;
    private DeviceMemory _stagingMemory;
    private ulong _stagingSize;
    private bool _requested;
    private string _outputPath = string.Empty;
    private bool _ready;

    public ScreenshotCapture(VulkanContext context, Swapchain swapchain)
    {
        _context = context;
        _swapchain = swapchain;
    }

    /// <summary>
    /// Request a screenshot to be captured on the next frame.
    /// </summary>
    public void Request(string outputPath)
    {
        _outputPath = outputPath;
        _requested = true;
        _ready = false;
    }

    /// <summary>
    /// True if a screenshot has been requested but not yet saved.
    /// </summary>
    public bool IsRequested => _requested;

    /// <summary>
    /// Records the image readback commands into the given command buffer.
    /// Must be called after the render pass has ended and before the image is presented.
    /// </summary>
    public void RecordReadback(CommandBuffer cmd, Silk.NET.Vulkan.Image sourceImage, uint width, uint height, Format format)
    {
        if (!_requested)
            return;

        var pixelSize = GetPixelSize(format);
        var rowPitch = width * pixelSize;
        var imageSize = rowPitch * height;

        EnsureStagingBuffer(imageSize);

        // Transition from present layout to transfer source.
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = ImageLayout.PresentSrcKhr,
            NewLayout = ImageLayout.TransferSrcOptimal,
            SrcAccessMask = AccessFlags.None,
            DstAccessMask = AccessFlags.TransferReadBit,
            Image = sourceImage,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        _context.Vk.CmdPipelineBarrier(cmd, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &barrier);

        var copyRegion = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
            ImageOffset = new Offset3D(0, 0, 0),
            ImageExtent = new Extent3D(width, height, 1)
        };

        _context.Vk.CmdCopyImageToBuffer(cmd, sourceImage, ImageLayout.TransferSrcOptimal, _stagingBuffer, 1, &copyRegion);

        // Transition back to present layout.
        barrier.OldLayout = ImageLayout.TransferSrcOptimal;
        barrier.NewLayout = ImageLayout.PresentSrcKhr;
        barrier.SrcAccessMask = AccessFlags.TransferReadBit;
        barrier.DstAccessMask = AccessFlags.None;

        _context.Vk.CmdPipelineBarrier(cmd, PipelineStageFlags.TransferBit, PipelineStageFlags.TransferBit, 0, 0, null, 0, null, 1, &barrier);

        _ready = true;
    }

    /// <summary>
    /// Save the captured pixels to disk. Must be called after the command buffer containing the readback has finished.
    /// </summary>
    public void Save(uint width, uint height, Format format)
    {
        if (!_ready)
            return;

        var pixelSize = GetPixelSize(format);
        var rowPitch = width * pixelSize;
        var imageSize = rowPitch * height;

        void* mappedData;
        var result = _context.Vk.MapMemory(_context.Device, _stagingMemory, 0, imageSize, MemoryMapFlags.None, &mappedData);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkMapMemory failed: {result}");

        try
        {
            var directory = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            SavePixels(mappedData, width, height, rowPitch, format);
        }
        finally
        {
            _context.Vk.UnmapMemory(_context.Device, _stagingMemory);
        }

        Console.WriteLine($"Screenshot saved: {_outputPath}");
        _requested = false;
        _ready = false;
    }

    private void SavePixels(void* mappedData, uint width, uint height, uint rowPitch, Format format)
    {
        if (format == Format.B8G8R8A8Unorm || format == Format.B8G8R8A8Srgb)
        {
            SaveBgra(mappedData, width, height, rowPitch);
            return;
        }

        if (format == Format.R8G8B8A8Unorm || format == Format.R8G8B8A8Srgb)
        {
            SaveRgba(mappedData, width, height, rowPitch);
            return;
        }

        throw new NotSupportedException($"Screenshot format {format} is not supported.");
    }

    private void SaveBgra(void* mappedData, uint width, uint height, uint rowPitch)
    {
        using var image = new SixLabors.ImageSharp.Image<Rgba32>((int)width, (int)height);
        var src = (byte*)mappedData;

        for (var y = 0; y < height; y++)
        {
            var rowStart = src + y * rowPitch;
            for (var x = 0; x < width; x++)
            {
                var b = rowStart[x * 4 + 0];
                var g = rowStart[x * 4 + 1];
                var r = rowStart[x * 4 + 2];
                var a = rowStart[x * 4 + 3];
                image[x, y] = new Rgba32(r, g, b, a);
            }
        }

        image.SaveAsPng(_outputPath);
    }

    private void SaveRgba(void* mappedData, uint width, uint height, uint rowPitch)
    {
        using var image = new SixLabors.ImageSharp.Image<Rgba32>((int)width, (int)height);
        var src = (byte*)mappedData;

        for (var y = 0; y < height; y++)
        {
            var rowStart = src + y * rowPitch;
            for (var x = 0; x < width; x++)
            {
                var r = rowStart[x * 4 + 0];
                var g = rowStart[x * 4 + 1];
                var b = rowStart[x * 4 + 2];
                var a = rowStart[x * 4 + 3];
                image[x, y] = new Rgba32(r, g, b, a);
            }
        }

        image.SaveAsPng(_outputPath);
    }

    private void EnsureStagingBuffer(ulong size)
    {
        if (_stagingSize >= size)
            return;

        if (_stagingBuffer.Handle != 0)
        {
            _context.Vk.DestroyBuffer(_context.Device, _stagingBuffer, null);
            _context.Vk.FreeMemory(_context.Device, _stagingMemory, null);
        }

        _stagingSize = size;
        _stagingBuffer = CreateBuffer(size, BufferUsageFlags.TransferDstBit);
        var requirements = GetMemoryRequirements(_stagingBuffer);
        _stagingMemory = AllocateMemory(requirements, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var result = _context.Vk.BindBufferMemory(_context.Device, _stagingBuffer, _stagingMemory, 0);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkBindBufferMemory failed: {result}");
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

    private MemoryRequirements GetMemoryRequirements(Silk.NET.Vulkan.Buffer buffer)
    {
        MemoryRequirements requirements;
        _context.Vk.GetBufferMemoryRequirements(_context.Device, buffer, &requirements);
        return requirements;
    }

    private DeviceMemory AllocateMemory(MemoryRequirements requirements, MemoryPropertyFlags properties)
    {
        var memoryTypeIndex = FindMemoryType(requirements.MemoryTypeBits, properties);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = memoryTypeIndex
        };

        DeviceMemory memory;
        var result = _context.Vk.AllocateMemory(_context.Device, &allocInfo, null, &memory);
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
        throw new InvalidOperationException("Failed to find suitable memory type.");
    }

    private static uint GetPixelSize(Format format)
    {
        return format switch
        {
            Format.B8G8R8A8Unorm or Format.B8G8R8A8Srgb or Format.R8G8B8A8Unorm or Format.R8G8B8A8Srgb => 4,
            _ => throw new NotSupportedException($"Format {format} is not supported for screenshots.")
        };
    }

    public void Dispose()
    {
        if (_stagingBuffer.Handle != 0)
        {
            _context.Vk.DeviceWaitIdle(_context.Device);
            _context.Vk.DestroyBuffer(_context.Device, _stagingBuffer, null);
            _context.Vk.FreeMemory(_context.Device, _stagingMemory, null);
        }
    }
}
