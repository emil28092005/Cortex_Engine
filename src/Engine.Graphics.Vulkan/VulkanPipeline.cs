using System.Runtime.InteropServices;
using System.Text;

namespace Engine.Graphics.Vulkan;

public sealed unsafe class VulkanPipeline : IDisposable
{
    public VkPipelineLayout PipelineLayout;
    public VkPipeline Pipeline;
    public VkDescriptorSetLayout DescriptorSetLayout;
    public VkShaderModule VertexShader;
    public VkShaderModule FragmentShader;

    private readonly VulkanContext _ctx;
    private bool _disposed;

    public const int PushConstantSize = 128;
    public const int FrameUboSize = 16 + 16 + 16 * 4 * 8;

    public VulkanPipeline(VulkanContext ctx, VkRenderPass renderPass)
    {
        _ctx = ctx;
        Create(renderPass);
    }

    private unsafe void Create(VkRenderPass renderPass)
    {
        Console.WriteLine("[Vulkan] Loading shaders...");
        VertexShader = CreateShaderModule("Shaders/vertex.spv");
        FragmentShader = CreateShaderModule("Shaders/fragment.spv");
        Console.WriteLine("[Vulkan] Shaders loaded.");

        var mainName = Vk.AllocUtf8("main");

        var stages = new VkPipelineShaderStageCreateInfo[2];
        stages[0] = new VkPipelineShaderStageCreateInfo
        {
            sType = VkStructureType.PipelineShaderStageCreateInfo,
            pNext = null,
            flags = 0,
            stage = VkShaderStageFlags.Vertex,
            module = VertexShader,
            pName = mainName,
            pSpecializationInfo = null
        };
        stages[1] = new VkPipelineShaderStageCreateInfo
        {
            sType = VkStructureType.PipelineShaderStageCreateInfo,
            pNext = null,
            flags = 0,
            stage = VkShaderStageFlags.Fragment,
            module = FragmentShader,
            pName = mainName,
            pSpecializationInfo = null
        };

        var bindingDesc = new VkVertexInputBindingDescription
        {
            binding = 0,
            stride = 36,
            inputRate = 0
        };

        var attrDescs = stackalloc VkVertexInputAttributeDescription[3];
        attrDescs[0] = new VkVertexInputAttributeDescription { location = 0, binding = 0, format = VkFormat.R32G32B32Sfloat, offset = 0 };
        attrDescs[1] = new VkVertexInputAttributeDescription { location = 1, binding = 0, format = VkFormat.R32G32B32Sfloat, offset = 12 };
        attrDescs[2] = new VkVertexInputAttributeDescription { location = 2, binding = 0, format = VkFormat.R32G32B32Sfloat, offset = 24 };

        VkPipelineVertexInputStateCreateInfo vertexInputState;
        vertexInputState.sType = VkStructureType.PipelineVertexInputStateCreateInfo;
        vertexInputState.pNext = null;
        vertexInputState.flags = 0;
        vertexInputState.vertexBindingDescriptionCount = 1;
        vertexInputState.pVertexBindingDescriptions = &bindingDesc;
        vertexInputState.vertexAttributeDescriptionCount = 3;
        vertexInputState.pVertexAttributeDescriptions = attrDescs;

        var inputAssemblyState = new VkPipelineInputAssemblyStateCreateInfo
        {
            sType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
            pNext = null,
            flags = 0,
            topology = VkPrimitiveTopology.TriangleList,
            primitiveRestartEnable = 0
        };

        var viewport = new VkViewport { x = 0, y = 0, width = 0, height = 0, minDepth = 0, maxDepth = 1 };
        var scissor = new VkRect2D { offset = new VkOffset2D { x = 0, y = 0 }, extent = new VkExtent2D { width = 0, height = 0 } };

        VkPipelineViewportStateCreateInfo viewportState;
        viewportState.sType = VkStructureType.PipelineViewportStateCreateInfo;
        viewportState.pNext = null;
        viewportState.flags = 0;
        viewportState.viewportCount = 1;
        viewportState.pViewports = &viewport;
        viewportState.scissorCount = 1;
        viewportState.pScissors = &scissor;

        var rasterizationState = new VkPipelineRasterizationStateCreateInfo
        {
            sType = VkStructureType.PipelineRasterizationStateCreateInfo,
            pNext = null,
            flags = 0,
            depthClampEnable = 0,
            rasterizerDiscardEnable = 0,
            polygonMode = VkPolygonMode.Fill,
            cullMode = VkCullModeFlags.None,
            frontFace = VkFrontFace.Clockwise,
            depthBiasEnable = 0,
            depthBiasConstantFactor = 0,
            depthBiasClamp = 0,
            depthBiasSlopeFactor = 0,
            lineWidth = 1.0f
        };

        var multisampleState = new VkPipelineMultisampleStateCreateInfo
        {
            sType = VkStructureType.PipelineMultisampleStateCreateInfo,
            pNext = null,
            flags = 0,
            rasterizationSamples = VkSampleCountFlags.One,
            sampleShadingEnable = 0,
            minSampleShading = 0,
            pSampleMask = null,
            alphaToCoverageEnable = 0,
            alphaToOneEnable = 0
        };

        var depthStencilState = new VkPipelineDepthStencilStateCreateInfo
        {
            sType = VkStructureType.PipelineDepthStencilStateCreateInfo,
            pNext = null,
            flags = 0,
            depthTestEnable = 1,
            depthWriteEnable = 1,
            depthCompareOp = VkCompareOp.Less,
            depthBoundsTestEnable = 0,
            stencilTestEnable = 0,
            front = new VkStencilOpState(),
            back = new VkStencilOpState(),
            minDepthBounds = 0,
            maxDepthBounds = 1
        };

        var blendAttachment = new VkPipelineColorBlendAttachmentState
        {
            blendEnable = 0,
            srcColorBlendFactor = VkBlendFactor.One,
            dstColorBlendFactor = VkBlendFactor.Zero,
            colorBlendOp = VkBlendOp.Add,
            srcAlphaBlendFactor = VkBlendFactor.One,
            dstAlphaBlendFactor = VkBlendFactor.Zero,
            alphaBlendOp = VkBlendOp.Add,
            colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
        };

        VkPipelineColorBlendStateCreateInfo colorBlendState = default;
        colorBlendState.sType = VkStructureType.PipelineColorBlendStateCreateInfo;
        colorBlendState.pNext = null;
        colorBlendState.flags = 0;
        colorBlendState.logicOpEnable = 0;
        colorBlendState.logicOp = 0;
        colorBlendState.attachmentCount = 1;
        colorBlendState.pAttachments = &blendAttachment;

        var uboBinding = new VkDescriptorSetLayoutBinding
        {
            binding = 0,
            descriptorType = VkDescriptorType.UniformBuffer,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment,
            pImmutableSamplers = null
        };

        Console.WriteLine("[Vulkan] Creating descriptor set layout...");
        VkDescriptorSetLayoutCreateInfo dsLayoutInfo;
        dsLayoutInfo.sType = VkStructureType.DescriptorSetLayoutCreateInfo;
        dsLayoutInfo.pNext = null;
        dsLayoutInfo.flags = 0;
        dsLayoutInfo.bindingCount = 1;
        dsLayoutInfo.pBindings = &uboBinding;

        VkDescriptorSetLayout dsLayout;
        VkResult result = Vk.vkCreateDescriptorSetLayout(_ctx.Device, &dsLayoutInfo, null, &dsLayout);
        Vk.CheckResult(result, "vkCreateDescriptorSetLayout");
        DescriptorSetLayout = dsLayout;
        Console.WriteLine("[Vulkan] Descriptor set layout created.");

        var pushConstantRange = new VkPushConstantRange
        {
            stageFlags = VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment,
            offset = 0,
            size = PushConstantSize
        };

        Console.WriteLine("[Vulkan] Creating pipeline layout...");
        VkPipelineLayoutCreateInfo layoutInfo;
        layoutInfo.sType = VkStructureType.PipelineLayoutCreateInfo;
        layoutInfo.pNext = null;
        layoutInfo.flags = 0;
        layoutInfo.setLayoutCount = 1;
        layoutInfo.pSetLayouts = &dsLayout;
        layoutInfo.pushConstantRangeCount = 1;
        layoutInfo.pPushConstantRanges = &pushConstantRange;

        VkPipelineLayout pipeLayout;
        result = Vk.vkCreatePipelineLayout(_ctx.Device, &layoutInfo, null, &pipeLayout);
        Vk.CheckResult(result, "vkCreatePipelineLayout");
        PipelineLayout = pipeLayout;
        Console.WriteLine("[Vulkan] Pipeline layout created.");

        Console.WriteLine("[Vulkan] Creating graphics pipeline...");
        fixed (VkPipelineShaderStageCreateInfo* pStages = stages)
        {
            VkGraphicsPipelineCreateInfo pipelineInfo;
            pipelineInfo.sType = VkStructureType.GraphicsPipelineCreateInfo;
            pipelineInfo.pNext = null;
            pipelineInfo.flags = 0;
            pipelineInfo.stageCount = 2;
            pipelineInfo.pStages = pStages;
            pipelineInfo.pVertexInputState = &vertexInputState;
            pipelineInfo.pInputAssemblyState = &inputAssemblyState;
            pipelineInfo.pTessellationState = null;
            pipelineInfo.pViewportState = &viewportState;
            pipelineInfo.pRasterizationState = &rasterizationState;
            pipelineInfo.pMultisampleState = &multisampleState;
            pipelineInfo.pDepthStencilState = &depthStencilState;
            pipelineInfo.pColorBlendState = &colorBlendState;
            pipelineInfo.pDynamicState = null;
            pipelineInfo.layout = pipeLayout;
            pipelineInfo.renderPass = renderPass;
            pipelineInfo.subpass = 0;
            pipelineInfo.basePipelineHandle = default;
            pipelineInfo.basePipelineIndex = -1;

            VkPipeline pipe;
            result = Vk.vkCreateGraphicsPipelines(_ctx.Device, 0, 1, &pipelineInfo, null, &pipe);
            Vk.CheckResult(result, "vkCreateGraphicsPipelines");
            Pipeline = pipe;
        }

        Vk.FreeUtf8(mainName);

        Console.WriteLine("[Vulkan] Graphics pipeline created.");
    }

    private VkShaderModule CreateShaderModule(string path)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"SPIR-V shader not found: {fullPath}");

        var code = File.ReadAllBytes(fullPath);
        var codeSize = (ulong)code.Length;

        fixed (byte* pCode = code)
        {
            VkShaderModuleCreateInfo createInfo;
            createInfo.sType = VkStructureType.ShaderModuleCreateInfo;
            createInfo.pNext = null;
            createInfo.flags = 0;
            createInfo.codeSize = codeSize;
            createInfo.pCode = (uint*)pCode;

            VkShaderModule module;
            var result = Vk.vkCreateShaderModule(_ctx.Device, &createInfo, null, &module);
            Vk.CheckResult(result, $"vkCreateShaderModule ({path})");
            return module;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Pipeline.Value != 0) Vk.vkDestroyPipeline(_ctx.Device, Pipeline, null);
        if (PipelineLayout.Value != 0) Vk.vkDestroyPipelineLayout(_ctx.Device, PipelineLayout, null);
        if (DescriptorSetLayout.Value != 0) Vk.vkDestroyDescriptorSetLayout(_ctx.Device, DescriptorSetLayout, null);
        if (VertexShader.Value != 0) Vk.vkDestroyShaderModule(_ctx.Device, VertexShader, null);
        if (FragmentShader.Value != 0) Vk.vkDestroyShaderModule(_ctx.Device, FragmentShader, null);
    }
}
