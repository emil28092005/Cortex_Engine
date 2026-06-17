using System.Runtime.InteropServices;
using Engine.Core;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanVertexBuffer : IDisposable
{
    public VkBuffer Buffer;
    public VkDeviceMemory Memory;

    private readonly VkDevice _device;
    private VkBuffer _stagingBuffer;
    private VkDeviceMemory _stagingMemory;
    private bool _disposed;

    public VulkanVertexBuffer(VkDevice device, VkPhysicalDevice physicalDevice,
        VkCommandPool commandPool, VkQueue queue, VulkanContext ctx, Vertex[] vertices)
    {
        _device = device;
        var bufferSize = (ulong)(vertices.Length * sizeof(Vertex));

        CreateStagingBuffer(bufferSize, ctx);
        UploadToStaging(vertices, bufferSize);
        CreateDeviceLocalBuffer(bufferSize, ctx);
        CopyBuffer(commandPool, queue, _stagingBuffer, Buffer, bufferSize);
        DestroyStaging();

        Console.WriteLine($"[Vulkan] Vertex buffer created: {vertices.Length} vertices, {bufferSize} bytes");
    }

    private void CreateStagingBuffer(ulong size, VulkanContext ctx)
    {
        var info = new VkBufferCreateInfo
        {
            sType = VkStructureType.BufferCreateInfo,
            size = size,
            usage = VkBufferUsageFlags.TransferSrc,
            sharingMode = VkSharingMode.Exclusive,
        };

        var buf = VkBuffer.Null;
        var result = Vk.vkCreateBuffer(_device, &info, 0, &buf);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateBuffer (staging) failed: {result}");
        _stagingBuffer = buf;

        var reqs = new VkMemoryRequirements();
        Vk.vkGetBufferMemoryRequirements(_device, _stagingBuffer, &reqs);

        var memTypeIndex = ctx.FindMemoryType(reqs.memoryTypeBits,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

        var allocInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = reqs.size,
            memoryTypeIndex = memTypeIndex,
        };

        var mem = VkDeviceMemory.Null;
        result = Vk.vkAllocateMemory(_device, &allocInfo, 0, &mem);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkAllocateMemory (staging) failed: {result}");
        _stagingMemory = mem;

        Vk.vkBindBufferMemory(_device, _stagingBuffer, _stagingMemory, 0);
    }

    private void UploadToStaging(Vertex[] vertices, ulong size)
    {
        void* pData = null;
        Vk.vkMapMemory(_device, _stagingMemory, 0, size, 0, &pData);
        fixed (Vertex* pVerts = vertices)
        {
            System.Buffer.MemoryCopy(pVerts, pData, (long)size, (long)size);
        }
        Vk.vkUnmapMemory(_device, _stagingMemory);
    }

    private void CreateDeviceLocalBuffer(ulong size, VulkanContext ctx)
    {
        var info = new VkBufferCreateInfo
        {
            sType = VkStructureType.BufferCreateInfo,
            size = size,
            usage = VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.VertexBuffer,
            sharingMode = VkSharingMode.Exclusive,
        };

        var buf = VkBuffer.Null;
        var result = Vk.vkCreateBuffer(_device, &info, 0, &buf);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateBuffer (vertex) failed: {result}");
        Buffer = buf;

        var reqs = new VkMemoryRequirements();
        Vk.vkGetBufferMemoryRequirements(_device, Buffer, &reqs);

        var memTypeIndex = ctx.FindMemoryType(reqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

        var allocInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = reqs.size,
            memoryTypeIndex = memTypeIndex,
        };

        var mem = VkDeviceMemory.Null;
        result = Vk.vkAllocateMemory(_device, &allocInfo, 0, &mem);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkAllocateMemory (vertex) failed: {result}");
        Memory = mem;

        Vk.vkBindBufferMemory(_device, Buffer, Memory, 0);
    }

    private void CopyBuffer(VkCommandPool pool, VkQueue queue, VkBuffer src, VkBuffer dst, ulong size)
    {
        var allocInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.CommandBufferAllocateInfo,
            commandPool = pool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = 1,
        };

        var cmd = VkCommandBuffer.Null;
        Vk.vkAllocateCommandBuffers(_device, &allocInfo, &cmd);

        var beginInfo = new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.CommandBufferBeginInfo,
            flags = VkCommandBufferUsageFlags.OneTimeSubmit,
        };

        Vk.vkBeginCommandBuffer(cmd, &beginInfo);

        var copyRegion = new VkBufferCopy
        {
            srcOffset = 0,
            dstOffset = 0,
            size = size,
        };
        Vk.vkCmdCopyBuffer(cmd, src, dst, 1, &copyRegion);

        Vk.vkEndCommandBuffer(cmd);

        var cmdInfo = new VkCommandBufferSubmitInfo
        {
            sType = VkStructureType.CommandBufferSubmitInfo,
            commandBuffer = cmd,
        };

        var submitInfo = new VkSubmitInfo2
        {
            sType = VkStructureType.SubmitInfo2,
            commandBufferInfoCount = 1,
            pCommandBufferInfos = &cmdInfo,
        };

        Vk.vkQueueSubmit2(queue, 1, &submitInfo, VkFence.Null);
        Vk.vkQueueWaitIdle(queue);

        Vk.vkFreeCommandBuffers(_device, pool, 1, &cmd);
    }

    private void DestroyStaging()
    {
        if (_stagingBuffer.Handle != 0) Vk.vkDestroyBuffer(_device, _stagingBuffer, 0);
        if (_stagingMemory.Handle != 0) Vk.vkFreeMemory(_device, _stagingMemory, 0);
        _stagingBuffer = VkBuffer.Null;
        _stagingMemory = VkDeviceMemory.Null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Buffer.Handle != 0) Vk.vkDestroyBuffer(_device, Buffer, 0);
        if (Memory.Handle != 0) Vk.vkFreeMemory(_device, Memory, 0);
    }
}
