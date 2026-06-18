using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanFrameResources : IDisposable
{
    public const int MaxFramesInFlight = 2;
    public const ulong UboSize = 64;

    public VkCommandPool CommandPool;
    public VkCommandBuffer[] CommandBuffers = new VkCommandBuffer[MaxFramesInFlight];
    public VkFence[] FrameFences = new VkFence[MaxFramesInFlight];
    public VkSemaphore[] AcquireSemaphores = new VkSemaphore[MaxFramesInFlight];
    public VkSemaphore[] SubmitSemaphores = Array.Empty<VkSemaphore>();

    public VkBuffer[] UboBuffers = new VkBuffer[MaxFramesInFlight];
    public VkDeviceMemory[] UboMemories = new VkDeviceMemory[MaxFramesInFlight];
    public VkDescriptorSet[] DescriptorSets = new VkDescriptorSet[MaxFramesInFlight];

    public VkDescriptorPool DescriptorPool;

    private readonly VkDevice _device;
    private bool _disposed;

    public VulkanFrameResources(VkDevice device, uint queueFamilyIndex, uint swapchainImageCount,
        VulkanContext ctx, VkDescriptorSetLayout descriptorSetLayout)
    {
        _device = device;

        var poolInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.CommandPoolCreateInfo,
            flags = VkCommandPoolCreateFlags.ResetCommandBuffer,
            queueFamilyIndex = queueFamilyIndex,
        };

        var cmdPool = VkCommandPool.Null;
        var result = Vk.vkCreateCommandPool(_device, &poolInfo, 0, &cmdPool);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateCommandPool failed: {result}");
        CommandPool = cmdPool;

        var allocInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.CommandBufferAllocateInfo,
            commandPool = CommandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = MaxFramesInFlight,
        };

        fixed (VkCommandBuffer* cmdPtr = CommandBuffers)
        {
            result = Vk.vkAllocateCommandBuffers(_device, &allocInfo, cmdPtr);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkAllocateCommandBuffers failed: {result}");
        }

        var fenceInfo = new VkFenceCreateInfo
        {
            sType = VkStructureType.FenceCreateInfo,
            flags = VkFenceCreateFlags.Signaled,
        };

        var semInfo = new VkSemaphoreCreateInfo
        {
            sType = VkStructureType.SemaphoreCreateInfo,
        };

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            var fence = VkFence.Null;
            result = Vk.vkCreateFence(_device, &fenceInfo, 0, &fence);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateFence failed: {result}");
            FrameFences[i] = fence;

            var sem = VkSemaphore.Null;
            result = Vk.vkCreateSemaphore(_device, &semInfo, 0, &sem);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateSemaphore (acquire) failed: {result}");
            AcquireSemaphores[i] = sem;
        }

        SubmitSemaphores = new VkSemaphore[swapchainImageCount];
        for (int i = 0; i < swapchainImageCount; i++)
        {
            var sem = VkSemaphore.Null;
            result = Vk.vkCreateSemaphore(_device, &semInfo, 0, &sem);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateSemaphore (submit) failed: {result}");
            SubmitSemaphores[i] = sem;
        }

        CreateUniformBuffers(ctx);
        CreateDescriptorPoolAndSets(descriptorSetLayout);

        Console.WriteLine($"[Vulkan] Frame resources: {MaxFramesInFlight} frames in flight, {swapchainImageCount} submit semaphores");
    }

    private void CreateUniformBuffers(VulkanContext ctx)
    {
        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            var info = new VkBufferCreateInfo
            {
                sType = VkStructureType.BufferCreateInfo,
                size = UboSize,
                usage = VkBufferUsageFlags.UniformBuffer,
                sharingMode = VkSharingMode.Exclusive,
            };

            var buf = VkBuffer.Null;
            var result = Vk.vkCreateBuffer(_device, &info, 0, &buf);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateBuffer (UBO) failed: {result}");
            UboBuffers[i] = buf;

            var reqs = new VkMemoryRequirements();
            Vk.vkGetBufferMemoryRequirements(_device, buf, &reqs);

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
                throw new InvalidOperationException($"vkAllocateMemory (UBO) failed: {result}");
            UboMemories[i] = mem;

            Vk.vkBindBufferMemory(_device, buf, mem, 0);
        }
    }

    private void CreateDescriptorPoolAndSets(VkDescriptorSetLayout layout)
    {
        var poolSizes = stackalloc VkDescriptorPoolSize[2];
        poolSizes[0] = new VkDescriptorPoolSize
        {
            type = VkDescriptorType.UniformBuffer,
            descriptorCount = MaxFramesInFlight,
        };
        poolSizes[1] = new VkDescriptorPoolSize
        {
            type = VkDescriptorType.CombinedImageSampler,
            descriptorCount = MaxFramesInFlight,
        };

        var poolInfo = new VkDescriptorPoolCreateInfo
        {
            sType = VkStructureType.DescriptorPoolCreateInfo,
            flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet,
            maxSets = MaxFramesInFlight,
            poolSizeCount = 2,
            pPoolSizes = poolSizes,
        };

        var descPool = VkDescriptorPool.Null;
        var result = Vk.vkCreateDescriptorPool(_device, &poolInfo, 0, &descPool);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateDescriptorPool failed: {result}");
        DescriptorPool = descPool;

        var layouts = stackalloc VkDescriptorSetLayout[MaxFramesInFlight];
        for (int i = 0; i < MaxFramesInFlight; i++)
            layouts[i] = layout;

        var allocInfo = new VkDescriptorSetAllocateInfo
        {
            sType = VkStructureType.DescriptorSetAllocateInfo,
            descriptorPool = DescriptorPool,
            descriptorSetCount = MaxFramesInFlight,
            pSetLayouts = layouts,
        };

        fixed (VkDescriptorSet* setPtr = DescriptorSets)
        {
            result = Vk.vkAllocateDescriptorSets(_device, &allocInfo, setPtr);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkAllocateDescriptorSets failed: {result}");
        }

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            var bufferInfo = new VkDescriptorBufferInfo
            {
                buffer = UboBuffers[i],
                offset = 0,
                range = UboSize,
            };

            var write = new VkWriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                dstSet = DescriptorSets[i],
                dstBinding = 0,
                dstArrayElement = 0,
                descriptorCount = 1,
                descriptorType = VkDescriptorType.UniformBuffer,
                pBufferInfo = &bufferInfo,
            };

            Vk.vkUpdateDescriptorSets(_device, 1, &write, 0, 0);
        }
    }

    public void UpdateShadowDescriptor(int frameIndex, VkSampler sampler, VkImageView shadowView)
    {
        var imageInfo = new VkDescriptorImageInfo
        {
            sampler = sampler,
            imageView = shadowView,
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
        };

        var write = new VkWriteDescriptorSet
        {
            sType = VkStructureType.WriteDescriptorSet,
            dstSet = DescriptorSets[frameIndex],
            dstBinding = 1,
            dstArrayElement = 0,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            pImageInfo = (nint)(&imageInfo),
        };

        Vk.vkUpdateDescriptorSets(_device, 1, &write, 0, 0);
    }

    public void UpdateUbo(int frameIndex, void* data, ulong size)
    {
        void* pData = null;
        Vk.vkMapMemory(_device, UboMemories[frameIndex], 0, size, 0, &pData);
        System.Buffer.MemoryCopy(data, pData, (long)size, (long)size);
        Vk.vkUnmapMemory(_device, UboMemories[frameIndex]);
    }

    public void WaitFrame(int frameIndex)
    {
        fixed (VkFence* fencePtr = &FrameFences[frameIndex])
        {
            Vk.vkWaitForFences(_device, 1, fencePtr, VkBool32.True, ulong.MaxValue);
            Vk.vkResetFences(_device, 1, fencePtr);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Vk.vkDeviceWaitIdle(_device);

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            if (FrameFences[i].Handle != 0) Vk.vkDestroyFence(_device, FrameFences[i], 0);
            if (AcquireSemaphores[i].Handle != 0) Vk.vkDestroySemaphore(_device, AcquireSemaphores[i], 0);
            if (UboBuffers[i].Handle != 0) Vk.vkDestroyBuffer(_device, UboBuffers[i], 0);
            if (UboMemories[i].Handle != 0) Vk.vkFreeMemory(_device, UboMemories[i], 0);
        }

        for (int i = 0; i < SubmitSemaphores.Length; i++)
        {
            if (SubmitSemaphores[i].Handle != 0) Vk.vkDestroySemaphore(_device, SubmitSemaphores[i], 0);
        }

        if (DescriptorPool.Handle != 0) Vk.vkDestroyDescriptorPool(_device, DescriptorPool, 0);
        if (CommandPool.Handle != 0) Vk.vkDestroyCommandPool(_device, CommandPool, 0);
    }
}
