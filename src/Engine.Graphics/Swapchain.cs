using System;
using Vortice.Vulkan;

namespace Engine.Graphics;

/// <summary>
/// Manages the Vulkan swapchain, image views, render pass, and framebuffers.
/// Recreates itself automatically when the window is resized.
/// </summary>
public sealed unsafe class Swapchain : IDisposable
{
    private readonly VulkanContext _context;
    private VkRenderPass _renderPass;
    private VkSwapchainKHR _swapchain;
    private VkImage[] _images = null!;
    private VkImageView[] _imageViews = null!;
    private VkFramebuffer[] _framebuffers = null!;
    private VkSurfaceFormatKHR _surfaceFormat;
    private VkPresentModeKHR _presentMode;
    private VkExtent2D _extent;

    public VkRenderPass RenderPass => _renderPass;
    public VkFramebuffer[] Framebuffers => _framebuffers;
    public VkExtent2D Extent => _extent;
    public VkSwapchainKHR Handle => _swapchain;
    public uint ImageCount => (uint)_images.Length;

    public Swapchain(VulkanContext context)
    {
        _context = context;
        _surfaceFormat = ChooseSurfaceFormat();
        CreateRenderPass();
        Recreate(1280, 720);
    }

    public void Recreate(int width, int height)
    {
        _context.DeviceApi.vkDeviceWaitIdle();
        CleanupSwapchain();

        var capabilities = GetSurfaceCapabilities();
        _surfaceFormat = ChooseSurfaceFormat();
        _presentMode = ChoosePresentMode();
        _extent = ChooseExtent(capabilities, (uint)width, (uint)height);

        var imageCount = capabilities.minImageCount + 1;
        if (capabilities.maxImageCount > 0 && imageCount > capabilities.maxImageCount)
            imageCount = capabilities.maxImageCount;

        var createInfo = new VkSwapchainCreateInfoKHR
        {
            sType = VkStructureType.SwapchainCreateInfoKHR,
            surface = _context.Surface,
            minImageCount = imageCount,
            imageFormat = _surfaceFormat.format,
            imageColorSpace = _surfaceFormat.colorSpace,
            imageExtent = _extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            imageSharingMode = VkSharingMode.Exclusive,
            preTransform = capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = _presentMode,
            clipped = true,
            oldSwapchain = _swapchain
        };

        var result = _context.DeviceApi.vkCreateSwapchainKHR(&createInfo, null, out _swapchain);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateSwapchainKHR failed: {result}");

        _images = GetSwapchainImages();
        _imageViews = new VkImageView[_images.Length];
        _framebuffers = new VkFramebuffer[_images.Length];

        for (var i = 0; i < _images.Length; i++)
        {
            _imageViews[i] = CreateImageView(_images[i], _surfaceFormat.format);
            _framebuffers[i] = CreateFramebuffer(_imageViews[i]);
        }
    }

    private VkSurfaceCapabilitiesKHR GetSurfaceCapabilities()
    {
        var result = _context.InstanceApi.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_context.PhysicalDevice, _context.Surface, out var capabilities);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkGetPhysicalDeviceSurfaceCapabilitiesKHR failed: {result}");
        return capabilities;
    }

    private VkImage[] GetSwapchainImages()
    {
        uint count = 0;
        _context.DeviceApi.vkGetSwapchainImagesKHR(_swapchain, &count, null);
        var images = new VkImage[count];
        fixed (VkImage* p = images)
        {
            var result = _context.DeviceApi.vkGetSwapchainImagesKHR(_swapchain, &count, p);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkGetSwapchainImagesKHR failed: {result}");
        }
        return images;
    }

    private void CreateRenderPass()
    {
        var colorAttachment = new VkAttachmentDescription
        {
            format = _surfaceFormat.format != VkFormat.Undefined ? _surfaceFormat.format : VkFormat.B8G8R8A8Unorm,
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.PresentSrcKHR
        };

        var colorAttachmentRef = new VkAttachmentReference
        {
            attachment = 0,
            layout = VkImageLayout.ColorAttachmentOptimal
        };

        var subpass = new VkSubpassDescription
        {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentRef
        };

        var dependency = new VkSubpassDependency
        {
            srcSubpass = Vulkan.VK_SUBPASS_EXTERNAL,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            srcAccessMask = VkAccessFlags.None,
            dstAccessMask = VkAccessFlags.ColorAttachmentWrite
        };

        var createInfo = new VkRenderPassCreateInfo
        {
            sType = VkStructureType.RenderPassCreateInfo,
            attachmentCount = 1,
            pAttachments = &colorAttachment,
            subpassCount = 1,
            pSubpasses = &subpass,
            dependencyCount = 1,
            pDependencies = &dependency
        };

        var result = _context.DeviceApi.vkCreateRenderPass(&createInfo, null, out _renderPass);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateRenderPass failed: {result}");
    }

    private VkImageView CreateImageView(VkImage image, VkFormat format)
    {
        var createInfo = new VkImageViewCreateInfo
        {
            sType = VkStructureType.ImageViewCreateInfo,
            image = image,
            viewType = VkImageViewType.Image2D,
            format = format,
            components = new VkComponentMapping(VkComponentSwizzle.R, VkComponentSwizzle.G, VkComponentSwizzle.B, VkComponentSwizzle.A),
            subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
        };

        var result = _context.DeviceApi.vkCreateImageView(&createInfo, null, out var imageView);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateImageView failed: {result}");

        return imageView;
    }

    private VkFramebuffer CreateFramebuffer(VkImageView imageView)
    {
        var createInfo = new VkFramebufferCreateInfo
        {
            sType = VkStructureType.FramebufferCreateInfo,
            renderPass = _renderPass,
            attachmentCount = 1,
            pAttachments = &imageView,
            width = _extent.width,
            height = _extent.height,
            layers = 1
        };

        var result = _context.DeviceApi.vkCreateFramebuffer(&createInfo, null, out var framebuffer);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateFramebuffer failed: {result}");

        return framebuffer;
    }

    private VkSurfaceFormatKHR ChooseSurfaceFormat()
    {
        var formats = GetSurfaceFormats();
        foreach (var format in formats)
        {
            if (format.format == VkFormat.B8G8R8A8Unorm && format.colorSpace == VkColorSpaceKHR.SrgbNonLinear)
                return format;
        }
        return formats[0];
    }

    private VkSurfaceFormatKHR[] GetSurfaceFormats()
    {
        uint count = 0;
        _context.InstanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(_context.PhysicalDevice, _context.Surface, &count, null);
        var formats = new VkSurfaceFormatKHR[count];
        fixed (VkSurfaceFormatKHR* p = formats)
        {
            var result = _context.InstanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(_context.PhysicalDevice, _context.Surface, &count, p);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkGetPhysicalDeviceSurfaceFormatsKHR failed: {result}");
        }
        return formats;
    }

    private VkPresentModeKHR ChoosePresentMode()
    {
        var modes = GetSurfacePresentModes();
        if (Array.Exists(modes, m => m == VkPresentModeKHR.Mailbox))
            return VkPresentModeKHR.Mailbox;
        return VkPresentModeKHR.Fifo;
    }

    private VkPresentModeKHR[] GetSurfacePresentModes()
    {
        uint count = 0;
        _context.InstanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(_context.PhysicalDevice, _context.Surface, &count, null);
        var modes = new VkPresentModeKHR[count];
        fixed (VkPresentModeKHR* p = modes)
        {
            var result = _context.InstanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(_context.PhysicalDevice, _context.Surface, &count, p);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkGetPhysicalDeviceSurfacePresentModesKHR failed: {result}");
        }
        return modes;
    }

    private VkExtent2D ChooseExtent(VkSurfaceCapabilitiesKHR capabilities, uint width, uint height)
    {
        if (capabilities.currentExtent.width != uint.MaxValue)
            return capabilities.currentExtent;

        var extent = new VkExtent2D
        {
            width = Math.Clamp(width, capabilities.minImageExtent.width, capabilities.maxImageExtent.width),
            height = Math.Clamp(height, capabilities.minImageExtent.height, capabilities.maxImageExtent.height)
        };
        return extent;
    }

    private void CleanupSwapchain()
    {
        if (_context.Device == VkDevice.Null)
            return;

        if (_framebuffers != null)
        {
            foreach (var fb in _framebuffers)
            {
                if (fb != VkFramebuffer.Null)
                    _context.DeviceApi.vkDestroyFramebuffer(fb);
            }
        }

        if (_imageViews != null)
        {
            foreach (var view in _imageViews)
            {
                if (view != VkImageView.Null)
                    _context.DeviceApi.vkDestroyImageView(view);
            }
        }

        if (_swapchain != VkSwapchainKHR.Null)
            _context.DeviceApi.vkDestroySwapchainKHR(_swapchain);
    }

    public void Dispose()
    {
        _context.DeviceApi.vkDeviceWaitIdle();
        CleanupSwapchain();

        if (_renderPass != VkRenderPass.Null)
            _context.DeviceApi.vkDestroyRenderPass(_renderPass);
    }
}
