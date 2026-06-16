using System;
using Vortice.Vulkan;

namespace Engine.Graphics;

/// <summary>
/// A simple graphics pipeline for a single vertex/fragment shader pair.
/// Assumes a triangle with vec2 position + vec3 color per vertex.
/// </summary>
public sealed unsafe class VulkanPipeline : IDisposable
{
    private readonly VulkanContext _context;
    private readonly Swapchain _swapchain;

    public VkPipeline Handle { get; }
    public VkPipelineLayout Layout { get; }
    private readonly VkShaderModule _vertexModule;
    private readonly VkShaderModule _fragmentModule;

    public VulkanPipeline(VulkanContext context, Swapchain swapchain)
    {
        _context = context;
        _swapchain = swapchain;

        _vertexModule = CreateShaderModule("vertex.spv");
        _fragmentModule = CreateShaderModule("fragment.spv");

        Layout = CreatePipelineLayout();
        Handle = CreateGraphicsPipeline();
    }

    private VkShaderModule CreateShaderModule(string resourceName)
    {
        var code = ShaderLoader.Load(resourceName);
        if (code.Length % 4 != 0)
            throw new InvalidOperationException($"Shader {resourceName} size is not a multiple of 4.");

        fixed (byte* pCode = code)
        {
            var createInfo = new VkShaderModuleCreateInfo
            {
                sType = VkStructureType.ShaderModuleCreateInfo,
                codeSize = (nuint)code.Length,
                pCode = (uint*)pCode
            };

            var result = _context.DeviceApi.vkCreateShaderModule(&createInfo, null, out var module);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateShaderModule failed for {resourceName}: {result}");
            return module;
        }
    }

    private VkPipelineLayout CreatePipelineLayout()
    {
        var createInfo = new VkPipelineLayoutCreateInfo
        {
            sType = VkStructureType.PipelineLayoutCreateInfo,
            setLayoutCount = 0,
            pushConstantRangeCount = 0
        };

        var result = _context.DeviceApi.vkCreatePipelineLayout(&createInfo, null, out var layout);
        if (result != VkResult.Success)
            throw new InvalidOperationException($"vkCreatePipelineLayout failed: {result}");
        return layout;
    }

    private VkPipeline CreateGraphicsPipeline()
    {
        var stages = new[]
        {
            new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Vertex,
                module = _vertexModule,
                pName = VkStringInterop.ConvertToUnmanaged("main")
            },
            new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Fragment,
                module = _fragmentModule,
                pName = VkStringInterop.ConvertToUnmanaged("main")
            }
        };

        var bindingDescription = new VkVertexInputBindingDescription
        {
            binding = 0,
            stride = (uint)(5 * sizeof(float)),
            inputRate = VkVertexInputRate.Vertex
        };

        var attributeDescriptions = new[]
        {
            new VkVertexInputAttributeDescription
            {
                binding = 0,
                location = 0,
                format = VkFormat.R32G32Sfloat,
                offset = 0
            },
            new VkVertexInputAttributeDescription
            {
                binding = 0,
                location = 1,
                format = VkFormat.R32G32B32Sfloat,
                offset = (uint)(2 * sizeof(float))
            }
        };

        VkPipelineVertexInputStateCreateInfo vertexInputInfo;
        fixed (VkVertexInputAttributeDescription* pAttributes = attributeDescriptions)
        {
            vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
            {
                sType = VkStructureType.PipelineVertexInputStateCreateInfo,
                vertexBindingDescriptionCount = 1,
                pVertexBindingDescriptions = &bindingDescription,
                vertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                pVertexAttributeDescriptions = pAttributes
            };
        }

        var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo
        {
            sType = VkStructureType.PipelineInputAssemblyStateCreateInfo,
            topology = VkPrimitiveTopology.TriangleList,
            primitiveRestartEnable = false
        };

        var viewport = new VkViewport(0, 0, _swapchain.Extent.width, _swapchain.Extent.height, 0, 1);
        var scissor = new VkRect2D(0, 0, _swapchain.Extent.width, _swapchain.Extent.height);

        var viewportState = new VkPipelineViewportStateCreateInfo
        {
            sType = VkStructureType.PipelineViewportStateCreateInfo,
            viewportCount = 1,
            pViewports = &viewport,
            scissorCount = 1,
            pScissors = &scissor
        };

        var rasterizer = new VkPipelineRasterizationStateCreateInfo
        {
            sType = VkStructureType.PipelineRasterizationStateCreateInfo,
            polygonMode = VkPolygonMode.Fill,
            cullMode = VkCullModeFlags.None,
            frontFace = VkFrontFace.Clockwise,
            lineWidth = 1.0f
        };

        var multisampling = new VkPipelineMultisampleStateCreateInfo
        {
            sType = VkStructureType.PipelineMultisampleStateCreateInfo,
            rasterizationSamples = VkSampleCountFlags.Count1,
            sampleShadingEnable = false
        };

        var colorBlendAttachment = new VkPipelineColorBlendAttachmentState
        {
            colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A
        };

        var colorBlending = new VkPipelineColorBlendStateCreateInfo
        {
            sType = VkStructureType.PipelineColorBlendStateCreateInfo,
            attachmentCount = 1,
            pAttachments = &colorBlendAttachment
        };

        var dynamicStates = new[] { VkDynamicState.Viewport, VkDynamicState.Scissor };
        VkPipelineDynamicStateCreateInfo dynamicState;
        fixed (VkDynamicState* pDynamic = dynamicStates)
        {
            dynamicState = new VkPipelineDynamicStateCreateInfo
            {
                sType = VkStructureType.PipelineDynamicStateCreateInfo,
                dynamicStateCount = (uint)dynamicStates.Length,
                pDynamicStates = pDynamic
            };
        }

        VkPipeline pipeline;
        fixed (VkPipelineShaderStageCreateInfo* pStages = stages)
        {
            var createInfo = new VkGraphicsPipelineCreateInfo
            {
                sType = VkStructureType.GraphicsPipelineCreateInfo,
                stageCount = (uint)stages.Length,
                pStages = pStages,
                pVertexInputState = &vertexInputInfo,
                pInputAssemblyState = &inputAssembly,
                pViewportState = &viewportState,
                pRasterizationState = &rasterizer,
                pMultisampleState = &multisampling,
                pColorBlendState = &colorBlending,
                pDynamicState = &dynamicState,
                layout = Layout,
                renderPass = _swapchain.RenderPass,
                subpass = 0
            };

            var result = _context.DeviceApi.vkCreateGraphicsPipelines(VkPipelineCache.Null, 1, &createInfo, null, &pipeline);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateGraphicsPipelines failed: {result}");
        }

        VkStringInterop.Free(stages[0].pName);
        VkStringInterop.Free(stages[1].pName);

        return pipeline;
    }

    public void Dispose()
    {
        _context.DeviceApi.vkDeviceWaitIdle();
        _context.DeviceApi.vkDestroyPipeline(Handle);
        _context.DeviceApi.vkDestroyPipelineLayout(Layout);
        _context.DeviceApi.vkDestroyShaderModule(_vertexModule);
        _context.DeviceApi.vkDestroyShaderModule(_fragmentModule);
    }
}
