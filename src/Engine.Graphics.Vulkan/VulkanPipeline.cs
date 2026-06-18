using System.Runtime.InteropServices;
using Engine.Core;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanPipeline : IDisposable
{
    public VkPipelineLayout PipelineLayout;
    public VkPipeline Pipeline;
    public VkShaderModule VertModule;
    public VkShaderModule FragModule;
    public VkDescriptorSetLayout DescriptorSetLayout;

    private readonly VkDevice _device;
    private bool _disposed;

    public VulkanPipeline(VkDevice device, VkFormat colorFormat, VkFormat depthFormat, byte[] vertSpv, byte[] fragSpv)
    {
        _device = device;
        VertModule = CreateShaderModule(vertSpv);
        FragModule = CreateShaderModule(fragSpv);

        CreateDescriptorSetLayout();

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
                module = FragModule,
                pName = pName,
            };

            var bindings = stackalloc VkVertexInputBindingDescription[1];
            bindings[0] = new VkVertexInputBindingDescription
            {
                binding = 0,
                stride = (uint)sizeof(Vertex),
                inputRate = VkVertexInputRate.Vertex,
            };

            var attributes = stackalloc VkVertexInputAttributeDescription[3];
            attributes[0] = new VkVertexInputAttributeDescription
            {
                location = 0,
                binding = 0,
                format = VkFormat.R32G32B32Sfloat,
                offset = 0,
            };
            attributes[1] = new VkVertexInputAttributeDescription
            {
                location = 1,
                binding = 0,
                format = VkFormat.R32G32B32Sfloat,
                offset = 12,
            };
            attributes[2] = new VkVertexInputAttributeDescription
            {
                location = 2,
                binding = 0,
                format = VkFormat.R32G32B32Sfloat,
                offset = 24,
            };

            var vertexInputState = new VkPipelineVertexInputStateCreateInfo
            {
                sType = VkStructureType.PipelineVertexInputStateCreateInfo,
                vertexBindingDescriptionCount = 1,
                pVertexBindingDescriptions = bindings,
                vertexAttributeDescriptionCount = 3,
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
                pViewports = null,
                scissorCount = 1,
                pScissors = null,
            };

            var rasterizationState = new VkPipelineRasterizationStateCreateInfo
            {
                sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                depthClampEnable = VkBool32.False,
                rasterizerDiscardEnable = VkBool32.False,
                polygonMode = VkPolygonMode.Fill,
                cullMode = VkCullModeFlags.None,
                frontFace = VkFrontFace.CounterClockwise,
                depthBiasEnable = VkBool32.False,
                lineWidth = 1.0f,
            };

            var multisampleState = new VkPipelineMultisampleStateCreateInfo
            {
                sType = VkStructureType.PipelineMultisampleStateCreateInfo,
                rasterizationSamples = VkSampleCountFlags.Count1,
                sampleShadingEnable = VkBool32.False,
            };

            var depthStencilState = new VkPipelineDepthStencilStateCreateInfo
            {
                sType = VkStructureType.PipelineDepthStencilStateCreateInfo,
                depthTestEnable = VkBool32.True,
                depthWriteEnable = VkBool32.True,
                depthCompareOp = VkCompareOp.Less,
                depthBoundsTestEnable = VkBool32.False,
                stencilTestEnable = VkBool32.False,
            };

            var blendAttachment = new VkPipelineColorBlendAttachmentState
            {
                blendEnable = VkBool32.False,
                colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A,
            };

            var colorBlendState = new VkPipelineColorBlendStateCreateInfo
            {
                sType = VkStructureType.PipelineColorBlendStateCreateInfo,
                logicOpEnable = VkBool32.False,
                attachmentCount = 1,
                pAttachments = &blendAttachment,
            };

            var dynamicStates = stackalloc VkDynamicState[2];
            dynamicStates[0] = VkDynamicState.Viewport;
            dynamicStates[1] = VkDynamicState.Scissor;

            var dynamicState = new VkPipelineDynamicStateCreateInfo
            {
                sType = VkStructureType.PipelineDynamicStateCreateInfo,
                dynamicStateCount = 2,
                pDynamicStates = dynamicStates,
            };

            var pushConstantRange = new VkPushConstantRange
            {
                stageFlags = VkShaderStageFlags.Vertex | VkShaderStageFlags.Fragment,
                offset = 0,
                size = 160,
            };

            var descLayout = DescriptorSetLayout;
            var layoutInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = 1,
                pSetLayouts = &descLayout,
                pushConstantRangeCount = 1,
                pPushConstantRanges = (nint)(&pushConstantRange),
            };

            fixed (VkPipelineLayout* layoutPtr = &PipelineLayout)
            {
                var result = Vk.vkCreatePipelineLayout(_device, &layoutInfo, 0, layoutPtr);
                if (result != VkResult.Success)
                    throw new InvalidOperationException($"vkCreatePipelineLayout failed: {result}");
            }

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

            fixed (VkPipeline* pipePtr = &Pipeline)
            {
                var result = Vk.vkCreateGraphicsPipelines(_device, 0, 1, &pipelineInfo, 0, pipePtr);
                if (result != VkResult.Success)
                    throw new InvalidOperationException($"vkCreateGraphicsPipelines failed: {result}");
            }
        }

        Console.WriteLine("[Vulkan] Graphics pipeline created (dynamic rendering)");
    }

    private void CreateDescriptorSetLayout()
    {
        var bindings = stackalloc VkDescriptorSetLayoutBinding[2];
        bindings[0] = new VkDescriptorSetLayoutBinding
        {
            binding = 0,
            descriptorType = VkDescriptorType.UniformBuffer,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Vertex,
        };
        bindings[1] = new VkDescriptorSetLayoutBinding
        {
            binding = 1,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Fragment,
        };

        var info = new VkDescriptorSetLayoutCreateInfo
        {
            sType = VkStructureType.DescriptorSetLayoutCreateInfo,
            bindingCount = 2,
            pBindings = bindings,
        };

        var descLayout = VkDescriptorSetLayout.Null;
        var result = Vk.vkCreateDescriptorSetLayout(_device, &info, 0, &descLayout);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreateDescriptorSetLayout failed: {result}");
        DescriptorSetLayout = descLayout;
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
            var result = Vk.vkCreateShaderModule(_device, &info, 0, &module);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateShaderModule failed: {result}");
            return module;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Pipeline.Handle != 0) Vk.vkDestroyPipeline(_device, Pipeline, 0);
        if (PipelineLayout.Handle != 0) Vk.vkDestroyPipelineLayout(_device, PipelineLayout, 0);
        if (DescriptorSetLayout.Handle != 0) Vk.vkDestroyDescriptorSetLayout(_device, DescriptorSetLayout, 0);
        if (FragModule.Handle != 0) Vk.vkDestroyShaderModule(_device, FragModule, 0);
        if (VertModule.Handle != 0) Vk.vkDestroyShaderModule(_device, VertModule, 0);
    }
}
