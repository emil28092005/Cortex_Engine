namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanFrameResources : IDisposable
{
    public const int MaxFramesInFlight = 2;

    public VkCommandPool CommandPool;
    public VkCommandBuffer[] CommandBuffers = new VkCommandBuffer[MaxFramesInFlight];
    public VkFence[] FrameFences = new VkFence[MaxFramesInFlight];
    public VkSemaphore[] AcquireSemaphores = new VkSemaphore[MaxFramesInFlight];
    public VkSemaphore[] SubmitSemaphores = Array.Empty<VkSemaphore>();

    private readonly VkDevice _device;
    private bool _disposed;

    public VulkanFrameResources(VkDevice device, uint queueFamilyIndex, uint swapchainImageCount)
    {
        _device = device;

        var poolInfo = new VkCommandPoolCreateInfo
        {
            sType = VkStructureType.CommandPoolCreateInfo,
            flags = VkCommandPoolCreateFlags.ResetCommandBuffer,
            queueFamilyIndex = queueFamilyIndex,
        };

        fixed (VkCommandPool* poolPtr = &CommandPool)
        {
            var result = Vk.vkCreateCommandPool(_device, &poolInfo, 0, poolPtr);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateCommandPool failed: {result}");
        }

        var allocInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.CommandBufferAllocateInfo,
            commandPool = CommandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = MaxFramesInFlight,
        };

        fixed (VkCommandBuffer* cmdPtr = CommandBuffers)
        {
            var result = Vk.vkAllocateCommandBuffers(_device, &allocInfo, cmdPtr);
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
            var result = Vk.vkCreateFence(_device, &fenceInfo, 0, &fence);
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
            var result = Vk.vkCreateSemaphore(_device, &semInfo, 0, &sem);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateSemaphore (submit) failed: {result}");
            SubmitSemaphores[i] = sem;
        }

        Console.WriteLine($"[Vulkan] Frame resources: {MaxFramesInFlight} frames in flight, {swapchainImageCount} submit semaphores");
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
        }

        for (int i = 0; i < SubmitSemaphores.Length; i++)
        {
            if (SubmitSemaphores[i].Handle != 0) Vk.vkDestroySemaphore(_device, SubmitSemaphores[i], 0);
        }

        if (CommandPool.Handle != 0) Vk.vkDestroyCommandPool(_device, CommandPool, 0);
    }
}
