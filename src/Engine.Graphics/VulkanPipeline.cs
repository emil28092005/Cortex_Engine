using System;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace Engine.Graphics;

/// <summary>
/// Graphics pipeline for indexed meshes with push-constant MVP and depth testing.
/// Uses Silk.NET.Vulkan.
/// </summary>
public sealed unsafe class VulkanPipeline : IDisposable
{
    private readonly VulkanContext _context;
    private readonly Swapchain _swapchain;

    public Pipeline Handle { get; }
    public PipelineLayout Layout { get; }
    private readonly ShaderModule _vertexModule;
    private readonly ShaderModule _fragmentModule;

    public VulkanPipeline(VulkanContext context, Swapchain swapchain)
    {
        _context = context;
        _swapchain = swapchain;

        _vertexModule = CreateShaderModule("vertex.spv");
        _fragmentModule = CreateShaderModule("fragment.spv");

        Layout = CreatePipelineLayout();
        Handle = CreateGraphicsPipeline();
    }

    private ShaderModule CreateShaderModule(string resourceName)
    {
        var code = ShaderLoader.Load(resourceName);
        if (code.Length % 4 != 0)
            throw new InvalidOperationException($"Shader {resourceName} size is not a multiple of 4.");

        fixed (byte* pCode = code)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
                PCode = (uint*)pCode
            };

            ShaderModule module;
            var result = _context.Vk.CreateShaderModule(_context.Device, &createInfo, null, &module);
            if (result != Result.Success)
                throw new InvalidOperationException($"vkCreateShaderModule failed for {resourceName}: {result}");
            return module;
        }
    }

    private PipelineLayout CreatePipelineLayout()
    {
        var pushConstantRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            Offset = 0,
            Size = (uint)(32 * sizeof(float))
        };

        var createInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PushConstantRangeCount = 1,
            PPushConstantRanges = &pushConstantRange
        };

        PipelineLayout layout;
        var result = _context.Vk.CreatePipelineLayout(_context.Device, &createInfo, null, &layout);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreatePipelineLayout failed: {result}");
        return layout;
    }

    private Pipeline CreateGraphicsPipeline()
    {
        var entryName = SilkMarshal.StringToPtr("main", NativeStringEncoding.UTF8);
        var stages = new[]
        {
            new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = _vertexModule,
                PName = (byte*)entryName
            },
            new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = _fragmentModule,
                PName = (byte*)entryName
            }
        };

        var bindingDescription = new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint)(9 * sizeof(float)),
            InputRate = VertexInputRate.Vertex
        };

        var attributeDescriptions = new[]
        {
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = 0
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)(3 * sizeof(float))
            },
            new VertexInputAttributeDescription
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)(6 * sizeof(float))
            }
        };

        PipelineVertexInputStateCreateInfo vertexInputInfo;
        fixed (VertexInputAttributeDescription* pAttributes = attributeDescriptions)
        {
            vertexInputInfo = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                PVertexBindingDescriptions = &bindingDescription,
                VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                PVertexAttributeDescriptions = pAttributes
            };
        }

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false
        };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1
        };

        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            PolygonMode = PolygonMode.Fill,
            CullMode = CullModeFlags.None,
            FrontFace = FrontFace.Clockwise,
            LineWidth = 1.0f
        };

        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            RasterizationSamples = SampleCountFlags.Count1Bit,
            SampleShadingEnable = false
        };

        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
        };

        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };

        var depthStencil = new PipelineDepthStencilStateCreateInfo
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = CompareOp.Less,
            DepthBoundsTestEnable = false,
            StencilTestEnable = false,
            Back = new StencilOpState(),
            Front = new StencilOpState()
        };

        var dynamicStates = new[] { DynamicState.Viewport, DynamicState.Scissor };
        PipelineDynamicStateCreateInfo dynamicState;
        fixed (DynamicState* pDynamic = dynamicStates)
        {
            dynamicState = new PipelineDynamicStateCreateInfo
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = (uint)dynamicStates.Length,
                PDynamicStates = pDynamic
            };
        }

        Pipeline pipeline;
        fixed (PipelineShaderStageCreateInfo* pStages = stages)
        {
            var createInfo = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = (uint)stages.Length,
                PStages = pStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multisampling,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                PDynamicState = &dynamicState,
                Layout = Layout,
                RenderPass = _swapchain.RenderPass,
                Subpass = 0
            };

            var result = _context.Vk.CreateGraphicsPipelines(_context.Device, default, 1, &createInfo, null, &pipeline);
            if (result != Result.Success)
                throw new InvalidOperationException($"vkCreateGraphicsPipelines failed: {result}");
        }

        SilkMarshal.FreeString(entryName, NativeStringEncoding.UTF8);
        return pipeline;
    }

    public void Dispose()
    {
        _context.Vk.DeviceWaitIdle(_context.Device);
        _context.Vk.DestroyPipeline(_context.Device, Handle, null);
        _context.Vk.DestroyPipelineLayout(_context.Device, Layout, null);
        _context.Vk.DestroyShaderModule(_context.Device, _vertexModule, null);
        _context.Vk.DestroyShaderModule(_context.Device, _fragmentModule, null);
    }
}
