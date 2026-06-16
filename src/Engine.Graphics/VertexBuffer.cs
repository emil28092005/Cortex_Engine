using System;
using Vortice.Vulkan;

namespace Engine.Graphics;

/// <summary>
/// Interleaved vertex: vec2 position + vec3 color.
/// </summary>
public sealed unsafe class VertexBuffer : IDisposable
{
    private readonly VulkanContext _context;
    public VkBuffer Buffer { get; }
    public VkDeviceMemory Memory { get; }
    public ulong Size { get; }

    public VertexBuffer(VulkanContext context, ReadOnlySpan<byte> data)
    {
        _context = context;
        Size = (ulong)data.Length;

        Buffer = CreateBuffer(Size, VkBufferUsageFlags.VertexBuffer);
        var memoryRequirements = GetMemoryRequirements(Buffer);
        Memory = AllocateMemory(memoryRequirements, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

        var result = _context.DeviceApi.vkBindBufferMemory(Buffer, Memory, 0);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkBindBufferMemory failed: {result}");

        CopyData(data);
    }

    private VkBuffer CreateBuffer(ulong size, VkBufferUsageFlags usage)
    {
        var createInfo = new VkBufferCreateInfo
        {
            sType = VkStructureType.BufferCreateInfo,
            size = size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive
        };

        var result = _context.DeviceApi.vkCreateBuffer(&createInfo, null, out var buffer);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateBuffer failed: {result}");
        return buffer;
    }

    private VkMemoryRequirements GetMemoryRequirements(VkBuffer buffer)
    {
        _context.DeviceApi.vkGetBufferMemoryRequirements(buffer, out var requirements);
        return requirements;
    }

    private VkDeviceMemory AllocateMemory(VkMemoryRequirements requirements, VkMemoryPropertyFlags properties)
    {
        var memoryTypeIndex = FindMemoryType(requirements.memoryTypeBits, properties);
        var allocateInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = requirements.size,
            memoryTypeIndex = memoryTypeIndex
        };

        var result = _context.DeviceApi.vkAllocateMemory(&allocateInfo, null, out var memory);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkAllocateMemory failed: {result}");
        return memory;
    }

    private uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        _context.InstanceApi.vkGetPhysicalDeviceMemoryProperties(_context.PhysicalDevice, out var memoryProperties);
        for (var i = 0; i < memoryProperties.memoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 &&
                (memoryProperties.memoryTypes[i].propertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }
        throw new InvalidOperationException("Failed to find suitable memory type.");
    }

    private void CopyData(ReadOnlySpan<byte> data)
    {
        void* mappedData;
        var result = _context.DeviceApi.vkMapMemory(Memory, 0, Size, VkMemoryMapFlags.None, &mappedData);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkMapMemory failed: {result}");

        fixed (byte* src = data)
        {
            System.Buffer.MemoryCopy(src, mappedData, (long)Size, data.Length);
        }

        _context.DeviceApi.vkUnmapMemory(Memory);
    }

    public void Dispose()
    {
        _context.DeviceApi.vkDeviceWaitIdle();
        _context.DeviceApi.vkDestroyBuffer(Buffer);
        _context.DeviceApi.vkFreeMemory(Memory);
    }
}
