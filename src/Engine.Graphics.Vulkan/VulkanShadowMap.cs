using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanShadowMap : IDisposable
{
    public const uint ShadowMapSize = 2048;

    public VkImage DepthImage;
    public VkDeviceMemory DepthImageMemory;
    public VkImageView DepthImageView;
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

        CreateShadowImage();
        CreateShadowSampler();
        CreateShadowPipeline(descLayout);

        Console.WriteLine("[Vulkan] Shadow map created (2048x2048 D32_SFLOAT)");
    }

    private void CreateShadowImage()
    {
        var imageInfo = new VkImageCreateInfo
        {
            sType = VkStructureType.ImageCreateInfo,
            imageType = VkImageType.Type2D,
            format = VkFormat.D32Sfloat,
            extent = new VkExtent3D { Width = ShadowMapSize, Height = ShadowMapSize, Depth = 1 },
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = VkImageUsageFlags.DepthStencilAttachment | VkImageUsageFlags.Sampled,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
        };

        var img = VkImage.Null;
        Vk.vkCreateImage(_device, &imageInfo, 0, &img);
        DepthImage = img;

        var reqs = new VkMemoryRequirements();
        Vk.vkGetImageMemoryRequirements(_device, DepthImage, &reqs);
        var memTypeIndex = _ctx.FindMemoryType(reqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

        var allocInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = reqs.size,
            memoryTypeIndex = memTypeIndex,
        };

        var mem = VkDeviceMemory.Null;
        Vk.vkAllocateMemory(_device, &allocInfo, 0, &mem);
        DepthImageMemory = mem;
        Vk.vkBindImageMemory(_device, DepthImage, DepthImageMemory, 0);

        var viewInfo = new VkImageViewCreateInfo
        {
            sType = VkStructureType.ImageViewCreateInfo,
            image = DepthImage,
            viewType = VkImageViewType.Type2D,
            format = VkFormat.D32Sfloat,
            components = new VkComponentMapping { R = VkComponentSwizzle.Identity, G = VkComponentSwizzle.Identity, B = VkComponentSwizzle.Identity, A = VkComponentSwizzle.Identity },
            subresourceRange = new VkImageSubresourceRange { AspectMask = VkImageAspectFlags.Depth, BaseMipLevel = 0, LevelCount = 1, BaseArrayLayer = 0, LayerCount = 1 },
        };

        var view = VkImageView.Null;
        Vk.vkCreateImageView(_device, &viewInfo, 0, &view);
        DepthImageView = view;
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
            borderColor = 1, // VK_BORDER_COLOR_FLOAT_OPAQUE_WHITE
        };

        var samp = VkSampler.Null;
        Vk.vkCreateSampler(_device, &samplerInfo, 0, &samp);
        ShadowSampler = samp;
    }

    private void CreateShadowPipeline(VkDescriptorSetLayout descLayout)
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

            var colorBlendState = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                logicOpEnable = VkBool32.False,
                attachmentCount = 0,
                pAttachments = null,
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

            var pushConstantRanges = stackalloc VkPushConstantRange[2];
            pushConstantRanges[0] = new VkPushConstantRange { stageFlags = VkShaderStageFlags.Vertex, offset = 0, size = 64 };
            pushConstantRanges[1] = new VkPushConstantRange { stageFlags = VkShaderStageFlags.Fragment, offset = 64, size = 96 };

            var layoutInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = 0,
                pushConstantRangeCount = 2,
                pPushConstantRanges = (nint)pushConstantRanges,
            };

            var pl = VkPipelineLayout.Null;
            Vk.vkCreatePipelineLayout(_device, &layoutInfo, 0, &pl);
            PipelineLayout = pl;

            var depthFormat = VkFormat.D32Sfloat;
            var renderingInfo = new VkPipelineRenderingCreateInfo
            {
                sType = VkStructureType.PipelineRenderingCreateInfo,
                colorAttachmentCount = 0,
                pColorAttachmentFormats = null,
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
        if (DepthImageView.Handle != 0) Vk.vkDestroyImageView(_device, DepthImageView, 0);
        if (DepthImage.Handle != 0) Vk.vkDestroyImage(_device, DepthImage, 0);
        if (DepthImageMemory.Handle != 0) Vk.vkFreeMemory(_device, DepthImageMemory, 0);
    }
}
