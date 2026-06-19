using System.Runtime.InteropServices;
using System.Numerics;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanShadowMap : IDisposable
{
    public const uint ShadowMapSize = 1024;
    public const int MaxShadowLights = 4;

    public VkImage ColorImage;
    public VkDeviceMemory ColorImageMemory;
    public VkImageView CubeArrayColorView;
    public VkImageView[] FaceColorViews = new VkImageView[MaxShadowLights * 6];

    public VkImage DepthImage;
    public VkDeviceMemory DepthImageMemory;
    public VkImageView[] FaceDepthViews = new VkImageView[MaxShadowLights * 6];

    public VkSampler ShadowSampler;
    public VkPipeline Pipeline;
    public VkPipelineLayout PipelineLayout;
    public VkShaderModule VertModule;

    private readonly VkDevice _device;
    private readonly VulkanContext _ctx;
    private bool _disposed;

    public VulkanShadowMap(VkDevice device, VulkanContext ctx, VkDescriptorSetLayout descLayout)
    {
        _device = device;
        _ctx = ctx;

        CreateShadowImages();
        CreateShadowSampler();
        CreateShadowPipeline();

        Console.WriteLine($"[Vulkan] Shadow cubemap array created ({ShadowMapSize}x{ShadowMapSize}x{MaxShadowLights*6} layers)");
    }

    private void CreateShadowImages()
    {
        int totalLayers = MaxShadowLights * 6;

        // Color cube array (R32_SFLOAT) — stores linear distance
        var colorInfo = new VkImageCreateInfo
        {
            sType = VkStructureType.ImageCreateInfo,
            flags = (uint)VkImageCreateFlags.CubeCompatible,
            imageType = VkImageType.Type2D,
            format = VkFormat.R32Sfloat,
            extent = new VkExtent3D { Width = ShadowMapSize, Height = ShadowMapSize, Depth = 1 },
            mipLevels = 1,
            arrayLayers = (uint)totalLayers,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
        };

        var colorImg = VkImage.Null;
        Vk.vkCreateImage(_device, &colorInfo, 0, &colorImg);
        ColorImage = colorImg;

        var colorReqs = new VkMemoryRequirements();
        Vk.vkGetImageMemoryRequirements(_device, ColorImage, &colorReqs);
        var colorMemType = _ctx.FindMemoryType(colorReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);
        var colorAlloc = new VkMemoryAllocateInfo { sType = VkStructureType.MemoryAllocateInfo, allocationSize = colorReqs.size, memoryTypeIndex = colorMemType };
        var colorMem = VkDeviceMemory.Null;
        Vk.vkAllocateMemory(_device, &colorAlloc, 0, &colorMem);
        ColorImageMemory = colorMem;
        Vk.vkBindImageMemory(_device, ColorImage, ColorImageMemory, 0);

        // Cube array color view for sampling
        var cubeArrayViewInfo = new VkImageViewCreateInfo
        {
            sType = VkStructureType.ImageViewCreateInfo,
            image = ColorImage,
            viewType = VkImageViewType.TypeCubeArray,
            format = VkFormat.R32Sfloat,
            components = new VkComponentMapping { R = VkComponentSwizzle.Identity, G = VkComponentSwizzle.Identity, B = VkComponentSwizzle.Identity, A = VkComponentSwizzle.Identity },
            subresourceRange = new VkImageSubresourceRange { AspectMask = VkImageAspectFlags.Color, BaseMipLevel = 0, LevelCount = 1, BaseArrayLayer = 0, LayerCount = (uint)totalLayers },
        };
        var cubeArrayView = VkImageView.Null;
        Vk.vkCreateImageView(_device, &cubeArrayViewInfo, 0, &cubeArrayView);
        CubeArrayColorView = cubeArrayView;

        // Face color views for rendering
        for (int i = 0; i < totalLayers; i++)
        {
            var faceViewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = ColorImage,
                viewType = VkImageViewType.Type2D,
                format = VkFormat.R32Sfloat,
                components = new VkComponentMapping { R = VkComponentSwizzle.Identity, G = VkComponentSwizzle.Identity, B = VkComponentSwizzle.Identity, A = VkComponentSwizzle.Identity },
                subresourceRange = new VkImageSubresourceRange { AspectMask = VkImageAspectFlags.Color, BaseMipLevel = 0, LevelCount = 1, BaseArrayLayer = (uint)i, LayerCount = 1 },
            };
            var fv = VkImageView.Null;
            Vk.vkCreateImageView(_device, &faceViewInfo, 0, &fv);
            FaceColorViews[i] = fv;
        }

        // Depth cube array (D32_SFLOAT)
        var depthInfo = new VkImageCreateInfo
        {
            sType = VkStructureType.ImageCreateInfo,
            flags = (uint)VkImageCreateFlags.CubeCompatible,
            imageType = VkImageType.Type2D,
            format = VkFormat.D32Sfloat,
            extent = new VkExtent3D { Width = ShadowMapSize, Height = ShadowMapSize, Depth = 1 },
            mipLevels = 1,
            arrayLayers = (uint)totalLayers,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = VkImageUsageFlags.DepthStencilAttachment,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
        };

        var depthImg = VkImage.Null;
        Vk.vkCreateImage(_device, &depthInfo, 0, &depthImg);
        DepthImage = depthImg;

        var depthReqs = new VkMemoryRequirements();
        Vk.vkGetImageMemoryRequirements(_device, DepthImage, &depthReqs);
        var depthMemType = _ctx.FindMemoryType(depthReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);
        var depthAlloc = new VkMemoryAllocateInfo { sType = VkStructureType.MemoryAllocateInfo, allocationSize = depthReqs.size, memoryTypeIndex = depthMemType };
        var depthMem = VkDeviceMemory.Null;
        Vk.vkAllocateMemory(_device, &depthAlloc, 0, &depthMem);
        DepthImageMemory = depthMem;
        Vk.vkBindImageMemory(_device, DepthImage, DepthImageMemory, 0);

        // Face depth views
        for (int i = 0; i < totalLayers; i++)
        {
            var faceDepthViewInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = DepthImage,
                viewType = VkImageViewType.Type2D,
                format = VkFormat.D32Sfloat,
                components = new VkComponentMapping { R = VkComponentSwizzle.Identity, G = VkComponentSwizzle.Identity, B = VkComponentSwizzle.Identity, A = VkComponentSwizzle.Identity },
                subresourceRange = new VkImageSubresourceRange { AspectMask = VkImageAspectFlags.Depth, BaseMipLevel = 0, LevelCount = 1, BaseArrayLayer = (uint)i, LayerCount = 1 },
            };
            var fdv = VkImageView.Null;
            Vk.vkCreateImageView(_device, &faceDepthViewInfo, 0, &fdv);
            FaceDepthViews[i] = fdv;
        }
    }

    private void CreateShadowSampler()
    {
        var samplerInfo = new VkSamplerCreateInfo
        {
            sType = VkStructureType.SamplerCreateInfo,
            magFilter = VkFilter.Linear,
            minFilter = VkFilter.Linear,
            mipmapMode = VkSamplerMipmapMode.Linear,
            addressModeU = VkSamplerAddressMode.ClampToBorder,
            addressModeV = VkSamplerAddressMode.ClampToBorder,
            addressModeW = VkSamplerAddressMode.ClampToBorder,
            minLod = 0,
            maxLod = 1,
            borderColor = 1, // FLOAT_OPAQUE_WHITE
        };

        var samp = VkSampler.Null;
        Vk.vkCreateSampler(_device, &samplerInfo, 0, &samp);
        ShadowSampler = samp;
    }

    public static (Matrix4x4 view, Matrix4x4 proj) GetFaceViewProj(Vector3 lightPos, int face)
    {
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1.0f, 0.1f, 60f);
        // No M22 flip for shadow cubemap — Vulkan cubemap sampling handles Y internally
        // The M22 flip is only for the main render pass (swapchain), not for offscreen cube

        var target = lightPos;
        Vector3 up;

        switch (face)
        {
            case 0: // +X
                target += Vector3.UnitX;
                up = new Vector3(0, -1, 0);
                break;
            case 1: // -X
                target += -Vector3.UnitX;
                up = new Vector3(0, -1, 0);
                break;
            case 2: // +Y
                target += Vector3.UnitY;
                up = new Vector3(0, 0, 1);
                break;
            case 3: // -Y
                target += -Vector3.UnitY;
                up = new Vector3(0, 0, -1);
                break;
            case 4: // +Z
                target += Vector3.UnitZ;
                up = new Vector3(0, -1, 0);
                break;
            case 5: // -Z
                target += -Vector3.UnitZ;
                up = new Vector3(0, -1, 0);
                break;
            default:
                target += Vector3.UnitZ;
                up = new Vector3(0, -1, 0);
                break;
        }

        var view = Matrix4x4.CreateLookAt(lightPos, target, up);
        return (view, proj);
    }

    private void CreateShadowPipeline()
    {
        var vertSpv = LoadShader("Shaders/shadow.vert.spv");
        var fragSpv = LoadShader("Shaders/shadow.frag.spv");
        VertModule = CreateShaderModule(vertSpv);
        var fragModule = CreateShaderModule(fragSpv);

        fixed (byte* pName = "main\0"u8)
        {
            var stages = stackalloc VkPipelineShaderStageCreateInfo[2];
            stages[0] = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Vertex,
                module = VertModule,
                pName = pName,
            };
            stages[1] = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Fragment,
                module = fragModule,
                pName = pName,
            };

            var bindings = stackalloc VkVertexInputBindingDescription[1];
            bindings[0] = new VkVertexInputBindingDescription { binding = 0, stride = (uint)sizeof(Engine.Core.Vertex), inputRate = VkVertexInputRate.Vertex };

            var attributes = stackalloc VkVertexInputAttributeDescription[1];
            attributes[0] = new VkVertexInputAttributeDescription { location = 0, binding = 0, format = VkFormat.R32G32B32Sfloat, offset = 0 };

            var vertexInputState = new VkPipelineVertexInputStateCreateInfo
            {
                sType = VkStructureType.PipelineVertexInputStateCreateInfo,
                vertexBindingDescriptionCount = 1,
                pVertexBindingDescriptions = bindings,
                vertexAttributeDescriptionCount = 1,
                pVertexAttributeDescriptions = attributes,
            };

            var inputAssemblyState = new VkPipelineInputAssemblyStateCreateInfo
            {
                sType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
                topology = VkPrimitiveTopology.TriangleList,
                primitiveRestartEnable = VkBool32.False,
            };

            var viewportState = new VkPipelineViewportStateCreateInfo
            {
                sType = VkStructureType.PipelineViewportStateCreateInfo,
                viewportCount = 1,
                scissorCount = 1,
            };

            var rasterizationState = new VkPipelineRasterizationStateCreateInfo
            {
                sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                depthClampEnable = VkBool32.False,
                rasterizerDiscardEnable = VkBool32.False,
                polygonMode = VkPolygonMode.Fill,
                cullMode = VkCullModeFlags.None,
                frontFace = VkFrontFace.CounterClockwise,
                depthBiasEnable = VkBool32.True,
                lineWidth = 1.0f,
            };

            var multisampleState = new VkPipelineMultisampleStateCreateInfo
            {
                sType = VkStructureType.PipelineMultisampleStateCreateInfo,
                rasterizationSamples = VkSampleCountFlags.Count1,
            };

            var depthStencilState = new VkPipelineDepthStencilStateCreateInfo
            {
                sType = VkStructureType.PipelineDepthStencilStateCreateInfo,
                depthTestEnable = VkBool32.True,
                depthWriteEnable = VkBool32.True,
                depthCompareOp = VkCompareOp.LessOrEqual,
                depthBoundsTestEnable = VkBool32.False,
                stencilTestEnable = VkBool32.False,
            };

            var blendAttachment = new VkPipelineColorBlendAttachmentState
            {
                blendEnable = VkBool32.False,
                colorWriteMask = VkColorComponentFlags.R,
            };

            var colorBlendState = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                logicOpEnable = VkBool32.False,
                attachmentCount = 1,
                pAttachments = &blendAttachment,
            };

            var dynamicStates = stackalloc VkDynamicState[3];
            dynamicStates[0] = VkDynamicState.Viewport;
            dynamicStates[1] = VkDynamicState.Scissor;
            dynamicStates[2] = VkDynamicState.DepthBias;

            var dynamicState = new VkPipelineDynamicStateCreateInfo
            {
                sType = VkStructureType.PipelineDynamicStateCreateInfo,
                dynamicStateCount = 3,
                pDynamicStates = dynamicStates,
            };

            var pushConstantRange = new VkPushConstantRange { stageFlags = VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment, offset = 0, size = 160 };

            var layoutInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = 0,
                pushConstantRangeCount = 1,
                pPushConstantRanges = (nint)(&pushConstantRange),
            };

            var pl = VkPipelineLayout.Null;
            Vk.vkCreatePipelineLayout(_device, &layoutInfo, 0, &pl);
            PipelineLayout = pl;

            var colorFormat = VkFormat.R32Sfloat;
            var depthFormat = VkFormat.D32Sfloat;
            var renderingInfo = new VkPipelineRenderingCreateInfo
            {
                sType = VkStructureType.PipelineRenderingCreateInfo,
                colorAttachmentCount = 1,
                pColorAttachmentFormats = &colorFormat,
                depthAttachmentFormat = depthFormat,
            };

            var pipelineInfo = new VkGraphicsPipelineCreateInfo
            {
                sType = VkStructureType.GraphicsPipelineCreateInfo,
                pNext = (nint)(&renderingInfo),
                stageCount = 2,
                pStages = stages,
                pVertexInputState = &vertexInputState,
                pInputAssemblyState = &inputAssemblyState,
                pViewportState = &viewportState,
                pRasterizationState = &rasterizationState,
                pMultisampleState = &multisampleState,
                pDepthStencilState = &depthStencilState,
                pColorBlendState = &colorBlendState,
                pDynamicState = &dynamicState,
                layout = PipelineLayout,
                renderPass = new VkRenderPass { Handle = 0 },
                subpass = 0,
            };

            var pp = VkPipeline.Null;
            Vk.vkCreateGraphicsPipelines(_device, 0, 1, &pipelineInfo, 0, &pp);
            Pipeline = pp;

            Vk.vkDestroyShaderModule(_device, fragModule, 0);
        }
    }

    private VkShaderModule CreateShaderModule(byte[] spv)
    {
        fixed (byte* pCode = spv)
        {
            var info = new VkShaderModuleCreateInfo
            {
                sType = VkStructureType.ShaderModuleCreateInfo,
                codeSize = (nuint)spv.Length,
                pCode = (uint*)pCode,
            };
            var module = VkShaderModule.Null;
            Vk.vkCreateShaderModule(_device, &info, 0, &module);
            return module;
        }
    }

    private static byte[] LoadShader(string path)
    {
        if (!File.Exists(path))
        {
            var altPath = Path.Combine(AppContext.BaseDirectory, path);
            if (!File.Exists(altPath))
            {
                altPath = Path.Combine(AppContext.BaseDirectory, "Shaders", Path.GetFileName(path));
                if (!File.Exists(altPath))
                    throw new FileNotFoundException($"Shader file not found: {path}");
            }
            return File.ReadAllBytes(altPath);
        }
        return File.ReadAllBytes(path);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Pipeline.Handle != 0) Vk.vkDestroyPipeline(_device, Pipeline, 0);
        if (PipelineLayout.Handle != 0) Vk.vkDestroyPipelineLayout(_device, PipelineLayout, 0);
        if (VertModule.Handle != 0) Vk.vkDestroyShaderModule(_device, VertModule, 0);
        if (ShadowSampler.Handle != 0) Vk.vkDestroySampler(_device, ShadowSampler, 0);
        if (CubeArrayColorView.Handle != 0) Vk.vkDestroyImageView(_device, CubeArrayColorView, 0);
        for (int i = 0; i < MaxShadowLights * 6; i++)
        {
            if (FaceColorViews[i].Handle != 0) Vk.vkDestroyImageView(_device, FaceColorViews[i], 0);
            if (FaceDepthViews[i].Handle != 0) Vk.vkDestroyImageView(_device, FaceDepthViews[i], 0);
        }
        if (ColorImage.Handle != 0) Vk.vkDestroyImage(_device, ColorImage, 0);
        if (ColorImageMemory.Handle != 0) Vk.vkFreeMemory(_device, ColorImageMemory, 0);
        if (DepthImage.Handle != 0) Vk.vkDestroyImage(_device, DepthImage, 0);
        if (DepthImageMemory.Handle != 0) Vk.vkFreeMemory(_device, DepthImageMemory, 0);
    }
}
