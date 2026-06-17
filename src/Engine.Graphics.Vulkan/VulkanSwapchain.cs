using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanSwapchain : IDisposable
{
    public VkSwapchainKHR Swapchain;
    public VkImage[] SwapchainImages = Array.Empty<VkImage>();
    public VkImageView[] SwapchainImageViews = Array.Empty<VkImageView>();
    public VkFormat ImageFormat;
    public VkFormat DepthFormat;
    public VkExtent2D Extent;
    public VkRenderPass RenderPass;
    public VkFramebuffer[] Framebuffers = Array.Empty<VkFramebuffer>();

    public VkImage DepthImage;
    public VkDeviceMemory DepthImageMemory;
    public VkImageView DepthImageView;

    private readonly VulkanContext _ctx;
    private bool _disposed;

    public VulkanSwapchain(VulkanContext ctx, int width, int height)
    {
        _ctx = ctx;
        Create(width, height);
    }

    public unsafe void Create(int width, int height)
    {
        VkSurfaceCapabilitiesKHR caps;
        Vk.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_ctx.PhysicalDevice, _ctx.Surface, &caps);

        uint formatCount = 0;
        Vk.vkGetPhysicalDeviceSurfaceFormatsKHR(_ctx.PhysicalDevice, _ctx.Surface, &formatCount, null);
        var formats = new VkSurfaceFormatKHR[formatCount];
        fixed (VkSurfaceFormatKHR* pFormats = formats)
        {
            Vk.vkGetPhysicalDeviceSurfaceFormatsKHR(_ctx.PhysicalDevice, _ctx.Surface, &formatCount, pFormats);
        }

        ImageFormat = formats[0].format;
        foreach (var f in formats)
        {
            if (f.format == VkFormat.B8G8R8A8Srgb && f.colorSpace == VkColorSpaceKHR.SrgbNonlinear)
            {
                ImageFormat = f.format;
                break;
            }
        }
        if (ImageFormat == VkFormat.Undefined)
            ImageFormat = VkFormat.B8G8R8A8Unorm;

        Extent = caps.currentExtent;
        if (Extent.width == int.MaxValue || Extent.height == int.MaxValue || Extent.width <= 0 || Extent.height <= 0)
        {
            Extent.width = Math.Clamp(width, caps.minImageExtent.width, caps.maxImageExtent.width);
            Extent.height = Math.Clamp(height, caps.minImageExtent.height, caps.maxImageExtent.height);
        }

        uint imageCount = caps.minImageCount + 1;
        if (caps.maxImageCount > 0 && imageCount > caps.maxImageCount)
            imageCount = caps.maxImageCount;

        VkSwapchainCreateInfoKHR createInfo;
        createInfo.sType = VkStructureType.SwapchainCreateInfoKHR;
        createInfo.pNext = null;
        createInfo.flags = 0;
        createInfo.surface = _ctx.Surface;
        createInfo.minImageCount = imageCount;
        createInfo.imageFormat = ImageFormat;
        createInfo.imageColorSpace = VkColorSpaceKHR.SrgbNonlinear;
        createInfo.imageExtent = Extent;
        createInfo.imageArrayLayers = 1;
        createInfo.imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc;
        createInfo.imageSharingMode = VkSharingMode.Exclusive;
        createInfo.queueFamilyIndexCount = 0;
        createInfo.pQueueFamilyIndices = null;
        createInfo.preTransform = caps.currentTransform;
        createInfo.compositeAlpha = 0x00000001;
        createInfo.presentMode = VkPresentModeKHR.Fifo;
        createInfo.clipped = 1;
        createInfo.oldSwapchain = default;

        VkSwapchainKHR swapchain;
        VkResult result = Vk.vkCreateSwapchainKHR(_ctx.Device, &createInfo, null, &swapchain);
        Vk.CheckResult(result, "vkCreateSwapchainKHR");
        Swapchain = swapchain;

        uint actualCount = 0;
        Vk.vkGetSwapchainImagesKHR(_ctx.Device, Swapchain, &actualCount, null);
        SwapchainImages = new VkImage[actualCount];
        fixed (VkImage* pImages = SwapchainImages)
        {
            Vk.vkGetSwapchainImagesKHR(_ctx.Device, Swapchain, &actualCount, pImages);
        }

        SwapchainImageViews = new VkImageView[actualCount];
        for (uint i = 0; i < actualCount; i++)
        {
            VkImageViewCreateInfo viewInfo;
            viewInfo.sType = VkStructureType.ImageViewCreateInfo;
            viewInfo.pNext = null;
            viewInfo.flags = 0;
            viewInfo.image = SwapchainImages[i];
            viewInfo.viewType = VkImageViewType._2D;
            viewInfo.format = ImageFormat;
            viewInfo.components = new VkComponentMapping { r = 0, g = 0, b = 0, a = 0 };
            viewInfo.subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1
            };

            VkImageView view;
            result = Vk.vkCreateImageView(_ctx.Device, &viewInfo, null, &view);
            Vk.CheckResult(result, "vkCreateImageView (swapchain)");
            SwapchainImageViews[i] = view;
        }

        DepthFormat = VkFormat.D32Sfloat;
        CreateDepthImage();

        CreateRenderPass();
        CreateFramebuffers();

        Console.WriteLine($"[Vulkan] Swapchain: {actualCount} images, {Extent.width}x{Extent.height}, format {ImageFormat}");
    }

    private unsafe void CreateDepthImage()
    {
        VkImageCreateInfo imageInfo;
        imageInfo.sType = VkStructureType.ImageCreateInfo;
        imageInfo.pNext = null;
        imageInfo.flags = 0;
        imageInfo.imageType = VkImageType._2D;
        imageInfo.format = DepthFormat;
        imageInfo.extent = new VkExtent3D { width = Extent.width, height = Extent.height, depth = 1 };
        imageInfo.mipLevels = 1;
        imageInfo.arrayLayers = 1;
        imageInfo.samples = VkSampleCountFlags.One;
        imageInfo.tiling = 0;
        imageInfo.usage = VkImageUsageFlags.DepthStencilAttachment;
        imageInfo.sharingMode = VkSharingMode.Exclusive;
        imageInfo.queueFamilyIndexCount = 0;
        imageInfo.pQueueFamilyIndices = null;
        imageInfo.initialLayout = 0;

        VkImage depthImage;
        VkResult result = Vk.vkCreateImage(_ctx.Device, &imageInfo, null, &depthImage);
        Vk.CheckResult(result, "vkCreateImage (depth)");
        DepthImage = depthImage;

        VkMemoryRequirements2 memReq;
        Vk.vkGetImageMemoryRequirements(_ctx.Device, DepthImage, &memReq);

        VkMemoryAllocateInfo allocInfo;
        allocInfo.sType = VkStructureType.MemoryAllocateInfo;
        allocInfo.pNext = null;
        allocInfo.allocationSize = memReq.size;
        allocInfo.memoryTypeIndex = _ctx.FindMemoryType(memReq.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

        VkDeviceMemory depthMem;
        result = Vk.vkAllocateMemory(_ctx.Device, &allocInfo, null, &depthMem);
        Vk.CheckResult(result, "vkAllocateMemory (depth)");
        DepthImageMemory = depthMem;

        result = Vk.vkBindImageMemory(_ctx.Device, DepthImage, DepthImageMemory, 0);
        Vk.CheckResult(result, "vkBindImageMemory (depth)");

        VkImageViewCreateInfo viewInfo;
        viewInfo.sType = VkStructureType.ImageViewCreateInfo;
        viewInfo.pNext = null;
        viewInfo.flags = 0;
        viewInfo.image = DepthImage;
        viewInfo.viewType = VkImageViewType._2D;
        viewInfo.format = DepthFormat;
        viewInfo.components = new VkComponentMapping { r = 0, g = 0, b = 0, a = 0 };
        viewInfo.subresourceRange = new VkImageSubresourceRange
        {
            aspectMask = VkImageAspectFlags.Depth,
            baseMipLevel = 0,
            levelCount = 1,
            baseArrayLayer = 0,
            layerCount = 1
        };

        VkImageView depthView;
        result = Vk.vkCreateImageView(_ctx.Device, &viewInfo, null, &depthView);
        Vk.CheckResult(result, "vkCreateImageView (depth)");
        DepthImageView = depthView;
    }

    private unsafe void CreateRenderPass()
    {
        var attachments = new VkAttachmentDescription[2];
        attachments[0] = new VkAttachmentDescription
        {
            flags = 0,
            format = ImageFormat,
            samples = (uint)VkSampleCountFlags.One,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.PresentSrcKHR
        };
        attachments[1] = new VkAttachmentDescription
        {
            flags = 0,
            format = DepthFormat,
            samples = (uint)VkSampleCountFlags.One,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.DontCare,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.DepthStencilAttachmentOptimal
        };

        var colorRef = new VkAttachmentReference { attachment = 0, layout = VkImageLayout.ColorAttachmentOptimal };
        var depthRef = new VkAttachmentReference { attachment = 1, layout = VkImageLayout.DepthStencilAttachmentOptimal };

        VkSubpassDescription subpass;
        subpass.flags = 0;
        subpass.pipelineBindPoint = 0;
        subpass.inputAttachmentCount = 0;
        subpass.pInputAttachments = null;
        subpass.colorAttachmentCount = 1;
        subpass.pColorAttachments = &colorRef;
        subpass.pResolveAttachments = null;
        subpass.pDepthStencilAttachment = &depthRef;
        subpass.preserveAttachmentCount = 0;
        subpass.pPreserveAttachments = null;

        var dependencies = new VkSubpassDependency[2];
        dependencies[0] = new VkSubpassDependency
        {
            srcSubpass = ~0u,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests,
            srcAccessMask = 0,
            dstAccessMask = VkAccessFlags.ColorAttachmentWrite | VkAccessFlags.DepthStencilAttachmentWrite,
            dependencyFlags = 0,
            viewOffset = 0
        };
        dependencies[1] = new VkSubpassDependency
        {
            srcSubpass = 0,
            dstSubpass = ~0u,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests,
            dstStageMask = VkPipelineStageFlags.BottomOfPipe,
            srcAccessMask = VkAccessFlags.ColorAttachmentWrite | VkAccessFlags.DepthStencilAttachmentWrite,
            dstAccessMask = 0,
            dependencyFlags = 0,
            viewOffset = 0
        };

        fixed (VkAttachmentDescription* pAttachments = attachments)
        fixed (VkSubpassDependency* pDeps = dependencies)
        {
            VkRenderPassCreateInfo createInfo;
            createInfo.sType = VkStructureType.RenderPassCreateInfo;
            createInfo.pNext = null;
            createInfo.flags = 0;
            createInfo.attachmentCount = 2;
            createInfo.pAttachments = pAttachments;
            createInfo.subpassCount = 1;
            createInfo.pSubpasses = &subpass;
            createInfo.dependencyCount = 2;
            createInfo.pDependencies = pDeps;

            VkRenderPass renderPass;
            VkResult result = Vk.vkCreateRenderPass(_ctx.Device, &createInfo, null, &renderPass);
            Vk.CheckResult(result, "vkCreateRenderPass");
            RenderPass = renderPass;
        }
    }

    private unsafe void CreateFramebuffers()
    {
        Framebuffers = new VkFramebuffer[SwapchainImageViews.Length];

        for (uint i = 0; i < SwapchainImageViews.Length; i++)
        {
            var attachments = new VkImageView[] { SwapchainImageViews[i], DepthImageView };

            fixed (VkImageView* pAttachments = attachments)
            {
                VkFramebufferCreateInfo createInfo;
                createInfo.sType = VkStructureType.FramebufferCreateInfo;
                createInfo.pNext = null;
                createInfo.flags = 0;
                createInfo.renderPass = RenderPass;
                createInfo.attachmentCount = 2;
                createInfo.pAttachments = pAttachments;
                createInfo.width = (uint)Extent.width;
                createInfo.height = (uint)Extent.height;
                createInfo.layers = 1;

                VkFramebuffer fb;
                VkResult result = Vk.vkCreateFramebuffer(_ctx.Device, &createInfo, null, &fb);
                Vk.CheckResult(result, "vkCreateFramebuffer");
                Framebuffers[i] = fb;
            }
        }
    }

    public void Recreate(int width, int height)
    {
        Vk.vkQueueWaitIdle(_ctx.GraphicsQueue);

        CleanupSwapchain();
        Create(width, height);
    }

    private void CleanupSwapchain()
    {
        foreach (var fb in Framebuffers)
            if (fb.Value != 0) Vk.vkDestroyFramebuffer(_ctx.Device, fb, null);

        if (DepthImageView.Value != 0) Vk.vkDestroyImageView(_ctx.Device, DepthImageView, null);
        if (DepthImage.Value != 0) Vk.vkDestroyImage(_ctx.Device, DepthImage, null);
        if (DepthImageMemory.Value != 0) Vk.vkFreeMemory(_ctx.Device, DepthImageMemory, null);

        foreach (var iv in SwapchainImageViews)
            if (iv.Value != 0) Vk.vkDestroyImageView(_ctx.Device, iv, null);

        if (Swapchain.Value != 0) Vk.vkDestroySwapchainKHR(_ctx.Device, Swapchain, null);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        CleanupSwapchain();
        if (RenderPass.Value != 0) Vk.vkDestroyRenderPass(_ctx.Device, RenderPass, null);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryRequirements2
{
    public ulong size;
    public ulong alignment;
    public uint memoryTypeBits;
    public uint _pad;
}
