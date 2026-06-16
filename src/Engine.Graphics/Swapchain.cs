using System;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Engine.Graphics;

/// <summary>
/// Manages the Vulkan swapchain, image views, render pass, and framebuffers.
/// Uses Silk.NET.Vulkan.
/// </summary>
public sealed unsafe class Swapchain : IDisposable
{
    private readonly VulkanContext _context;
    private RenderPass _renderPass;
    private SwapchainKHR _swapchain;
    private Image[] _images = null!;
    private ImageView[] _imageViews = null!;
    private Framebuffer[] _framebuffers = null!;
    private SurfaceFormatKHR _surfaceFormat;
    private PresentModeKHR _presentMode;
    private Extent2D _extent;

    public RenderPass RenderPass => _renderPass;
    public Framebuffer[] Framebuffers => _framebuffers;
    public Extent2D Extent => _extent;
    public SwapchainKHR Handle => _swapchain;
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
        _context.Vk.DeviceWaitIdle(_context.Device);
        CleanupSwapchain();

        var capabilities = GetSurfaceCapabilities();
        _surfaceFormat = ChooseSurfaceFormat();
        _presentMode = ChoosePresentMode();
        _extent = ChooseExtent(capabilities, (uint)width, (uint)height);

        var imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
            imageCount = capabilities.MaxImageCount;

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _context.Surface,
            MinImageCount = imageCount,
            ImageFormat = _surfaceFormat.Format,
            ImageColorSpace = _surfaceFormat.ColorSpace,
            ImageExtent = _extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = _presentMode,
            Clipped = true,
            OldSwapchain = _swapchain
        };

        SwapchainKHR swapchain;
        var result = _context.KhrSwapchain!.CreateSwapchain(_context.Device, &createInfo, null, &swapchain);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateSwapchainKHR failed: {result}");
        _swapchain = swapchain;

        _images = GetSwapchainImages();
        _imageViews = new ImageView[_images.Length];
        _framebuffers = new Framebuffer[_images.Length];

        for (var i = 0; i < _images.Length; i++)
        {
            _imageViews[i] = CreateImageView(_images[i], _surfaceFormat.Format);
            _framebuffers[i] = CreateFramebuffer(_imageViews[i]);
        }
    }

    private SurfaceCapabilitiesKHR GetSurfaceCapabilities()
    {
        SurfaceCapabilitiesKHR capabilities;
        var result = _context.KhrSurface!.GetPhysicalDeviceSurfaceCapabilities(_context.PhysicalDevice, _context.Surface, &capabilities);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkGetPhysicalDeviceSurfaceCapabilitiesKHR failed: {result}");
        return capabilities;
    }

    private Image[] GetSwapchainImages()
    {
        uint count = 0;
        _context.KhrSwapchain!.GetSwapchainImages(_context.Device, _swapchain, &count, null);
        var images = new Image[count];
        fixed (Image* p = images)
        {
            var result = _context.KhrSwapchain!.GetSwapchainImages(_context.Device, _swapchain, &count, p);
            if (result != Result.Success)
                throw new InvalidOperationException($"vkGetSwapchainImagesKHR failed: {result}");
        }
        return images;
    }

    private void CreateRenderPass()
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = _surfaceFormat.Format != Format.Undefined ? _surfaceFormat.Format : Format.B8G8R8A8Unorm,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef
        };

        var dependency = new SubpassDependency
        {
            SrcSubpass = ~0u,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = AccessFlags.None,
            DstAccessMask = AccessFlags.ColorAttachmentWriteBit
        };

        var createInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency
        };

        RenderPass renderPass;
        var result = _context.Vk.CreateRenderPass(_context.Device, &createInfo, null, &renderPass);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateRenderPass failed: {result}");
        _renderPass = renderPass;
    }

    private ImageView CreateImageView(Image image, Format format)
    {
        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            Components = new ComponentMapping(ComponentSwizzle.R, ComponentSwizzle.G, ComponentSwizzle.B, ComponentSwizzle.A),
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };

        ImageView imageView;
        var result = _context.Vk.CreateImageView(_context.Device, &createInfo, null, &imageView);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateImageView failed: {result}");
        return imageView;
    }

    private Framebuffer CreateFramebuffer(ImageView imageView)
    {
        var createInfo = new FramebufferCreateInfo
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = _renderPass,
            AttachmentCount = 1,
            PAttachments = &imageView,
            Width = _extent.Width,
            Height = _extent.Height,
            Layers = 1
        };

        Framebuffer framebuffer;
        var result = _context.Vk.CreateFramebuffer(_context.Device, &createInfo, null, &framebuffer);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateFramebuffer failed: {result}");
        return framebuffer;
    }

    private SurfaceFormatKHR ChooseSurfaceFormat()
    {
        var formats = GetSurfaceFormats();
        foreach (var format in formats)
        {
            if (format.Format == Format.B8G8R8A8Unorm && format.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
                return format;
        }
        return formats[0];
    }

    private SurfaceFormatKHR[] GetSurfaceFormats()
    {
        uint count = 0;
        _context.KhrSurface!.GetPhysicalDeviceSurfaceFormats(_context.PhysicalDevice, _context.Surface, &count, null);
        var formats = new SurfaceFormatKHR[count];
        fixed (SurfaceFormatKHR* p = formats)
        {
            var result = _context.KhrSurface!.GetPhysicalDeviceSurfaceFormats(_context.PhysicalDevice, _context.Surface, &count, p);
            if (result != Result.Success)
                throw new InvalidOperationException($"vkGetPhysicalDeviceSurfaceFormatsKHR failed: {result}");
        }
        return formats;
    }

    private PresentModeKHR ChoosePresentMode()
    {
        var modes = GetSurfacePresentModes();
        if (Array.Exists(modes, m => m == PresentModeKHR.MailboxKhr))
            return PresentModeKHR.MailboxKhr;
        return PresentModeKHR.FifoKhr;
    }

    private PresentModeKHR[] GetSurfacePresentModes()
    {
        uint count = 0;
        _context.KhrSurface!.GetPhysicalDeviceSurfacePresentModes(_context.PhysicalDevice, _context.Surface, &count, null);
        var modes = new PresentModeKHR[count];
        fixed (PresentModeKHR* p = modes)
        {
            var result = _context.KhrSurface!.GetPhysicalDeviceSurfacePresentModes(_context.PhysicalDevice, _context.Surface, &count, p);
            if (result != Result.Success)
                throw new InvalidOperationException($"vkGetPhysicalDeviceSurfacePresentModesKHR failed: {result}");
        }
        return modes;
    }

    private Extent2D ChooseExtent(SurfaceCapabilitiesKHR capabilities, uint width, uint height)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
            return capabilities.CurrentExtent;

        var extent = new Extent2D
        {
            Width = Math.Clamp(width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
            Height = Math.Clamp(height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height)
        };
        return extent;
    }

    private void CleanupSwapchain()
    {
        if (_context.Device.Handle == 0)
            return;

        if (_framebuffers != null)
        {
            foreach (var fb in _framebuffers)
            {
                if (fb.Handle != 0)
                    _context.Vk.DestroyFramebuffer(_context.Device, fb, null);
            }
        }

        if (_imageViews != null)
        {
            foreach (var view in _imageViews)
            {
                if (view.Handle != 0)
                    _context.Vk.DestroyImageView(_context.Device, view, null);
            }
        }

        if (_swapchain.Handle != 0)
            _context.KhrSwapchain!.DestroySwapchain(_context.Device, _swapchain, null);
    }

    public void Dispose()
    {
        _context.Vk.DeviceWaitIdle(_context.Device);
        CleanupSwapchain();

        if (_renderPass.Handle != 0)
            _context.Vk.DestroyRenderPass(_context.Device, _renderPass, null);
    }
}
