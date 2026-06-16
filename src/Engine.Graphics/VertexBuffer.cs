using System;
using Silk.NET.Core;
using Silk.NET.Vulkan;

namespace Engine.Graphics;

/// <summary>
/// Interleaved vertex buffer: vec2 position + vec3 color.
/// Uses Silk.NET.Vulkan.
/// </summary>
public sealed unsafe class VertexBuffer : IDisposable
{
    private readonly VulkanContext _context;
    public Silk.NET.Vulkan.Buffer Buffer { get; }
    public DeviceMemory Memory { get; }
    public ulong Size { get; }

    public VertexBuffer(VulkanContext context, ReadOnlySpan<byte> data)
    {
        _context = context;
        Size = (ulong)data.Length;

        Buffer = CreateBuffer(Size, BufferUsageFlags.VertexBufferBit);
        var memoryRequirements = GetMemoryRequirements(Buffer);
        Memory = AllocateMemory(memoryRequirements, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        var result = _context.Vk.BindBufferMemory(_context.Device, Buffer, Memory, 0);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkBindBufferMemory failed: {result}");

        CopyData(data);
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
        throw new InvalidOperationException("Failed to find suitable memory type.");
    }

    private void CopyData(ReadOnlySpan<byte> data)
    {
        void* mappedData;
        var result = _context.Vk.MapMemory(_context.Device, Memory, 0, Size, MemoryMapFlags.None, &mappedData);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkMapMemory failed: {result}");

        fixed (byte* src = data)
        {
            global::System.Buffer.MemoryCopy(src, mappedData, (long)Size, data.Length);
        }

        _context.Vk.UnmapMemory(_context.Device, Memory);
    }

    public void Dispose()
    {
        _context.Vk.DeviceWaitIdle(_context.Device);
        _context.Vk.DestroyBuffer(_context.Device, Buffer, null);
        _context.Vk.FreeMemory(_context.Device, Memory, null);
    }
}
