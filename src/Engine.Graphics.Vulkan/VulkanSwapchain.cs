using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanSwapchain : IDisposable
{
    public VkSwapchainKHR Swapchain;
    public VkImage[] Images = Array.Empty<VkImage>();
    public VkImageView[] ImageViews = Array.Empty<VkImageView>();
    public VkFormat Format;
    public VkExtent2D Extent;
    public uint ImageCount;

    private readonly VkDevice _device;
    private readonly VkPhysicalDevice _physicalDevice;
    private readonly VkSurfaceKHR _surface;
    private readonly VkSurfaceFormatKHR _surfaceFormat;
    private bool _disposed;

    public VulkanSwapchain(VkDevice device, VkPhysicalDevice physicalDevice, VkSurfaceKHR surface,
        VkSurfaceFormatKHR surfaceFormat, int width, int height)
    {
        _device = device;
        _physicalDevice = physicalDevice;
        _surface = surface;
        _surfaceFormat = surfaceFormat;
        Create(width, height);
    }

    private void Create(int width, int height)
    {
        var caps = new VkSurfaceCapabilitiesKHR();
        Vk.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_physicalDevice, _surface, &caps);

        Extent = caps.currentExtent;
        if (Extent.Width == uint.MaxValue || Extent.Height == uint.MaxValue)
        {
            Extent.Width = (uint)width;
            Extent.Height = (uint)height;
        }
        Extent.Width = Math.Max(caps.minImageExtent.Width, Math.Min(caps.maxImageExtent.Width, Extent.Width));
        Extent.Height = Math.Max(caps.minImageExtent.Height, Math.Min(caps.maxImageExtent.Height, Extent.Height));

        uint imageCount = caps.minImageCount + 1;
        if (caps.maxImageCount > 0 && imageCount > caps.maxImageCount)
            imageCount = caps.maxImageCount;

        uint presentModeCount = 0;
        Vk.vkGetPhysicalDeviceSurfacePresentModesKHR(_physicalDevice, _surface, &presentModeCount, null);
        var presentModes = stackalloc VkPresentModeKHR[(int)presentModeCount];
        Vk.vkGetPhysicalDeviceSurfacePresentModesKHR(_physicalDevice, _surface, &presentModeCount, presentModes);

        var presentMode = VkPresentModeKHR.Fifo;
        for (uint i = 0; i < presentModeCount; i++)
        {
            if (presentModes[(int)i] == VkPresentModeKHR.Mailbox)
            {
                presentMode = VkPresentModeKHR.Mailbox;
                break;
            }
        }

        Format = _surfaceFormat.format;

        var createInfo = new VkSwapchainCreateInfoKHR
        {
            sType = VkStructureType.SwapchainCreateInfoKHR,
            surface = _surface,
            minImageCount = imageCount,
            imageFormat = _surfaceFormat.format,
            imageColorSpace = _surfaceFormat.colorSpace,
            imageExtent = Extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst,
            imageSharingMode = VkSharingMode.Exclusive,
            preTransform = caps.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = presentMode,
            clipped = VkBool32.True,
            oldSwapchain = VkSwapchainKHR.Null,
        };

        fixed (VkSwapchainKHR* swPtr = &Swapchain)
        {
            var result = Vk.vkCreateSwapchainKHR(_device, &createInfo, 0, swPtr);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateSwapchainKHR failed: {result}");
        }

        uint actualCount = 0;
        Vk.vkGetSwapchainImagesKHR(_device, Swapchain, &actualCount, null);
        Images = new VkImage[actualCount];
        ImageViews = new VkImageView[actualCount];
        ImageCount = actualCount;

        fixed (VkImage* imgPtr = Images)
        {
            Vk.vkGetSwapchainImagesKHR(_device, Swapchain, &actualCount, imgPtr);
        }

        for (uint i = 0; i < actualCount; i++)
        {
            var viewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = Images[i],
                viewType = VkImageViewType.Type2D,
                format = Format,
                components = new VkComponentMapping
                {
                    R = VkComponentSwizzle.Identity,
                    G = VkComponentSwizzle.Identity,
                    B = VkComponentSwizzle.Identity,
                    A = VkComponentSwizzle.Identity,
                },
                subresourceRange = new VkImageSubresourceRange
                {
                    AspectMask = VkImageAspectFlags.Color,
                    BaseMipLevel = 0,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    LayerCount = 1,
                },
            };

            fixed (VkImageView* viewPtr = &ImageViews[i])
            {
                var result = Vk.vkCreateImageView(_device, &viewInfo, 0, viewPtr);
                if (result != VkResult.Success)
                    throw new InvalidOperationException($"vkCreateImageView failed: {result}");
            }
        }

        Console.WriteLine($"[Vulkan] Swapchain: {actualCount} images, {Extent.Width}x{Extent.Height}, format={Format}");
    }

    public void Recreate(int width, int height)
    {
        Vk.vkDeviceWaitIdle(_device);
        Cleanup();
        Create(width, height);
    }

    private void Cleanup()
    {
        for (int i = 0; i < ImageViews.Length; i++)
        {
            if (ImageViews[i].Handle != 0)
                Vk.vkDestroyImageView(_device, ImageViews[i], 0);
        }
        ImageViews = Array.Empty<VkImageView>();
        Images = Array.Empty<VkImage>();

        if (Swapchain.Handle != 0)
        {
            Vk.vkDestroySwapchainKHR(_device, Swapchain, 0);
            Swapchain = VkSwapchainKHR.Null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cleanup();
    }
}
