using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanIndexBuffer : IDisposable
{
    public VkBuffer Buffer;
    public VkDeviceMemory Memory;
    public uint IndexCount;

    private readonly VkDevice _device;
    private bool _disposed;

    public VulkanIndexBuffer(VkDevice device, VkCommandPool commandPool, VkQueue queue,
        VulkanContext ctx, uint[] indices)
    {
        _device = device;
        IndexCount = (uint)indices.Length;
        var bufferSize = (ulong)(indices.Length * sizeof(uint));

        var stagingBuffer = VkBuffer.Null;
        var stagingMemory = VkDeviceMemory.Null;
        CreateStagingBuffer(device, ctx, bufferSize, &stagingBuffer, &stagingMemory);
        UploadToStaging(device, stagingMemory, indices, bufferSize);
        var buf = VkBuffer.Null;
        var mem = VkDeviceMemory.Null;
        CreateDeviceLocalBuffer(device, ctx, bufferSize, &buf, &mem);
        Buffer = buf;
        Memory = mem;
        CopyBuffer(device, commandPool, queue, stagingBuffer, Buffer, bufferSize);

        Vk.vkDestroyBuffer(device, stagingBuffer, 0);
        Vk.vkFreeMemory(device, stagingMemory, 0);
    }

    private static void CreateStagingBuffer(VkDevice device, VulkanContext ctx, ulong size,
        VkBuffer* pBuffer, VkDeviceMemory* pMemory)
    {
        var info = new VkBufferCreateInfo
        {
            sType = VkStructureType.BufferCreateInfo,
            size = size,
            usage = VkBufferUsageFlags.TransferSrc,
            sharingMode = VkSharingMode.Exclusive,
        };

        var result = Vk.vkCreateBuffer(device, &info, 0, pBuffer);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateBuffer (staging) failed: {result}");

        var reqs = new VkMemoryRequirements();
        Vk.vkGetBufferMemoryRequirements(device, *pBuffer, &reqs);

        var memTypeIndex = ctx.FindMemoryType(reqs.memoryTypeBits,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

        var allocInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = reqs.size,
            memoryTypeIndex = memTypeIndex,
        };

        result = Vk.vkAllocateMemory(device, &allocInfo, 0, pMemory);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkAllocateMemory (staging) failed: {result}");

        Vk.vkBindBufferMemory(device, *pBuffer, *pMemory, 0);
    }

    private static void UploadToStaging(VkDevice device, VkDeviceMemory memory, uint[] indices, ulong size)
    {
        void* pData = null;
        Vk.vkMapMemory(device, memory, 0, size, 0, &pData);
        fixed (uint* pIndices = indices)
        {
            System.Buffer.MemoryCopy(pIndices, pData, (long)size, (long)size);
        }
        Vk.vkUnmapMemory(device, memory);
    }

    private static void CreateDeviceLocalBuffer(VkDevice device, VulkanContext ctx, ulong size,
        VkBuffer* pBuffer, VkDeviceMemory* pMemory)
    {
        var info = new VkBufferCreateInfo
        {
            sType = VkStructureType.BufferCreateInfo,
            size = size,
            usage = VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.IndexBuffer,
            sharingMode = VkSharingMode.Exclusive,
        };

        var result = Vk.vkCreateBuffer(device, &info, 0, pBuffer);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateBuffer (index) failed: {result}");

        var reqs = new VkMemoryRequirements();
        Vk.vkGetBufferMemoryRequirements(device, *pBuffer, &reqs);

        var memTypeIndex = ctx.FindMemoryType(reqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

        var allocInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = reqs.size,
            memoryTypeIndex = memTypeIndex,
        };

        result = Vk.vkAllocateMemory(device, &allocInfo, 0, pMemory);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkAllocateMemory (index) failed: {result}");

        Vk.vkBindBufferMemory(device, *pBuffer, *pMemory, 0);
    }

    private static void CopyBuffer(VkDevice device, VkCommandPool pool, VkQueue queue,
        VkBuffer src, VkBuffer dst, ulong size)
    {
        var allocInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.CommandBufferAllocateInfo,
            commandPool = pool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = 1,
        };

        var cmd = VkCommandBuffer.Null;
        Vk.vkAllocateCommandBuffers(device, &allocInfo, &cmd);

        var beginInfo = new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.CommandBufferBeginInfo,
            flags = VkCommandBufferUsageFlags.OneTimeSubmit,
        };

        Vk.vkBeginCommandBuffer(cmd, &beginInfo);

        var copyRegion = new VkBufferCopy { srcOffset = 0, dstOffset = 0, size = size };
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

        Vk.vkFreeCommandBuffers(device, pool, 1, &cmd);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Buffer.Handle != 0) Vk.vkDestroyBuffer(_device, Buffer, 0);
        if (Memory.Handle != 0) Vk.vkFreeMemory(_device, Memory, 0);
    }
}
