using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal static unsafe class Vk
{
    public static VkInstance Instance;
    public static VkDevice Device;

    public static PFN_vkCreateInstance vkCreateInstance;
    public static PFN_vkDestroyInstance vkDestroyInstance;
    public static PFN_vkEnumeratePhysicalDevices vkEnumeratePhysicalDevices;
    public static PFN_vkGetPhysicalDeviceProperties vkGetPhysicalDeviceProperties;
    public static PFN_vkGetPhysicalDeviceQueueFamilyProperties vkGetPhysicalDeviceQueueFamilyProperties;
    public static PFN_vkGetPhysicalDeviceMemoryProperties vkGetPhysicalDeviceMemoryProperties;
    public static PFN_vkEnumerateDeviceExtensionProperties vkEnumerateDeviceExtensionProperties;
    public static PFN_vkCreateDevice vkCreateDevice;
    public static PFN_vkDestroyDevice vkDestroyDevice;
    public static PFN_vkGetDeviceQueue vkGetDeviceQueue;
    public static PFN_vkCreateSwapchainKHR vkCreateSwapchainKHR;
    public static PFN_vkDestroySwapchainKHR vkDestroySwapchainKHR;
    public static PFN_vkGetSwapchainImagesKHR vkGetSwapchainImagesKHR;
    public static PFN_vkCreateImageView vkCreateImageView;
    public static PFN_vkDestroyImageView vkDestroyImageView;
    public static PFN_vkCreateImage vkCreateImage;
    public static PFN_vkDestroyImage vkDestroyImage;
    public static PFN_vkGetImageMemoryRequirements vkGetImageMemoryRequirements;
    public static PFN_vkBindImageMemory vkBindImageMemory;
    public static PFN_vkCreateRenderPass vkCreateRenderPass;
    public static PFN_vkDestroyRenderPass vkDestroyRenderPass;
    public static PFN_vkCreateFramebuffer vkCreateFramebuffer;
    public static PFN_vkDestroyFramebuffer vkDestroyFramebuffer;
    public static PFN_vkCreateShaderModule vkCreateShaderModule;
    public static PFN_vkDestroyShaderModule vkDestroyShaderModule;
    public static PFN_vkCreateDescriptorSetLayout vkCreateDescriptorSetLayout;
    public static PFN_vkDestroyDescriptorSetLayout vkDestroyDescriptorSetLayout;
    public static PFN_vkCreatePipelineLayout vkCreatePipelineLayout;
    public static PFN_vkDestroyPipelineLayout vkDestroyPipelineLayout;
    public static PFN_vkCreateGraphicsPipelines vkCreateGraphicsPipelines;
    public static PFN_vkDestroyPipeline vkDestroyPipeline;
    public static PFN_vkCreateDescriptorPool vkCreateDescriptorPool;
    public static PFN_vkDestroyDescriptorPool vkDestroyDescriptorPool;
    public static PFN_vkAllocateDescriptorSets vkAllocateDescriptorSets;
    public static PFN_vkUpdateDescriptorSets vkUpdateDescriptorSets;
    public static PFN_vkCreateBuffer vkCreateBuffer;
    public static PFN_vkDestroyBuffer vkDestroyBuffer;
    public static PFN_vkGetBufferMemoryRequirements vkGetBufferMemoryRequirements;
    public static PFN_vkBindBufferMemory vkBindBufferMemory;
    public static PFN_vkAllocateMemory vkAllocateMemory;
    public static PFN_vkFreeMemory vkFreeMemory;
    public static PFN_vkMapMemory vkMapMemory;
    public static PFN_vkUnmapMemory vkUnmapMemory;
    public static PFN_vkCreateCommandPool vkCreateCommandPool;
    public static PFN_vkDestroyCommandPool vkDestroyCommandPool;
    public static PFN_vkAllocateCommandBuffers vkAllocateCommandBuffers;
    public static PFN_vkFreeCommandBuffers vkFreeCommandBuffers;
    public static PFN_vkBeginCommandBuffer vkBeginCommandBuffer;
    public static PFN_vkEndCommandBuffer vkEndCommandBuffer;
    public static PFN_vkResetCommandBuffer vkResetCommandBuffer;
    public static PFN_vkQueueSubmit vkQueueSubmit;
    public static PFN_vkQueueWaitIdle vkQueueWaitIdle;
    public static PFN_vkQueuePresentKHR vkQueuePresentKHR;
    public static PFN_vkAcquireNextImageKHR vkAcquireNextImageKHR;
    public static PFN_vkCreateSemaphore vkCreateSemaphore;
    public static PFN_vkDestroySemaphore vkDestroySemaphore;
    public static PFN_vkCreateFence vkCreateFence;
    public static PFN_vkDestroyFence vkDestroyFence;
    public static PFN_vkWaitForFences vkWaitForFences;
    public static PFN_vkResetFences vkResetFences;
    public static PFN_vkCmdBeginRenderPass vkCmdBeginRenderPass;
    public static PFN_vkCmdEndRenderPass vkCmdEndRenderPass;
    public static PFN_vkCmdBindPipeline vkCmdBindPipeline;
    public static PFN_vkCmdBindDescriptorSets vkCmdBindDescriptorSets;
    public static PFN_vkCmdBindVertexBuffers vkCmdBindVertexBuffers;
    public static PFN_vkCmdBindIndexBuffer vkCmdBindIndexBuffer;
    public static PFN_vkCmdDrawIndexed vkCmdDrawIndexed;
    public static PFN_vkCmdDraw vkCmdDraw;
    public static PFN_vkCmdSetViewport vkCmdSetViewport;
    public static PFN_vkCmdSetScissor vkCmdSetScissor;
    public static PFN_vkCmdPipelineBarrier vkCmdPipelineBarrier;
    public static PFN_vkCmdCopyBuffer vkCmdCopyBuffer;
    public static PFN_vkCmdCopyBufferToImage vkCmdCopyBufferToImage;
    public static PFN_vkCmdCopyImageToBuffer vkCmdCopyImageToBuffer;
    public static PFN_vkCmdClearColorImage vkCmdClearColorImage;
    public static PFN_vkCmdPushConstants vkCmdPushConstants;
    public static PFN_vkCreateSampler vkCreateSampler;
    public static PFN_vkDestroySampler vkDestroySampler;
    public static PFN_vkGetPhysicalDeviceSurfaceSupportKHR vkGetPhysicalDeviceSurfaceSupportKHR;
    public static PFN_vkGetPhysicalDeviceSurfaceCapabilitiesKHR vkGetPhysicalDeviceSurfaceCapabilitiesKHR;
    public static PFN_vkGetPhysicalDeviceSurfaceFormatsKHR vkGetPhysicalDeviceSurfaceFormatsKHR;
    public static PFN_vkGetPhysicalDeviceSurfacePresentModesKHR vkGetPhysicalDeviceSurfacePresentModesKHR;
    public static PFN_vkDestroySurfaceKHR vkDestroySurfaceKHR;

    public static void LoadGlobalFunctions()
    {
        VulkanNative.LoadLibrary();
        var libHandle = VulkanNative.LoadLibrary();

        var createInstancePtr = NativeLibrary.GetExport(libHandle, "vkCreateInstance");
        vkCreateInstance = Marshal.GetDelegateForFunctionPointer<PFN_vkCreateInstance>(createInstancePtr);
    }

    public static void LoadInstanceFunctions(VkInstance instance)
    {
        Instance = instance;

        vkDestroyInstance = VulkanNative.LoadInstanceFunction<PFN_vkDestroyInstance>(instance, "vkDestroyInstance");
        vkEnumeratePhysicalDevices = VulkanNative.LoadInstanceFunction<PFN_vkEnumeratePhysicalDevices>(instance, "vkEnumeratePhysicalDevices");
        vkGetPhysicalDeviceProperties = VulkanNative.LoadInstanceFunction<PFN_vkGetPhysicalDeviceProperties>(instance, "vkGetPhysicalDeviceProperties");
        vkGetPhysicalDeviceQueueFamilyProperties = VulkanNative.LoadInstanceFunction<PFN_vkGetPhysicalDeviceQueueFamilyProperties>(instance, "vkGetPhysicalDeviceQueueFamilyProperties");
        vkGetPhysicalDeviceMemoryProperties = VulkanNative.LoadInstanceFunction<PFN_vkGetPhysicalDeviceMemoryProperties>(instance, "vkGetPhysicalDeviceMemoryProperties");
        vkEnumerateDeviceExtensionProperties = VulkanNative.LoadInstanceFunction<PFN_vkEnumerateDeviceExtensionProperties>(instance, "vkEnumerateDeviceExtensionProperties");
        vkCreateDevice = VulkanNative.LoadInstanceFunction<PFN_vkCreateDevice>(instance, "vkCreateDevice");
        vkGetPhysicalDeviceSurfaceSupportKHR = VulkanNative.LoadInstanceFunction<PFN_vkGetPhysicalDeviceSurfaceSupportKHR>(instance, "vkGetPhysicalDeviceSurfaceSupportKHR");
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR = VulkanNative.LoadInstanceFunction<PFN_vkGetPhysicalDeviceSurfaceCapabilitiesKHR>(instance, "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        vkGetPhysicalDeviceSurfaceFormatsKHR = VulkanNative.LoadInstanceFunction<PFN_vkGetPhysicalDeviceSurfaceFormatsKHR>(instance, "vkGetPhysicalDeviceSurfaceFormatsKHR");
        vkGetPhysicalDeviceSurfacePresentModesKHR = VulkanNative.LoadInstanceFunction<PFN_vkGetPhysicalDeviceSurfacePresentModesKHR>(instance, "vkGetPhysicalDeviceSurfacePresentModesKHR");
        vkDestroySurfaceKHR = VulkanNative.LoadInstanceFunction<PFN_vkDestroySurfaceKHR>(instance, "vkDestroySurfaceKHR");
    }

    public static void LoadDeviceFunctions(VkDevice device)
    {
        Device = device;

        vkDestroyDevice = VulkanNative.LoadDeviceFunction<PFN_vkDestroyDevice>(device, "vkDestroyDevice");
        vkGetDeviceQueue = VulkanNative.LoadDeviceFunction<PFN_vkGetDeviceQueue>(device, "vkGetDeviceQueue");
        vkCreateSwapchainKHR = VulkanNative.LoadDeviceFunction<PFN_vkCreateSwapchainKHR>(device, "vkCreateSwapchainKHR");
        vkDestroySwapchainKHR = VulkanNative.LoadDeviceFunction<PFN_vkDestroySwapchainKHR>(device, "vkDestroySwapchainKHR");
        vkGetSwapchainImagesKHR = VulkanNative.LoadDeviceFunction<PFN_vkGetSwapchainImagesKHR>(device, "vkGetSwapchainImagesKHR");
        vkCreateImageView = VulkanNative.LoadDeviceFunction<PFN_vkCreateImageView>(device, "vkCreateImageView");
        vkDestroyImageView = VulkanNative.LoadDeviceFunction<PFN_vkDestroyImageView>(device, "vkDestroyImageView");
        vkCreateImage = VulkanNative.LoadDeviceFunction<PFN_vkCreateImage>(device, "vkCreateImage");
        vkDestroyImage = VulkanNative.LoadDeviceFunction<PFN_vkDestroyImage>(device, "vkDestroyImage");
        vkGetImageMemoryRequirements = VulkanNative.LoadDeviceFunction<PFN_vkGetImageMemoryRequirements>(device, "vkGetImageMemoryRequirements");
        vkBindImageMemory = VulkanNative.LoadDeviceFunction<PFN_vkBindImageMemory>(device, "vkBindImageMemory");
        vkCreateRenderPass = VulkanNative.LoadDeviceFunction<PFN_vkCreateRenderPass>(device, "vkCreateRenderPass");
        vkDestroyRenderPass = VulkanNative.LoadDeviceFunction<PFN_vkDestroyRenderPass>(device, "vkDestroyRenderPass");
        vkCreateFramebuffer = VulkanNative.LoadDeviceFunction<PFN_vkCreateFramebuffer>(device, "vkCreateFramebuffer");
        vkDestroyFramebuffer = VulkanNative.LoadDeviceFunction<PFN_vkDestroyFramebuffer>(device, "vkDestroyFramebuffer");
        vkCreateShaderModule = VulkanNative.LoadDeviceFunction<PFN_vkCreateShaderModule>(device, "vkCreateShaderModule");
        vkDestroyShaderModule = VulkanNative.LoadDeviceFunction<PFN_vkDestroyShaderModule>(device, "vkDestroyShaderModule");
        vkCreateDescriptorSetLayout = VulkanNative.LoadDeviceFunction<PFN_vkCreateDescriptorSetLayout>(device, "vkCreateDescriptorSetLayout");
        vkDestroyDescriptorSetLayout = VulkanNative.LoadDeviceFunction<PFN_vkDestroyDescriptorSetLayout>(device, "vkDestroyDescriptorSetLayout");
        vkCreatePipelineLayout = VulkanNative.LoadDeviceFunction<PFN_vkCreatePipelineLayout>(device, "vkCreatePipelineLayout");
        vkDestroyPipelineLayout = VulkanNative.LoadDeviceFunction<PFN_vkDestroyPipelineLayout>(device, "vkDestroyPipelineLayout");
        vkCreateGraphicsPipelines = VulkanNative.LoadDeviceFunction<PFN_vkCreateGraphicsPipelines>(device, "vkCreateGraphicsPipelines");
        vkDestroyPipeline = VulkanNative.LoadDeviceFunction<PFN_vkDestroyPipeline>(device, "vkDestroyPipeline");
        vkCreateDescriptorPool = VulkanNative.LoadDeviceFunction<PFN_vkCreateDescriptorPool>(device, "vkCreateDescriptorPool");
        vkDestroyDescriptorPool = VulkanNative.LoadDeviceFunction<PFN_vkDestroyDescriptorPool>(device, "vkDestroyDescriptorPool");
        vkAllocateDescriptorSets = VulkanNative.LoadDeviceFunction<PFN_vkAllocateDescriptorSets>(device, "vkAllocateDescriptorSets");
        vkUpdateDescriptorSets = VulkanNative.LoadDeviceFunction<PFN_vkUpdateDescriptorSets>(device, "vkUpdateDescriptorSets");
        vkCreateBuffer = VulkanNative.LoadDeviceFunction<PFN_vkCreateBuffer>(device, "vkCreateBuffer");
        vkDestroyBuffer = VulkanNative.LoadDeviceFunction<PFN_vkDestroyBuffer>(device, "vkDestroyBuffer");
        vkGetBufferMemoryRequirements = VulkanNative.LoadDeviceFunction<PFN_vkGetBufferMemoryRequirements>(device, "vkGetBufferMemoryRequirements");
        vkBindBufferMemory = VulkanNative.LoadDeviceFunction<PFN_vkBindBufferMemory>(device, "vkBindBufferMemory");
        vkAllocateMemory = VulkanNative.LoadDeviceFunction<PFN_vkAllocateMemory>(device, "vkAllocateMemory");
        vkFreeMemory = VulkanNative.LoadDeviceFunction<PFN_vkFreeMemory>(device, "vkFreeMemory");
        vkMapMemory = VulkanNative.LoadDeviceFunction<PFN_vkMapMemory>(device, "vkMapMemory");
        vkUnmapMemory = VulkanNative.LoadDeviceFunction<PFN_vkUnmapMemory>(device, "vkUnmapMemory");
        vkCreateCommandPool = VulkanNative.LoadDeviceFunction<PFN_vkCreateCommandPool>(device, "vkCreateCommandPool");
        vkDestroyCommandPool = VulkanNative.LoadDeviceFunction<PFN_vkDestroyCommandPool>(device, "vkDestroyCommandPool");
        vkAllocateCommandBuffers = VulkanNative.LoadDeviceFunction<PFN_vkAllocateCommandBuffers>(device, "vkAllocateCommandBuffers");
        vkFreeCommandBuffers = VulkanNative.LoadDeviceFunction<PFN_vkFreeCommandBuffers>(device, "vkFreeCommandBuffers");
        vkBeginCommandBuffer = VulkanNative.LoadDeviceFunction<PFN_vkBeginCommandBuffer>(device, "vkBeginCommandBuffer");
        vkEndCommandBuffer = VulkanNative.LoadDeviceFunction<PFN_vkEndCommandBuffer>(device, "vkEndCommandBuffer");
        vkResetCommandBuffer = VulkanNative.LoadDeviceFunction<PFN_vkResetCommandBuffer>(device, "vkResetCommandBuffer");
        vkQueueSubmit = VulkanNative.LoadDeviceFunction<PFN_vkQueueSubmit>(device, "vkQueueSubmit");
        vkQueueWaitIdle = VulkanNative.LoadDeviceFunction<PFN_vkQueueWaitIdle>(device, "vkQueueWaitIdle");
        vkQueuePresentKHR = VulkanNative.LoadDeviceFunction<PFN_vkQueuePresentKHR>(device, "vkQueuePresentKHR");
        vkAcquireNextImageKHR = VulkanNative.LoadDeviceFunction<PFN_vkAcquireNextImageKHR>(device, "vkAcquireNextImageKHR");
        vkCreateSemaphore = VulkanNative.LoadDeviceFunction<PFN_vkCreateSemaphore>(device, "vkCreateSemaphore");
        vkDestroySemaphore = VulkanNative.LoadDeviceFunction<PFN_vkDestroySemaphore>(device, "vkDestroySemaphore");
        vkCreateFence = VulkanNative.LoadDeviceFunction<PFN_vkCreateFence>(device, "vkCreateFence");
        vkDestroyFence = VulkanNative.LoadDeviceFunction<PFN_vkDestroyFence>(device, "vkDestroyFence");
        vkWaitForFences = VulkanNative.LoadDeviceFunction<PFN_vkWaitForFences>(device, "vkWaitForFences");
        vkResetFences = VulkanNative.LoadDeviceFunction<PFN_vkResetFences>(device, "vkResetFences");
        vkCmdBeginRenderPass = VulkanNative.LoadDeviceFunction<PFN_vkCmdBeginRenderPass>(device, "vkCmdBeginRenderPass");
        vkCmdEndRenderPass = VulkanNative.LoadDeviceFunction<PFN_vkCmdEndRenderPass>(device, "vkCmdEndRenderPass");
        vkCmdBindPipeline = VulkanNative.LoadDeviceFunction<PFN_vkCmdBindPipeline>(device, "vkCmdBindPipeline");
        vkCmdBindDescriptorSets = VulkanNative.LoadDeviceFunction<PFN_vkCmdBindDescriptorSets>(device, "vkCmdBindDescriptorSets");
        vkCmdBindVertexBuffers = VulkanNative.LoadDeviceFunction<PFN_vkCmdBindVertexBuffers>(device, "vkCmdBindVertexBuffers");
        vkCmdBindIndexBuffer = VulkanNative.LoadDeviceFunction<PFN_vkCmdBindIndexBuffer>(device, "vkCmdBindIndexBuffer");
        vkCmdDrawIndexed = VulkanNative.LoadDeviceFunction<PFN_vkCmdDrawIndexed>(device, "vkCmdDrawIndexed");
        vkCmdDraw = VulkanNative.LoadDeviceFunction<PFN_vkCmdDraw>(device, "vkCmdDraw");
        vkCmdSetViewport = VulkanNative.LoadDeviceFunction<PFN_vkCmdSetViewport>(device, "vkCmdSetViewport");
        vkCmdSetScissor = VulkanNative.LoadDeviceFunction<PFN_vkCmdSetScissor>(device, "vkCmdSetScissor");
        vkCmdPipelineBarrier = VulkanNative.LoadDeviceFunction<PFN_vkCmdPipelineBarrier>(device, "vkCmdPipelineBarrier");
        vkCmdCopyBuffer = VulkanNative.LoadDeviceFunction<PFN_vkCmdCopyBuffer>(device, "vkCmdCopyBuffer");
        vkCmdCopyBufferToImage = VulkanNative.LoadDeviceFunction<PFN_vkCmdCopyBufferToImage>(device, "vkCmdCopyBufferToImage");
        vkCmdCopyImageToBuffer = VulkanNative.LoadDeviceFunction<PFN_vkCmdCopyImageToBuffer>(device, "vkCmdCopyImageToBuffer");
        vkCmdClearColorImage = VulkanNative.LoadDeviceFunction<PFN_vkCmdClearColorImage>(device, "vkCmdClearColorImage");
        vkCmdPushConstants = VulkanNative.LoadDeviceFunction<PFN_vkCmdPushConstants>(device, "vkCmdPushConstants");
        vkCreateSampler = VulkanNative.LoadDeviceFunction<PFN_vkCreateSampler>(device, "vkCreateSampler");
        vkDestroySampler = VulkanNative.LoadDeviceFunction<PFN_vkDestroySampler>(device, "vkDestroySampler");
    }

    private static T Load<T>(nint libHandle, string name) where T : Delegate
    {
        var ptr = NativeLibrary.GetExport(libHandle, name);
        if (ptr == 0)
            throw new InvalidOperationException($"Failed to load Vulkan function: {name}");
        return Marshal.GetDelegateForFunctionPointer<T>(ptr);
    }

    public static void CheckResult(VkResult result, string operation)
    {
        if (result != VkResult.Success && result != VkResult.SuboptimalKHR)
            throw new InvalidOperationException($"Vulkan error {result} during: {operation}");
    }

    public static byte[] ToUtf8NullTerminated(string s)
    {
        var bytes = new byte[System.Text.Encoding.UTF8.GetByteCount(s) + 1];
        System.Text.Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
        return bytes;
    }

    public static byte* AllocUtf8(string s)
    {
        var bytes = ToUtf8NullTerminated(s);
        var ptr = (byte*)Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, (nint)ptr, bytes.Length);
        return ptr;
    }

    public static void FreeUtf8(byte* ptr) => Marshal.FreeHGlobal((nint)ptr);

    public static byte** AllocStringArray(string[] strings)
    {
        var ptrArray = (byte**)Marshal.AllocHGlobal(strings.Length * sizeof(nint));
        for (var i = 0; i < strings.Length; i++)
            ptrArray[i] = AllocUtf8(strings[i]);
        return ptrArray;
    }

    public static void FreeStringArray(byte** ptrArray, int count)
    {
        for (var i = 0; i < count; i++)
            FreeUtf8(ptrArray[i]);
        Marshal.FreeHGlobal((nint)ptrArray);
    }
}

[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateInstance(VkInstanceCreateInfo* pCreateInfo, void* pAllocator, VkInstance* pInstance);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyInstance(VkInstance instance, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkEnumeratePhysicalDevices(VkInstance instance, uint* pPhysicalDeviceCount, VkPhysicalDevice* pPhysicalDevices);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkGetPhysicalDeviceProperties(VkPhysicalDevice physicalDevice, VkPhysicalDeviceProperties* pProperties);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkGetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice physicalDevice, uint* pQueueFamilyPropertyCount, VkQueueFamilyProperties* pQueueFamilyProperties);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkGetPhysicalDeviceMemoryProperties(VkPhysicalDevice physicalDevice, VkPhysicalDeviceMemoryProperties* pMemoryProperties);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkEnumerateDeviceExtensionProperties(VkPhysicalDevice physicalDevice, byte* pLayerName, uint* pPropertyCount, VkExtensionProperties* pProperties);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateDevice(VkPhysicalDevice physicalDevice, VkDeviceCreateInfo* pCreateInfo, void* pAllocator, VkDevice* pDevice);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyDevice(VkDevice device, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkGetDeviceQueue(VkDevice device, uint queueFamilyIndex, uint queueIndex, VkQueue* pQueue);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateSwapchainKHR(VkDevice device, VkSwapchainCreateInfoKHR* pCreateInfo, void* pAllocator, VkSwapchainKHR* pSwapchain);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroySwapchainKHR(VkDevice device, VkSwapchainKHR swapchain, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkGetSwapchainImagesKHR(VkDevice device, VkSwapchainKHR swapchain, uint* pSwapchainImageCount, VkImage* pSwapchainImages);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateImageView(VkDevice device, VkImageViewCreateInfo* pCreateInfo, void* pAllocator, VkImageView* pView);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyImageView(VkDevice device, VkImageView imageView, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateImage(VkDevice device, VkImageCreateInfo* pCreateInfo, void* pAllocator, VkImage* pImage);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyImage(VkDevice device, VkImage image, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkGetImageMemoryRequirements(VkDevice device, VkImage image, void* pMemoryRequirements);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkBindImageMemory(VkDevice device, VkImage image, VkDeviceMemory memory, ulong memoryOffset);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateRenderPass(VkDevice device, VkRenderPassCreateInfo* pCreateInfo, void* pAllocator, VkRenderPass* pRenderPass);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyRenderPass(VkDevice device, VkRenderPass renderPass, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateFramebuffer(VkDevice device, VkFramebufferCreateInfo* pCreateInfo, void* pAllocator, VkFramebuffer* pFramebuffer);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyFramebuffer(VkDevice device, VkFramebuffer framebuffer, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateShaderModule(VkDevice device, VkShaderModuleCreateInfo* pCreateInfo, void* pAllocator, VkShaderModule* pShaderModule);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyShaderModule(VkDevice device, VkShaderModule shaderModule, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateDescriptorSetLayout(VkDevice device, VkDescriptorSetLayoutCreateInfo* pCreateInfo, void* pAllocator, VkDescriptorSetLayout* pSetLayout);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyDescriptorSetLayout(VkDevice device, VkDescriptorSetLayout descriptorSetLayout, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreatePipelineLayout(VkDevice device, VkPipelineLayoutCreateInfo* pCreateInfo, void* pAllocator, VkPipelineLayout* pPipelineLayout);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyPipelineLayout(VkDevice device, VkPipelineLayout pipelineLayout, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateGraphicsPipelines(VkDevice device, ulong pipelineCache, uint createInfoCount, VkGraphicsPipelineCreateInfo* pCreateInfos, void* pAllocator, VkPipeline* pPipelines);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyPipeline(VkDevice device, VkPipeline pipeline, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateDescriptorPool(VkDevice device, VkDescriptorPoolCreateInfo* pCreateInfo, void* pAllocator, VkDescriptorPool* pDescriptorPool);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyDescriptorPool(VkDevice device, VkDescriptorPool descriptorPool, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkAllocateDescriptorSets(VkDevice device, VkDescriptorSetAllocateInfo* pAllocateInfo, VkDescriptorSet* pDescriptorSets);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkUpdateDescriptorSets(VkDevice device, uint descriptorWriteCount, VkWriteDescriptorSet* pDescriptorWrites, uint descriptorCopyCount, void* pDescriptorCopies);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateBuffer(VkDevice device, VkBufferCreateInfo* pCreateInfo, void* pAllocator, VkBuffer* pBuffer);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyBuffer(VkDevice device, VkBuffer buffer, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkGetBufferMemoryRequirements(VkDevice device, VkBuffer buffer, void* pMemoryRequirements);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkBindBufferMemory(VkDevice device, VkBuffer buffer, VkDeviceMemory memory, ulong memoryOffset);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkAllocateMemory(VkDevice device, VkMemoryAllocateInfo* pAllocateInfo, void* pAllocator, VkDeviceMemory* pMemory);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkFreeMemory(VkDevice device, VkDeviceMemory memory, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkMapMemory(VkDevice device, VkDeviceMemory memory, ulong offset, ulong size, uint flags, void** ppData);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkUnmapMemory(VkDevice device, VkDeviceMemory memory);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateCommandPool(VkDevice device, VkCommandPoolCreateInfo* pCreateInfo, void* pAllocator, VkCommandPool* pCommandPool);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyCommandPool(VkDevice device, VkCommandPool commandPool, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkAllocateCommandBuffers(VkDevice device, VkCommandBufferAllocateInfo* pAllocateInfo, VkCommandBuffer* pCommandBuffers);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkFreeCommandBuffers(VkDevice device, VkCommandPool commandPool, uint commandBufferCount, VkCommandBuffer* pCommandBuffers);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkBeginCommandBuffer(VkCommandBuffer commandBuffer, VkCommandBufferBeginInfo* pBeginInfo);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkEndCommandBuffer(VkCommandBuffer commandBuffer);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkResetCommandBuffer(VkCommandBuffer commandBuffer, uint flags);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkQueueSubmit(VkQueue queue, uint submitCount, VkSubmitInfo* pSubmits, VkFence fence);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkQueueWaitIdle(VkQueue queue);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkQueuePresentKHR(VkQueue queue, VkPresentInfoKHR* pPresentInfo);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkAcquireNextImageKHR(VkDevice device, VkSwapchainKHR swapchain, ulong timeout, VkSemaphore semaphore, VkFence fence, uint* pImageIndex);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateSemaphore(VkDevice device, VkSemaphoreCreateInfo* pCreateInfo, void* pAllocator, VkSemaphore* pSemaphore);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroySemaphore(VkDevice device, VkSemaphore semaphore, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateFence(VkDevice device, VkFenceCreateInfo* pCreateInfo, void* pAllocator, VkFence* pFence);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroyFence(VkDevice device, VkFence fence, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkWaitForFences(VkDevice device, uint fenceCount, VkFence* pFences, uint waitAll, ulong timeout);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkResetFences(VkDevice device, uint fenceCount, VkFence* pFences);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdBeginRenderPass(VkCommandBuffer commandBuffer, VkRenderPassBeginInfo* pRenderPassBegin, VkSubpassContents contents);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdEndRenderPass(VkCommandBuffer commandBuffer);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdBindPipeline(VkCommandBuffer commandBuffer, int pipelineBindPoint, VkPipeline pipeline);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdBindDescriptorSets(VkCommandBuffer commandBuffer, int pipelineBindPoint, VkPipelineLayout layout, uint firstSet, uint descriptorSetCount, VkDescriptorSet* pDescriptorSets, uint dynamicOffsetCount, uint* pDynamicOffsets);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdBindVertexBuffers(VkCommandBuffer commandBuffer, uint firstBinding, uint bindingCount, VkBuffer* pBuffers, ulong* pOffsets);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdBindIndexBuffer(VkCommandBuffer commandBuffer, VkBuffer buffer, ulong offset, VkIndexType indexType);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdDrawIndexed(VkCommandBuffer commandBuffer, uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdDraw(VkCommandBuffer commandBuffer, uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdSetViewport(VkCommandBuffer commandBuffer, uint firstViewport, uint viewportCount, VkViewport* pViewports);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdSetScissor(VkCommandBuffer commandBuffer, uint firstScissor, uint scissorCount, VkRect2D* pScissors);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdPipelineBarrier(VkCommandBuffer commandBuffer, VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, int dependencyFlags, uint memoryBarrierCount, void* pMemoryBarriers, uint bufferMemoryBarrierCount, VkBufferMemoryBarrier* pBufferMemoryBarriers, uint imageMemoryBarrierCount, VkImageMemoryBarrier* pImageMemoryBarriers);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdCopyBuffer(VkCommandBuffer commandBuffer, VkBuffer srcBuffer, VkBuffer dstBuffer, uint regionCount, VkBufferCopy* pRegions);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdCopyBufferToImage(VkCommandBuffer commandBuffer, VkBuffer srcBuffer, VkImage dstImage, int dstImageLayout, uint regionCount, VkBufferImageCopy* pRegions);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdCopyImageToBuffer(VkCommandBuffer commandBuffer, VkImage srcImage, int srcImageLayout, VkBuffer dstBuffer, uint regionCount, VkBufferImageCopy* pRegions);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdClearColorImage(VkCommandBuffer commandBuffer, VkImage image, int imageLayout, VkClearColorValue* pColor, uint rangeCount, VkImageSubresourceRange* pRanges);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkCmdPushConstants(VkCommandBuffer commandBuffer, VkPipelineLayout layout, VkShaderStageFlags stageFlags, uint offset, uint size, void* pValues);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkCreateSampler(VkDevice device, VkSamplerCreateInfo* pCreateInfo, void* pAllocator, void* pSampler);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroySampler(VkDevice device, void* sampler, void* pAllocator);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkGetPhysicalDeviceSurfaceSupportKHR(VkPhysicalDevice physicalDevice, uint queueFamilyIndex, VkSurfaceKHR surface, uint* pSupported);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkGetPhysicalDeviceSurfaceCapabilitiesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, VkSurfaceCapabilitiesKHR* pSurfaceCapabilities);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkGetPhysicalDeviceSurfaceFormatsKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, uint* pSurfaceFormatCount, VkSurfaceFormatKHR* pSurfaceFormats);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate VkResult PFN_vkGetPhysicalDeviceSurfacePresentModesKHR(VkPhysicalDevice physicalDevice, VkSurfaceKHR surface, uint* pPresentModeCount, VkPresentModeKHR* pPresentModes);
[UnmanagedFunctionPointer(CallingConvention.Winapi)]
unsafe internal delegate void PFN_vkDestroySurfaceKHR(VkInstance instance, VkSurfaceKHR surface, void* pAllocator);
