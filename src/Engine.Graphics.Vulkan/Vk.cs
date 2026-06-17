using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal static unsafe class Vk
{
    public delegate VkResult VkCreateInstance(VkInstanceCreateInfo* pCreateInfo, nint pAllocator, VkInstance* pInstance);
    public delegate void VkDestroyInstance(VkInstance instance, nint pAllocator);
    public delegate VkResult VkEnumeratePhysicalDevices(VkInstance instance, uint* pPhysicalDeviceCount, VkPhysicalDevice* pPhysicalDevices);
    public delegate void VkGetPhysicalDeviceProperties(VkPhysicalDevice physicalDevice, VkPhysicalDeviceProperties* pProperties);
    public delegate void VkGetPhysicalDeviceMemoryProperties(VkPhysicalDevice physicalDevice, VkPhysicalDeviceMemoryProperties* pMemoryProperties);
    public delegate void VkGetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice, uint* pQueueFamilyPropertyCount, VkQueueFamilyProperties* pQueueFamilyProperties);
    public delegate VkResult VkGetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice physicalDevice, uint queueFamilyIndex, VkSurfaceKHR surface, VkBool32* pSupported);
    public delegate VkResult VkGetPhysicalDeviceSurfaceCapabilitiesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, VkSurfaceCapabilitiesKHR* pSurfaceCapabilities);
    public delegate VkResult VkGetPhysicalDeviceSurfaceFormatsKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, uint* pSurfaceFormatCount, VkSurfaceFormatKHR* pSurfaceFormats);
    public delegate VkResult VkGetPhysicalDeviceSurfacePresentModesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, uint* pPresentModeCount, VkPresentModeKHR* pPresentModes);
    public delegate VkResult VkCreateDevice(VkPhysicalDevice physicalDevice, VkDeviceCreateInfo* pCreateInfo, nint pAllocator, VkDevice* pDevice);
    public delegate void VkDestroyDevice(VkDevice device, nint pAllocator);
    public delegate void VkDestroySurfaceKHR(VkInstance instance, VkSurfaceKHR surface, nint pAllocator);
    public delegate nint VkGetDeviceProcAddr(VkDevice device, byte* pName);

    public delegate void VkGetDeviceQueue(VkDevice device, uint queueFamilyIndex, uint queueIndex, VkQueue* pQueue);
    public delegate VkResult VkCreateSwapchainKHR(VkDevice device, VkSwapchainCreateInfoKHR* pCreateInfo, nint pAllocator, VkSwapchainKHR* pSwapchain);
    public delegate void VkDestroySwapchainKHR(VkDevice device, VkSwapchainKHR swapchain, nint pAllocator);
    public delegate VkResult VkGetSwapchainImagesKHR(VkDevice device, VkSwapchainKHR swapchain, uint* pSwapchainImageCount, VkImage* pSwapchainImages);
    public delegate VkResult VkCreateImageView(VkDevice device, VkImageViewCreateInfo* pCreateInfo, nint pAllocator, VkImageView* pImageView);
    public delegate void VkDestroyImageView(VkDevice device, VkImageView imageView, nint pAllocator);
    public delegate VkResult VkCreateShaderModule(VkDevice device, VkShaderModuleCreateInfo* pCreateInfo, nint pAllocator, VkShaderModule* pShaderModule);
    public delegate void VkDestroyShaderModule(VkDevice device, VkShaderModule shaderModule, nint pAllocator);
    public delegate VkResult VkCreatePipelineLayout(VkDevice device, VkPipelineLayoutCreateInfo* pCreateInfo, nint pAllocator, VkPipelineLayout* pPipelineLayout);
    public delegate void VkDestroyPipelineLayout(VkDevice device, VkPipelineLayout pipelineLayout, nint pAllocator);
    public delegate VkResult VkCreateGraphicsPipelines(VkDevice device, nint pipelineCache, uint createInfoCount, VkGraphicsPipelineCreateInfo* pCreateInfos, nint pAllocator, VkPipeline* pPipelines);
    public delegate void VkDestroyPipeline(VkDevice device, VkPipeline pipeline, nint pAllocator);
    public delegate VkResult VkCreateCommandPool(VkDevice device, VkCommandPoolCreateInfo* pCreateInfo, nint pAllocator, VkCommandPool* pCommandPool);
    public delegate void VkDestroyCommandPool(VkDevice device, VkCommandPool commandPool, nint pAllocator);
    public delegate VkResult VkAllocateCommandBuffers(VkDevice device, VkCommandBufferAllocateInfo* pAllocateInfo, VkCommandBuffer* pCommandBuffers);
    public delegate void VkFreeCommandBuffers(VkDevice device, VkCommandPool commandPool, uint commandBufferCount, VkCommandBuffer* pCommandBuffers);
    public delegate VkResult VkBeginCommandBuffer(VkCommandBuffer commandBuffer, VkCommandBufferBeginInfo* pBeginInfo);
    public delegate VkResult VkEndCommandBuffer(VkCommandBuffer commandBuffer);
    public delegate VkResult VkResetCommandBuffer(VkCommandBuffer commandBuffer, uint flags);
    public delegate VkResult VkCreateSemaphore(VkDevice device, VkSemaphoreCreateInfo* pCreateInfo, nint pAllocator, VkSemaphore* pSemaphore);
    public delegate void VkDestroySemaphore(VkDevice device, VkSemaphore semaphore, nint pAllocator);
    public delegate VkResult VkCreateFence(VkDevice device, VkFenceCreateInfo* pCreateInfo, nint pAllocator, VkFence* pFence);
    public delegate void VkDestroyFence(VkDevice device, VkFence fence, nint pAllocator);
    public delegate VkResult VkResetFences(VkDevice device, uint fenceCount, VkFence* pFences);
    public delegate VkResult VkWaitForFences(VkDevice device, uint fenceCount, VkFence* pFences, VkBool32 waitAll, ulong timeout);
    public delegate VkResult VkGetFenceStatus(VkDevice device, VkFence fence);
    public delegate VkResult VkCreateBuffer(VkDevice device, VkBufferCreateInfo* pCreateInfo, nint pAllocator, VkBuffer* pBuffer);
    public delegate void VkDestroyBuffer(VkDevice device, VkBuffer buffer, nint pAllocator);
    public delegate VkResult VkAllocateMemory(VkDevice device, VkMemoryAllocateInfo* pAllocateInfo, nint pAllocator, VkDeviceMemory* pMemory);
    public delegate void VkFreeMemory(VkDevice device, VkDeviceMemory memory, nint pAllocator);
    public delegate VkResult VkBindBufferMemory(VkDevice device, VkBuffer buffer, VkDeviceMemory memory, ulong memoryOffset);
    public delegate void VkGetBufferMemoryRequirements(VkDevice device, VkBuffer buffer, VkMemoryRequirements* pMemoryRequirements);
    public delegate VkResult VkMapMemory(VkDevice device, VkDeviceMemory memory, ulong offset, ulong size, uint flags, void** ppData);
    public delegate void VkUnmapMemory(VkDevice device, VkDeviceMemory memory);
    public delegate void VkCmdBindPipeline(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipeline pipeline);
    public delegate void VkCmdSetViewport(VkCommandBuffer commandBuffer, uint firstViewport, uint viewportCount, VkViewport* pViewports);
    public delegate void VkCmdSetScissor(VkCommandBuffer commandBuffer, uint firstScissor, uint scissorCount, VkRect2D* pScissors);
    public delegate void VkCmdBindVertexBuffers(VkCommandBuffer commandBuffer, uint firstBinding, uint bindingCount, VkBuffer* pBuffers, ulong* pOffsets);
    public delegate void VkCmdDraw(VkCommandBuffer commandBuffer, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);
    public delegate void VkCmdDrawIndexed(VkCommandBuffer commandBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance);
    public delegate void VkCmdBindIndexBuffer(VkCommandBuffer commandBuffer, VkBuffer buffer, ulong offset, int indexType);
    public delegate void VkCmdBeginRendering(VkCommandBuffer commandBuffer, VkRenderingInfo* pRenderingInfo);
    public delegate void VkCmdEndRendering(VkCommandBuffer commandBuffer);
    public delegate void VkCmdPipelineBarrier2(VkCommandBuffer commandBuffer, VkDependencyInfo* pDependencyInfo);
    public delegate void VkCmdCopyBuffer(VkCommandBuffer commandBuffer, VkBuffer srcBuffer, VkBuffer dstBuffer, uint regionCount, VkBufferCopy* pRegions);
    public delegate void VkCmdPushConstants(VkCommandBuffer commandBuffer, VkPipelineLayout layout, VkShaderStageFlags stageFlags, uint offset, uint size, void* pValues);
    public delegate void VkCmdBindDescriptorSets(VkCommandBuffer commandBuffer, VkPipelineBindPoint pipelineBindPoint, VkPipelineLayout layout, uint firstSet, uint descriptorSetCount, VkDescriptorSet* pDescriptorSets, uint dynamicOffsetCount, uint* pDynamicOffsets);
    public delegate VkResult VkCreateDescriptorSetLayout(VkDevice device, VkDescriptorSetLayoutCreateInfo* pCreateInfo, nint pAllocator, VkDescriptorSetLayout* pSetLayout);
    public delegate void VkDestroyDescriptorSetLayout(VkDevice device, VkDescriptorSetLayout descriptorSetLayout, nint pAllocator);
    public delegate VkResult VkCreateDescriptorPool(VkDevice device, VkDescriptorPoolCreateInfo* pCreateInfo, nint pAllocator, VkDescriptorPool* pDescriptorPool);
    public delegate void VkDestroyDescriptorPool(VkDevice device, VkDescriptorPool descriptorPool, nint pAllocator);
    public delegate VkResult VkAllocateDescriptorSets(VkDevice device, VkDescriptorSetAllocateInfo* pAllocateInfo, VkDescriptorSet* pDescriptorSets);
    public delegate void VkUpdateDescriptorSets(VkDevice device, uint descriptorWriteCount, VkWriteDescriptorSet* pDescriptorWrites, uint descriptorCopyCount, nint pDescriptorCopies);
    public delegate VkResult VkAcquireNextImageKHR(VkDevice device, VkSwapchainKHR swapchain, ulong timeout, VkSemaphore semaphore, VkFence fence, uint* pImageIndex);
    public delegate VkResult VkQueueSubmit2(VkQueue queue, uint submitCount, VkSubmitInfo2* pSubmits, VkFence fence);
    public delegate VkResult VkQueuePresentKHR(VkQueue queue, VkPresentInfoKHR* pPresentInfo);
    public delegate VkResult VkDeviceWaitIdle(VkDevice device);
    public delegate VkResult VkQueueWaitIdle(VkQueue queue);

    public delegate VkResult VkCreateDebugUtilsMessengerEXT(VkInstance instance, VkDebugUtilsMessengerCreateInfoEXT* pCreateInfo, nint pAllocator, VkDebugUtilsMessengerEXT* pMessenger);
    public delegate void VkDestroyDebugUtilsMessengerEXT(VkInstance instance, VkDebugUtilsMessengerEXT messenger, nint pAllocator);

    public static VkCreateInstance vkCreateInstance;
    public static VkDestroyInstance vkDestroyInstance;
    public static VkEnumeratePhysicalDevices vkEnumeratePhysicalDevices;
    public static VkGetPhysicalDeviceProperties vkGetPhysicalDeviceProperties;
    public static VkGetPhysicalDeviceMemoryProperties vkGetPhysicalDeviceMemoryProperties;
    public static VkGetPhysicalDeviceQueueFamilyProperties vkGetPhysicalDeviceQueueFamilyProperties;
    public static VkGetPhysicalDeviceSurfaceSupportKHR vkGetPhysicalDeviceSurfaceSupportKHR;
    public static VkGetPhysicalDeviceSurfaceCapabilitiesKHR vkGetPhysicalDeviceSurfaceCapabilitiesKHR;
    public static VkGetPhysicalDeviceSurfaceFormatsKHR vkGetPhysicalDeviceSurfaceFormatsKHR;
    public static VkGetPhysicalDeviceSurfacePresentModesKHR vkGetPhysicalDeviceSurfacePresentModesKHR;
    public static VkCreateDevice vkCreateDevice;
    public static VkDestroyDevice vkDestroyDevice;
    public static VkDestroySurfaceKHR vkDestroySurfaceKHR;
    public static VkGetDeviceProcAddr vkGetDeviceProcAddr;

    public static VkGetDeviceQueue vkGetDeviceQueue;
    public static VkCreateSwapchainKHR vkCreateSwapchainKHR;
    public static VkDestroySwapchainKHR vkDestroySwapchainKHR;
    public static VkGetSwapchainImagesKHR vkGetSwapchainImagesKHR;
    public static VkCreateImageView vkCreateImageView;
    public static VkDestroyImageView vkDestroyImageView;
    public static VkCreateShaderModule vkCreateShaderModule;
    public static VkDestroyShaderModule vkDestroyShaderModule;
    public static VkCreatePipelineLayout vkCreatePipelineLayout;
    public static VkDestroyPipelineLayout vkDestroyPipelineLayout;
    public static VkCreateGraphicsPipelines vkCreateGraphicsPipelines;
    public static VkDestroyPipeline vkDestroyPipeline;
    public static VkCreateCommandPool vkCreateCommandPool;
    public static VkDestroyCommandPool vkDestroyCommandPool;
    public static VkAllocateCommandBuffers vkAllocateCommandBuffers;
    public static VkFreeCommandBuffers vkFreeCommandBuffers;
    public static VkBeginCommandBuffer vkBeginCommandBuffer;
    public static VkEndCommandBuffer vkEndCommandBuffer;
    public static VkResetCommandBuffer vkResetCommandBuffer;
    public static VkCreateSemaphore vkCreateSemaphore;
    public static VkDestroySemaphore vkDestroySemaphore;
    public static VkCreateFence vkCreateFence;
    public static VkDestroyFence vkDestroyFence;
    public static VkResetFences vkResetFences;
    public static VkWaitForFences vkWaitForFences;
    public static VkGetFenceStatus vkGetFenceStatus;
    public static VkCreateBuffer vkCreateBuffer;
    public static VkDestroyBuffer vkDestroyBuffer;
    public static VkAllocateMemory vkAllocateMemory;
    public static VkFreeMemory vkFreeMemory;
    public static VkBindBufferMemory vkBindBufferMemory;
    public static VkGetBufferMemoryRequirements vkGetBufferMemoryRequirements;
    public static VkMapMemory vkMapMemory;
    public static VkUnmapMemory vkUnmapMemory;
    public static VkCmdBindPipeline vkCmdBindPipeline;
    public static VkCmdSetViewport vkCmdSetViewport;
    public static VkCmdSetScissor vkCmdSetScissor;
    public static VkCmdBindVertexBuffers vkCmdBindVertexBuffers;
    public static VkCmdDraw vkCmdDraw;
    public static VkCmdDrawIndexed vkCmdDrawIndexed;
    public static VkCmdBindIndexBuffer vkCmdBindIndexBuffer;
    public static VkCmdBeginRendering vkCmdBeginRendering;
    public static VkCmdEndRendering vkCmdEndRendering;
    public static VkCmdPipelineBarrier2 vkCmdPipelineBarrier2;
    public static VkCmdCopyBuffer vkCmdCopyBuffer;
    public static VkCmdPushConstants vkCmdPushConstants;
    public static VkCmdBindDescriptorSets vkCmdBindDescriptorSets;
    public static VkCreateDescriptorSetLayout vkCreateDescriptorSetLayout;
    public static VkDestroyDescriptorSetLayout vkDestroyDescriptorSetLayout;
    public static VkCreateDescriptorPool vkCreateDescriptorPool;
    public static VkDestroyDescriptorPool vkDestroyDescriptorPool;
    public static VkAllocateDescriptorSets vkAllocateDescriptorSets;
    public static VkUpdateDescriptorSets vkUpdateDescriptorSets;
    public static VkAcquireNextImageKHR vkAcquireNextImageKHR;
    public static VkQueueSubmit2 vkQueueSubmit2;
    public static VkQueuePresentKHR vkQueuePresentKHR;
    public static VkDeviceWaitIdle vkDeviceWaitIdle;
    public static VkQueueWaitIdle vkQueueWaitIdle;

    public static VkCreateDebugUtilsMessengerEXT vkCreateDebugUtilsMessengerEXT;
    public static VkDestroyDebugUtilsMessengerEXT vkDestroyDebugUtilsMessengerEXT;

    public static void LoadInstanceFunctions(VkInstance instance)
    {
        var p = instance.Handle;
        vkDestroyInstance = Load<VkDestroyInstance>(p, "vkDestroyInstance");
        vkEnumeratePhysicalDevices = Load<VkEnumeratePhysicalDevices>(p, "vkEnumeratePhysicalDevices");
        vkGetPhysicalDeviceProperties = Load<VkGetPhysicalDeviceProperties>(p, "vkGetPhysicalDeviceProperties");
        vkGetPhysicalDeviceMemoryProperties = Load<VkGetPhysicalDeviceMemoryProperties>(p, "vkGetPhysicalDeviceMemoryProperties");
        vkGetPhysicalDeviceQueueFamilyProperties = Load<VkGetPhysicalDeviceQueueFamilyProperties>(p, "vkGetPhysicalDeviceQueueFamilyProperties");
        vkGetPhysicalDeviceSurfaceSupportKHR = Load<VkGetPhysicalDeviceSurfaceSupportKHR>(p, "vkGetPhysicalDeviceSurfaceSupportKHR");
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR = Load<VkGetPhysicalDeviceSurfaceCapabilitiesKHR>(p, "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        vkGetPhysicalDeviceSurfaceFormatsKHR = Load<VkGetPhysicalDeviceSurfaceFormatsKHR>(p, "vkGetPhysicalDeviceSurfaceFormatsKHR");
        vkGetPhysicalDeviceSurfacePresentModesKHR = Load<VkGetPhysicalDeviceSurfacePresentModesKHR>(p, "vkGetPhysicalDeviceSurfacePresentModesKHR");
        vkCreateDevice = Load<VkCreateDevice>(p, "vkCreateDevice");
        vkDestroyDevice = Load<VkDestroyDevice>(p, "vkDestroyDevice");
        vkDestroySurfaceKHR = Load<VkDestroySurfaceKHR>(p, "vkDestroySurfaceKHR");
        vkGetDeviceProcAddr = Load<VkGetDeviceProcAddr>(p, "vkGetDeviceProcAddr");
        TryLoadDebugUtils(p);
    }

    public static void LoadDeviceFunctions(VkDevice device)
    {
        var p = device.Handle;
        vkGetDeviceQueue = LoadDev<VkGetDeviceQueue>(p, "vkGetDeviceQueue");
        vkCreateSwapchainKHR = LoadDev<VkCreateSwapchainKHR>(p, "vkCreateSwapchainKHR");
        vkDestroySwapchainKHR = LoadDev<VkDestroySwapchainKHR>(p, "vkDestroySwapchainKHR");
        vkGetSwapchainImagesKHR = LoadDev<VkGetSwapchainImagesKHR>(p, "vkGetSwapchainImagesKHR");
        vkCreateImageView = LoadDev<VkCreateImageView>(p, "vkCreateImageView");
        vkDestroyImageView = LoadDev<VkDestroyImageView>(p, "vkDestroyImageView");
        vkCreateShaderModule = LoadDev<VkCreateShaderModule>(p, "vkCreateShaderModule");
        vkDestroyShaderModule = LoadDev<VkDestroyShaderModule>(p, "vkDestroyShaderModule");
        vkCreatePipelineLayout = LoadDev<VkCreatePipelineLayout>(p, "vkCreatePipelineLayout");
        vkDestroyPipelineLayout = LoadDev<VkDestroyPipelineLayout>(p, "vkDestroyPipelineLayout");
        vkCreateGraphicsPipelines = LoadDev<VkCreateGraphicsPipelines>(p, "vkCreateGraphicsPipelines");
        vkDestroyPipeline = LoadDev<VkDestroyPipeline>(p, "vkDestroyPipeline");
        vkCreateCommandPool = LoadDev<VkCreateCommandPool>(p, "vkCreateCommandPool");
        vkDestroyCommandPool = LoadDev<VkDestroyCommandPool>(p, "vkDestroyCommandPool");
        vkAllocateCommandBuffers = LoadDev<VkAllocateCommandBuffers>(p, "vkAllocateCommandBuffers");
        vkFreeCommandBuffers = LoadDev<VkFreeCommandBuffers>(p, "vkFreeCommandBuffers");
        vkBeginCommandBuffer = LoadDev<VkBeginCommandBuffer>(p, "vkBeginCommandBuffer");
        vkEndCommandBuffer = LoadDev<VkEndCommandBuffer>(p, "vkEndCommandBuffer");
        vkResetCommandBuffer = LoadDev<VkResetCommandBuffer>(p, "vkResetCommandBuffer");
        vkCreateSemaphore = LoadDev<VkCreateSemaphore>(p, "vkCreateSemaphore");
        vkDestroySemaphore = LoadDev<VkDestroySemaphore>(p, "vkDestroySemaphore");
        vkCreateFence = LoadDev<VkCreateFence>(p, "vkCreateFence");
        vkDestroyFence = LoadDev<VkDestroyFence>(p, "vkDestroyFence");
        vkResetFences = LoadDev<VkResetFences>(p, "vkResetFences");
        vkWaitForFences = LoadDev<VkWaitForFences>(p, "vkWaitForFences");
        vkGetFenceStatus = LoadDev<VkGetFenceStatus>(p, "vkGetFenceStatus");
        vkCreateBuffer = LoadDev<VkCreateBuffer>(p, "vkCreateBuffer");
        vkDestroyBuffer = LoadDev<VkDestroyBuffer>(p, "vkDestroyBuffer");
        vkAllocateMemory = LoadDev<VkAllocateMemory>(p, "vkAllocateMemory");
        vkFreeMemory = LoadDev<VkFreeMemory>(p, "vkFreeMemory");
        vkBindBufferMemory = LoadDev<VkBindBufferMemory>(p, "vkBindBufferMemory");
        vkGetBufferMemoryRequirements = LoadDev<VkGetBufferMemoryRequirements>(p, "vkGetBufferMemoryRequirements");
        vkMapMemory = LoadDev<VkMapMemory>(p, "vkMapMemory");
        vkUnmapMemory = LoadDev<VkUnmapMemory>(p, "vkUnmapMemory");
        vkCmdBindPipeline = LoadDev<VkCmdBindPipeline>(p, "vkCmdBindPipeline");
        vkCmdSetViewport = LoadDev<VkCmdSetViewport>(p, "vkCmdSetViewport");
        vkCmdSetScissor = LoadDev<VkCmdSetScissor>(p, "vkCmdSetScissor");
        vkCmdBindVertexBuffers = LoadDev<VkCmdBindVertexBuffers>(p, "vkCmdBindVertexBuffers");
        vkCmdDraw = LoadDev<VkCmdDraw>(p, "vkCmdDraw");
        vkCmdDrawIndexed = LoadDev<VkCmdDrawIndexed>(p, "vkCmdDrawIndexed");
        vkCmdBindIndexBuffer = LoadDev<VkCmdBindIndexBuffer>(p, "vkCmdBindIndexBuffer");
        vkCmdBeginRendering = LoadDev<VkCmdBeginRendering>(p, "vkCmdBeginRendering");
        vkCmdEndRendering = LoadDev<VkCmdEndRendering>(p, "vkCmdEndRendering");
        vkCmdPipelineBarrier2 = LoadDev<VkCmdPipelineBarrier2>(p, "vkCmdPipelineBarrier2");
        vkCmdCopyBuffer = LoadDev<VkCmdCopyBuffer>(p, "vkCmdCopyBuffer");
        vkCmdPushConstants = LoadDev<VkCmdPushConstants>(p, "vkCmdPushConstants");
        vkCmdBindDescriptorSets = LoadDev<VkCmdBindDescriptorSets>(p, "vkCmdBindDescriptorSets");
        vkCreateDescriptorSetLayout = LoadDev<VkCreateDescriptorSetLayout>(p, "vkCreateDescriptorSetLayout");
        vkDestroyDescriptorSetLayout = LoadDev<VkDestroyDescriptorSetLayout>(p, "vkDestroyDescriptorSetLayout");
        vkCreateDescriptorPool = LoadDev<VkCreateDescriptorPool>(p, "vkCreateDescriptorPool");
        vkDestroyDescriptorPool = LoadDev<VkDestroyDescriptorPool>(p, "vkDestroyDescriptorPool");
        vkAllocateDescriptorSets = LoadDev<VkAllocateDescriptorSets>(p, "vkAllocateDescriptorSets");
        vkUpdateDescriptorSets = LoadDev<VkUpdateDescriptorSets>(p, "vkUpdateDescriptorSets");
        vkAcquireNextImageKHR = LoadDev<VkAcquireNextImageKHR>(p, "vkAcquireNextImageKHR");
        vkQueueSubmit2 = LoadDev<VkQueueSubmit2>(p, "vkQueueSubmit2");
        vkQueuePresentKHR = LoadDev<VkQueuePresentKHR>(p, "vkQueuePresentKHR");
        vkDeviceWaitIdle = LoadDev<VkDeviceWaitIdle>(p, "vkDeviceWaitIdle");
        vkQueueWaitIdle = LoadDev<VkQueueWaitIdle>(p, "vkQueueWaitIdle");
    }

    private static void TryLoadDebugUtils(nint instance)
    {
        try
        {
            vkCreateDebugUtilsMessengerEXT = Load<VkCreateDebugUtilsMessengerEXT>(instance, "vkCreateDebugUtilsMessengerEXT");
            vkDestroyDebugUtilsMessengerEXT = Load<VkDestroyDebugUtilsMessengerEXT>(instance, "vkDestroyDebugUtilsMessengerEXT");
        }
        catch
        {
            vkCreateDebugUtilsMessengerEXT = null!;
            vkDestroyDebugUtilsMessengerEXT = null!;
        }
    }

    private static T Load<T>(nint instance, string name) where T : Delegate
    {
        fixed (byte* pName = VulkanString.ToUtf8Terminated(name))
        {
            var addr = VulkanNative.vkGetInstanceProcAddr(instance, pName);
            if (addr == 0)
                throw new EntryPointNotFoundException($"vkGetInstanceProcAddr returned null for: {name}");
            return Marshal.GetDelegateForFunctionPointer<T>(addr);
        }
    }

    private static T LoadDev<T>(nint device, string name) where T : Delegate
    {
        fixed (byte* pName = VulkanString.ToUtf8Terminated(name))
        {
            var addr = vkGetDeviceProcAddr(new VkDevice { Handle = device }, pName);
            if (addr == 0)
            {
                addr = VulkanNative.vkGetInstanceProcAddr(0, pName);
                if (addr == 0)
                    throw new EntryPointNotFoundException($"vkGetDeviceProcAddr returned null for: {name}");
            }
            return Marshal.GetDelegateForFunctionPointer<T>(addr);
        }
    }
}
