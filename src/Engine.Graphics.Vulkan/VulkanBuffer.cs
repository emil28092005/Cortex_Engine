using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanBuffer : IDisposable
{
    public VkBuffer Buffer;
    public VkDeviceMemory Memory;
    public ulong Size;
    public void* MappedData;

    private readonly VulkanContext _ctx;
    private bool _disposed;

    public VulkanBuffer(VulkanContext ctx, ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties)
    {
        _ctx = ctx;
        Size = size;

        VkBufferCreateInfo bufferInfo;
        bufferInfo.sType = VkStructureType.BufferCreateInfo;
        bufferInfo.pNext = null;
        bufferInfo.flags = 0;
        bufferInfo.size = size;
        bufferInfo.usage = usage;
        bufferInfo.sharingMode = VkSharingMode.Exclusive;
        bufferInfo.queueFamilyIndexCount = 0;
        bufferInfo.pQueueFamilyIndices = null;

        VkBuffer buffer;
        VkResult result = Vk.vkCreateBuffer(_ctx.Device, &bufferInfo, null, &buffer);
        Vk.CheckResult(result, "vkCreateBuffer");
        Buffer = buffer;

        VkMemoryRequirements2 memReq;
        Vk.vkGetBufferMemoryRequirements(_ctx.Device, Buffer, &memReq);

        VkMemoryAllocateInfo allocInfo;
        allocInfo.sType = VkStructureType.MemoryAllocateInfo;
        allocInfo.pNext = null;
        allocInfo.allocationSize = memReq.size;
        allocInfo.memoryTypeIndex = _ctx.FindMemoryType(memReq.memoryTypeBits, properties);

        VkDeviceMemory memory;
        result = Vk.vkAllocateMemory(_ctx.Device, &allocInfo, null, &memory);
        Vk.CheckResult(result, "vkAllocateMemory (buffer)");
        Memory = memory;

        result = Vk.vkBindBufferMemory(_ctx.Device, Buffer, Memory, 0);
        Vk.CheckResult(result, "vkBindBufferMemory");

        if ((properties & VkMemoryPropertyFlags.HostVisible) != 0)
        {
            void* mapped;
            result = Vk.vkMapMemory(_ctx.Device, Memory, 0, size, 0, &mapped);
            Vk.CheckResult(result, "vkMapMemory");
            MappedData = mapped;
        }
    }

    public void Write(void* data, ulong size, ulong offset = 0)
    {
        if (MappedData == null)
            throw new InvalidOperationException("Buffer is not host-visible/mapped.");

        System.Buffer.MemoryCopy(data, (void*)((byte*)MappedData + offset), size, size);
    }

    public void Write<T>(T[] data, ulong offset = 0) where T : struct
    {
        var size = (ulong)(data.Length * Marshal.SizeOf<T>());
        fixed (T* pData = data)
        {
            Write(pData, size, offset);
        }
    }

    public static unsafe VulkanBuffer CreateStaging(VulkanContext ctx, void* data, ulong size)
    {
        var staging = new VulkanBuffer(ctx, size, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
        staging.Write(data, size);
        return staging;
    }

    public static unsafe void CopyBuffer(VulkanContext ctx, VkCommandPool cmdPool, VkBuffer src, VkBuffer dst, ulong size)
    {
        VkCommandBuffer cmd = BeginSingleTimeCommands(ctx, cmdPool);

        var region = new VkBufferCopy { srcOffset = 0, dstOffset = 0, size = size };
        Vk.vkCmdCopyBuffer(cmd, src, dst, 1, &region);

        EndSingleTimeCommands(ctx, cmdPool, cmd);
    }

    public static VulkanBuffer CreateDeviceLocal<T>(VulkanContext ctx, VkCommandPool cmdPool, T[] data, VkBufferUsageFlags usage) where T : struct
    {
        var size = (ulong)(data.Length * Marshal.SizeOf<T>());
        var staging = new VulkanBuffer(ctx, size, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

        fixed (T* pData = data)
        {
            staging.Write(pData, size);
        }

        var deviceBuffer = new VulkanBuffer(ctx, size, usage | VkBufferUsageFlags.TransferDst, VkMemoryPropertyFlags.DeviceLocal);
        CopyBuffer(ctx, cmdPool, staging.Buffer, deviceBuffer.Buffer, size);

        staging.Dispose();
        return deviceBuffer;
    }

    public static unsafe VkCommandBuffer BeginSingleTimeCommands(VulkanContext ctx, VkCommandPool cmdPool)
    {
        VkCommandBufferAllocateInfo allocInfo;
        allocInfo.sType = VkStructureType.CommandBufferAllocateInfo;
        allocInfo.pNext = null;
        allocInfo.commandPool = cmdPool;
        allocInfo.level = VkCommandBufferLevel.Primary;
        allocInfo.commandBufferCount = 1;

        VkCommandBuffer cmd;
        Vk.vkAllocateCommandBuffers(ctx.Device, &allocInfo, &cmd);

        VkCommandBufferBeginInfo beginInfo;
        beginInfo.sType = VkStructureType.CommandBufferBeginInfo;
        beginInfo.pNext = null;
        beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
        beginInfo.pInheritanceInfo = null;

        Vk.vkBeginCommandBuffer(cmd, &beginInfo);
        return cmd;
    }

    public static unsafe void EndSingleTimeCommands(VulkanContext ctx, VkCommandPool cmdPool, VkCommandBuffer cmd)
    {
        Vk.vkEndCommandBuffer(cmd);

        VkSubmitInfo submitInfo;
        submitInfo.sType = VkStructureType.SubmitInfo;
        submitInfo.pNext = null;
        submitInfo.waitSemaphoreCount = 0;
        submitInfo.pWaitSemaphores = null;
        submitInfo.pWaitDstStageMask = null;
        submitInfo.commandBufferCount = 1;
        submitInfo.pCommandBuffers = &cmd;
        submitInfo.signalSemaphoreCount = 0;
        submitInfo.pSignalSemaphores = null;

        Vk.vkQueueSubmit(ctx.GraphicsQueue, 1, &submitInfo, default);
        Vk.vkQueueWaitIdle(ctx.GraphicsQueue);

        Vk.vkFreeCommandBuffers(ctx.Device, cmdPool, 1, &cmd);
    }

    public unsafe void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (MappedData != null)
        {
            Vk.vkUnmapMemory(_ctx.Device, Memory);
            MappedData = null;
        }
        if (Buffer.Value != 0) Vk.vkDestroyBuffer(_ctx.Device, Buffer, null);
        if (Memory.Value != 0) Vk.vkFreeMemory(_ctx.Device, Memory, null);
    }
}
