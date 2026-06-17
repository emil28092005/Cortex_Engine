using System.Runtime.InteropServices;
using System.Numerics;
using ImGuiNET;

namespace Engine.Graphics.Vulkan;

public sealed unsafe class VulkanImGui : IDisposable
{
    private readonly VulkanContext _ctx;
    private readonly VulkanSwapchain _swapchain;
    private VkCommandPool _initCommandPool;
    private VkPipeline _pipeline;
    private VkPipelineLayout _pipelineLayout;
    private VkDescriptorSetLayout _descriptorSetLayout;
    private VkDescriptorPool _descriptorPool;
    private VkDescriptorSet _descriptorSet;
    private VkShaderModule _vertexShader;
    private VkShaderModule _fragmentShader;
    private VkImage _fontImage;
    private VkDeviceMemory _fontImageMemory;
    private VkImageView _fontImageView;
    private VkSampler _fontSampler;
    private VulkanBuffer? _vertexBuffer;
    private VulkanBuffer? _indexBuffer;

    private static readonly byte[] VkDescriptorWriteDummy = new byte[1];

    public VulkanImGui(VulkanContext ctx, VulkanSwapchain swapchain)
    {
        _ctx = ctx;
        _swapchain = swapchain;
        Initialize();
    }

    private unsafe void Initialize()
    {
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.Fonts.Build();

        VkCommandPoolCreateInfo poolInfo = default;
        poolInfo.sType = VkStructureType.CommandPoolCreateInfo;
        poolInfo.flags = 0x00000002;
        poolInfo.queueFamilyIndex = _ctx.GraphicsFamily;
        VkCommandPool pool;
        Vk.CheckResult(Vk.vkCreateCommandPool(_ctx.Device, &poolInfo, null, &pool), "vkCreateCommandPool (ImGui init)");
        _initCommandPool = pool;

        CreateFontTexture();
        CreateShaders();
        CreateDescriptorSetLayout();
        CreatePipelineLayout();
        CreatePipeline();
        CreateDescriptorPoolAndSet();

        Vk.vkDestroyCommandPool(_ctx.Device, _initCommandPool, null);
    }

    private unsafe void CreateFontTexture()
    {
        var io = ImGui.GetIO();
        int width, height, bpp;
        byte* pixels;
        io.Fonts.GetTexDataAsRGBA32(out pixels, out width, out height, out bpp);

        VkImageCreateInfo imageInfo = default;
        imageInfo.sType = VkStructureType.ImageCreateInfo;
        imageInfo.imageType = VkImageType._2D;
        imageInfo.format = VkFormat.R8G8B8A8Unorm;
        imageInfo.extent = new VkExtent3D { width = width, height = height, depth = 1 };
        imageInfo.mipLevels = 1;
        imageInfo.arrayLayers = 1;
        imageInfo.samples = VkSampleCountFlags.One;
        imageInfo.tiling = 0;
        imageInfo.usage = VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferDst;
        imageInfo.sharingMode = VkSharingMode.Exclusive;
        imageInfo.initialLayout = 0;

        VkImage fontImage;
        Vk.CheckResult(Vk.vkCreateImage(_ctx.Device, &imageInfo, null, &fontImage), "vkCreateImage (font)");
        _fontImage = fontImage;

        VkMemoryRequirements2 memReq;
        Vk.vkGetImageMemoryRequirements(_ctx.Device, _fontImage, &memReq);

        VkMemoryAllocateInfo allocInfo = default;
        allocInfo.sType = VkStructureType.MemoryAllocateInfo;
        allocInfo.allocationSize = memReq.size;
        allocInfo.memoryTypeIndex = _ctx.FindMemoryType(memReq.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

        VkDeviceMemory fontMem;
        Vk.CheckResult(Vk.vkAllocateMemory(_ctx.Device, &allocInfo, null, &fontMem), "vkAllocateMemory (font)");
        _fontImageMemory = fontMem;
        Vk.CheckResult(Vk.vkBindImageMemory(_ctx.Device, _fontImage, _fontImageMemory, 0), "vkBindImageMemory (font)");

        var imageSize = (ulong)(width * height * 4);
        var staging = new VulkanBuffer(_ctx, imageSize,
            VkBufferUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

        Buffer.MemoryCopy(pixels, staging.MappedData, imageSize, imageSize);

        var cmd = VulkanBuffer.BeginSingleTimeCommands(_ctx, _initCommandPool);

        var barrier = new VkImageMemoryBarrier
        {
            sType = VkStructureType.ImageMemoryBarrier,
            srcAccessMask = 0,
            dstAccessMask = VkAccessFlags.TransferWrite,
            oldLayout = VkImageLayout.Undefined,
            newLayout = VkImageLayout.TransferDstOptimal,
            srcQueueFamilyIndex = ~0u,
            dstQueueFamilyIndex = ~0u,
            image = _fontImage,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1
            }
        };

        Vk.vkCmdPipelineBarrier(cmd, VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer,
            0, 0, null, 0, null, 1, &barrier);

        var region = new VkBufferImageCopy
        {
            bufferOffset = 0,
            bufferRowLength = (uint)width,
            bufferImageHeight = (uint)height,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = 0, baseArrayLayer = 0, layerCount = 1
            },
            imageOffset = new VkOffset3D { x = 0, y = 0, z = 0 },
            imageExtent = new VkExtent3D { width = width, height = height, depth = 1 }
        };

        Vk.vkCmdCopyBufferToImage(cmd, staging.Buffer, _fontImage,
            (int)VkImageLayout.TransferDstOptimal, 1, &region);

        var barrier2 = new VkImageMemoryBarrier
        {
            sType = VkStructureType.ImageMemoryBarrier,
            srcAccessMask = VkAccessFlags.TransferWrite,
            dstAccessMask = VkAccessFlags.ShaderRead,
            oldLayout = VkImageLayout.TransferDstOptimal,
            newLayout = VkImageLayout.ShaderReadOnlyOptimal,
            srcQueueFamilyIndex = ~0u,
            dstQueueFamilyIndex = ~0u,
            image = _fontImage,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1
            }
        };

        Vk.vkCmdPipelineBarrier(cmd, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.FragmentShader,
            0, 0, null, 0, null, 1, &barrier2);

        VulkanBuffer.EndSingleTimeCommands(_ctx, _initCommandPool, cmd);
        staging.Dispose();

        VkImageViewCreateInfo viewInfo = default;
        viewInfo.sType = VkStructureType.ImageViewCreateInfo;
        viewInfo.image = _fontImage;
        viewInfo.viewType = VkImageViewType._2D;
        viewInfo.format = VkFormat.R8G8B8A8Unorm;
        viewInfo.subresourceRange = new VkImageSubresourceRange
        {
            aspectMask = VkImageAspectFlags.Color,
            baseMipLevel = 0, levelCount = 1, baseArrayLayer = 0, layerCount = 1
        };

        VkImageView fontView;
        Vk.CheckResult(Vk.vkCreateImageView(_ctx.Device, &viewInfo, null, &fontView), "vkCreateImageView (font)");
        _fontImageView = fontView;

        VkSamplerCreateInfo samplerInfo = default;
        samplerInfo.sType = VkStructureType.SamplerCreateInfo;
        samplerInfo.magFilter = VkFilter.Linear;
        samplerInfo.minFilter = VkFilter.Linear;
        samplerInfo.mipmapMode = VkSamplerMipmapMode.Linear;
        samplerInfo.addressModeU = VkSamplerAddressMode.Repeat;
        samplerInfo.addressModeV = VkSamplerAddressMode.Repeat;
        samplerInfo.addressModeW = VkSamplerAddressMode.Repeat;
        samplerInfo.minLod = -1000;
        samplerInfo.maxLod = 1000;

        VkSampler fontSampler;
        Vk.CheckResult(Vk.vkCreateSampler(_ctx.Device, &samplerInfo, null, (void*)&fontSampler), "vkCreateSampler (font)");
        _fontSampler = fontSampler;
    }

    private unsafe VkShaderModule LoadShader(string path)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"ImGui SPIR-V shader not found: {fullPath}");

        var code = File.ReadAllBytes(fullPath);
        fixed (byte* pCode = code)
        {
            VkShaderModuleCreateInfo createInfo = default;
            createInfo.sType = VkStructureType.ShaderModuleCreateInfo;
            createInfo.codeSize = (ulong)code.Length;
            createInfo.pCode = (uint*)pCode;

            VkShaderModule module;
            Vk.CheckResult(Vk.vkCreateShaderModule(_ctx.Device, &createInfo, null, &module), $"vkCreateShaderModule ({path})");
            return module;
        }
    }

    private void CreateShaders()
    {
        _vertexShader = LoadShader("Shaders/imgui.vert.spv");
        _fragmentShader = LoadShader("Shaders/imgui.frag.spv");
    }

    private unsafe void CreateDescriptorSetLayout()
    {
        var binding = new VkDescriptorSetLayoutBinding
        {
            binding = 0,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Fragment,
            pImmutableSamplers = null
        };

        VkDescriptorSetLayoutCreateInfo createInfo = default;
        createInfo.sType = VkStructureType.DescriptorSetLayoutCreateInfo;
        createInfo.bindingCount = 1;
        createInfo.pBindings = &binding;

        VkDescriptorSetLayout dsLayout;
        Vk.CheckResult(Vk.vkCreateDescriptorSetLayout(_ctx.Device, &createInfo, null, &dsLayout), "vkCreateDescriptorSetLayout (ImGui)");
        _descriptorSetLayout = dsLayout;
    }

    private unsafe void CreatePipelineLayout()
    {
        var pushConstantRange = new VkPushConstantRange
        {
            stageFlags = VkShaderStageFlags.Vertex,
            offset = 0,
            size = 16
        };

        var dsLayout = _descriptorSetLayout;
        VkPipelineLayoutCreateInfo createInfo = default;
        createInfo.sType = VkStructureType.PipelineLayoutCreateInfo;
        createInfo.setLayoutCount = 1;
        createInfo.pSetLayouts = &dsLayout;
        createInfo.pushConstantRangeCount = 1;
        createInfo.pPushConstantRanges = &pushConstantRange;

        VkPipelineLayout pipeLayout;
        Vk.CheckResult(Vk.vkCreatePipelineLayout(_ctx.Device, &createInfo, null, &pipeLayout), "vkCreatePipelineLayout (ImGui)");
        _pipelineLayout = pipeLayout;
    }

    private unsafe void CreatePipeline()
    {
        var mainName = Vk.AllocUtf8("main");

        var stages = stackalloc VkPipelineShaderStageCreateInfo[2];
        stages[0] = new VkPipelineShaderStageCreateInfo
        {
            sType = VkStructureType.PipelineShaderStageCreateInfo,
            stage = VkShaderStageFlags.Vertex,
            module = _vertexShader,
            pName = mainName
        };
        stages[1] = new VkPipelineShaderStageCreateInfo
        {
            sType = VkStructureType.PipelineShaderStageCreateInfo,
            stage = VkShaderStageFlags.Fragment,
            module = _fragmentShader,
            pName = mainName
        };

        var bindingDesc = new VkVertexInputBindingDescription
        {
            binding = 0,
            stride = (uint)Marshal.SizeOf<ImDrawVert>(),
            inputRate = 0
        };

        var attrDescs = stackalloc VkVertexInputAttributeDescription[3];
        attrDescs[0] = new VkVertexInputAttributeDescription { location = 0, binding = 0, format = VkFormat.R32G32Sfloat, offset = 0 };
        attrDescs[1] = new VkVertexInputAttributeDescription { location = 1, binding = 0, format = VkFormat.R32G32Sfloat, offset = 8 };
        attrDescs[2] = new VkVertexInputAttributeDescription { location = 2, binding = 0, format = VkFormat.R8G8B8A8Unorm, offset = 16 };

        VkPipelineVertexInputStateCreateInfo vertexInputState = default;
        vertexInputState.sType = VkStructureType.PipelineVertexInputStateCreateInfo;
        vertexInputState.vertexBindingDescriptionCount = 1;
        vertexInputState.pVertexBindingDescriptions = &bindingDesc;
        vertexInputState.vertexAttributeDescriptionCount = 3;
        vertexInputState.pVertexAttributeDescriptions = attrDescs;

        VkPipelineInputAssemblyStateCreateInfo inputAssemblyState = default;
        inputAssemblyState.sType = VkStructureType.PipelineInputAssemblyStateCreateInfo;
        inputAssemblyState.topology = VkPrimitiveTopology.TriangleList;

        var viewport = new VkViewport();
        var scissor = new VkRect2D();

        VkPipelineViewportStateCreateInfo viewportState = default;
        viewportState.sType = VkStructureType.PipelineViewportStateCreateInfo;
        viewportState.viewportCount = 1;
        viewportState.pViewports = &viewport;
        viewportState.scissorCount = 1;
        viewportState.pScissors = &scissor;

        VkPipelineRasterizationStateCreateInfo rasterState = default;
        rasterState.sType = VkStructureType.PipelineRasterizationStateCreateInfo;
        rasterState.polygonMode = VkPolygonMode.Fill;
        rasterState.cullMode = VkCullModeFlags.None;
        rasterState.frontFace = VkFrontFace.Clockwise;
        rasterState.lineWidth = 1.0f;

        VkPipelineMultisampleStateCreateInfo msState = default;
        msState.sType = VkStructureType.PipelineMultisampleStateCreateInfo;
        msState.rasterizationSamples = VkSampleCountFlags.One;

        VkPipelineColorBlendAttachmentState blendAttachment = default;
        blendAttachment.blendEnable = 1;
        blendAttachment.srcColorBlendFactor = VkBlendFactor.SrcAlpha;
        blendAttachment.dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha;
        blendAttachment.colorBlendOp = VkBlendOp.Add;
        blendAttachment.srcAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha;
        blendAttachment.dstAlphaBlendFactor = VkBlendFactor.Zero;
        blendAttachment.alphaBlendOp = VkBlendOp.Add;
        blendAttachment.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;

        VkPipelineColorBlendStateCreateInfo blendState = default;
        blendState.sType = VkStructureType.PipelineColorBlendStateCreateInfo;
        blendState.attachmentCount = 1;
        blendState.pAttachments = &blendAttachment;

        VkGraphicsPipelineCreateInfo pipelineInfo = default;
        pipelineInfo.sType = VkStructureType.GraphicsPipelineCreateInfo;
        pipelineInfo.stageCount = 2;
        pipelineInfo.pStages = stages;
        pipelineInfo.pVertexInputState = &vertexInputState;
        pipelineInfo.pInputAssemblyState = &inputAssemblyState;
        pipelineInfo.pViewportState = &viewportState;
        pipelineInfo.pRasterizationState = &rasterState;
        pipelineInfo.pMultisampleState = &msState;
        pipelineInfo.pColorBlendState = &blendState;
        pipelineInfo.layout = _pipelineLayout;
        pipelineInfo.renderPass = _swapchain.RenderPass;
        pipelineInfo.subpass = 0;

        VkPipeline pipe;
        Vk.CheckResult(Vk.vkCreateGraphicsPipelines(_ctx.Device, 0, 1, &pipelineInfo, null, &pipe), "vkCreateGraphicsPipelines (ImGui)");
        _pipeline = pipe;

        Vk.FreeUtf8(mainName);
        Console.WriteLine("[Vulkan] ImGui pipeline created.");
    }

    private unsafe void CreateDescriptorPoolAndSet()
    {
        var poolSize = new VkDescriptorPoolSize
        {
            type = VkDescriptorType.CombinedImageSampler,
            descriptorCount = 1
        };

        VkDescriptorPoolCreateInfo poolInfo = default;
        poolInfo.sType = VkStructureType.DescriptorPoolCreateInfo;
        poolInfo.flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet;
        poolInfo.maxSets = 1;
        poolInfo.poolSizeCount = 1;
        poolInfo.pPoolSizes = &poolSize;

        VkDescriptorPool descPool;
        Vk.CheckResult(Vk.vkCreateDescriptorPool(_ctx.Device, &poolInfo, null, &descPool), "vkCreateDescriptorPool (ImGui)");
        _descriptorPool = descPool;

        VkDescriptorSetAllocateInfo allocInfo = default;
        allocInfo.sType = VkStructureType.DescriptorSetAllocateInfo;
        allocInfo.descriptorPool = _descriptorPool;
        allocInfo.descriptorSetCount = 1;
        var dsLayout = _descriptorSetLayout;
        allocInfo.pSetLayouts = &dsLayout;

        VkDescriptorSet descSet;
        Vk.CheckResult(Vk.vkAllocateDescriptorSets(_ctx.Device, &allocInfo, &descSet), "vkAllocateDescriptorSets (ImGui)");
        _descriptorSet = descSet;

        var imageInfo = new VkDescriptorImageInfo
        {
            sampler = _fontSampler,
            imageView = _fontImageView,
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal
        };

        VkWriteDescriptorSet writeInfo = default;
        writeInfo.sType = VkStructureType.WriteDescriptorSet;
        writeInfo.dstSet = _descriptorSet;
        writeInfo.dstBinding = 0;
        writeInfo.descriptorCount = 1;
        writeInfo.descriptorType = VkDescriptorType.CombinedImageSampler;
        writeInfo.pImageInfo = &imageInfo;

        Vk.vkUpdateDescriptorSets(_ctx.Device, 1, &writeInfo, 0, null);
    }

    public unsafe void Render(VkCommandBuffer cmd)
    {
        var drawData = ImGui.GetDrawData();
        if (drawData.CmdListsCount == 0) return;

        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);
        UpdateBuffers(drawData);

        Vk.vkCmdBindPipeline(cmd, 0, _pipeline);

        var set = _descriptorSet;
        Vk.vkCmdBindDescriptorSets(cmd, 0, _pipelineLayout, 0, 1, &set, 0, null);

        var displaySize = ImGui.GetIO().DisplaySize;
        var scale = new Vector2(2.0f / displaySize.X, 2.0f / displaySize.Y);
        var pushData = stackalloc float[2];
        pushData[0] = scale.X;
        pushData[1] = -scale.Y;

        Vk.vkCmdPushConstants(cmd, _pipelineLayout, VkShaderStageFlags.Vertex, 0, 8, pushData);

        var vb = _vertexBuffer!.Buffer;
        var offset = 0ul;
        Vk.vkCmdBindVertexBuffers(cmd, 0, 1, &vb, &offset);
        Vk.vkCmdBindIndexBuffer(cmd, _indexBuffer!.Buffer, 0, VkIndexType.Uint16);

        var indexOffset = 0u;
        var vtxOffset = 0u;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = new ImDrawListPtr(((ImDrawList**)drawData.CmdLists.Data)[n]);
            for (var i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                var imCmd = cmdList.CmdBuffer[i];
                Vk.vkCmdDrawIndexed(cmd, (uint)imCmd.ElemCount, 1, indexOffset, (int)vtxOffset, 0);
                indexOffset += (uint)imCmd.ElemCount;
            }
            vtxOffset += (uint)cmdList.VtxBuffer.Size;
        }
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        var vertexSize = (ulong)(drawData.TotalVtxCount * Marshal.SizeOf<ImDrawVert>());
        var indexSize = (ulong)(drawData.TotalIdxCount * sizeof(ushort));

        if (vertexSize == 0 || indexSize == 0) return;

        if (_vertexBuffer == null || _vertexBuffer.Size < vertexSize)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = new VulkanBuffer(_ctx, vertexSize,
                VkBufferUsageFlags.VertexBuffer,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
        }

        if (_indexBuffer == null || _indexBuffer.Size < indexSize)
        {
            _indexBuffer?.Dispose();
            _indexBuffer = new VulkanBuffer(_ctx, indexSize,
                VkBufferUsageFlags.IndexBuffer,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
        }

        var vtxDst = (byte*)_vertexBuffer.MappedData;
        var idxDst = (ushort*)_indexBuffer.MappedData;

        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = new ImDrawListPtr(((ImDrawList**)drawData.CmdLists.Data)[n]);
            var vtxSize = cmdList.VtxBuffer.Size * Marshal.SizeOf<ImDrawVert>();
            var idxSize = cmdList.IdxBuffer.Size * sizeof(ushort);

            Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDst, vtxSize, vtxSize);
            Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDst, idxSize, idxSize);

            vtxDst += vtxSize;
            idxDst += cmdList.IdxBuffer.Size;
        }
    }

    public void Dispose()
    {
        Vk.vkQueueWaitIdle(_ctx.GraphicsQueue);

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();

        if (_fontSampler.Value != 0) Vk.vkDestroySampler(_ctx.Device, (void*)_fontSampler.Value, null);
        if (_fontImageView.Value != 0) Vk.vkDestroyImageView(_ctx.Device, _fontImageView, null);
        if (_fontImage.Value != 0) Vk.vkDestroyImage(_ctx.Device, _fontImage, null);
        if (_fontImageMemory.Value != 0) Vk.vkFreeMemory(_ctx.Device, _fontImageMemory, null);
        if (_descriptorPool.Value != 0) Vk.vkDestroyDescriptorPool(_ctx.Device, _descriptorPool, null);
        if (_pipeline.Value != 0) Vk.vkDestroyPipeline(_ctx.Device, _pipeline, null);
        if (_pipelineLayout.Value != 0) Vk.vkDestroyPipelineLayout(_ctx.Device, _pipelineLayout, null);
        if (_descriptorSetLayout.Value != 0) Vk.vkDestroyDescriptorSetLayout(_ctx.Device, _descriptorSetLayout, null);
        if (_vertexShader.Value != 0) Vk.vkDestroyShaderModule(_ctx.Device, _vertexShader, null);
        if (_fragmentShader.Value != 0) Vk.vkDestroyShaderModule(_ctx.Device, _fragmentShader, null);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorImageInfo
{
    public VkSampler sampler;
    public VkImageView imageView;
    public VkImageLayout imageLayout;
}
