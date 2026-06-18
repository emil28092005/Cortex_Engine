using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Engine.Graphics.Vulkan;

internal sealed unsafe class VulkanImGui : IDisposable
{
    private readonly VkDevice _device;
    private readonly VkPhysicalDevice _physicalDevice;
    private readonly VkQueue _queue;
    private readonly VkCommandPool _commandPool;
    private readonly VulkanContext _ctx;
    private readonly VkFormat _colorFormat;
    private readonly VkFormat _depthFormat;

    private VkPipelineLayout _pipelineLayout;
    private VkPipeline _pipeline;
    private VkShaderModule _vertModule;
    private VkShaderModule _fragModule;
    private VkDescriptorSetLayout _descriptorSetLayout;
    private VkDescriptorPool _descriptorPool;
    private VkDescriptorSet _descriptorSet;

    private VkImage _fontImage;
    private VkDeviceMemory _fontImageMemory;
    private VkImageView _fontImageView;
    private VkSampler _fontSampler;

    private VkBuffer _vertexBuffer;
    private VkDeviceMemory _vertexMemory;
    private ulong _vertexBufferSize;

    private VkBuffer _indexBuffer;
    private VkDeviceMemory _indexMemory;
    private ulong _indexBufferSize;

    private bool _disposed;

    public VulkanImGui(VulkanContext ctx, VkCommandPool commandPool, VkFormat colorFormat, VkFormat depthFormat)
    {
        _ctx = ctx;
        _device = ctx.Device;
        _physicalDevice = ctx.PhysicalDevice;
        _queue = ctx.GraphicsQueue;
        _commandPool = commandPool;
        _colorFormat = colorFormat;
        _depthFormat = depthFormat;

        var imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(imGuiContext);

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.DisplaySize = new System.Numerics.Vector2(1280, 720);

        UploadFontAtlas();
        CreateDescriptorSetLayout();
        CreateDescriptorPoolAndSet();
        CreatePipeline();
        CreateBuffers();

        Console.WriteLine("[Vulkan] ImGui initialized");
    }

    private void UploadFontAtlas()
    {
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();

        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height);
        var uploadSize = (ulong)(width * height * 4);

        var stagingBuffer = VkBuffer.Null;
        var stagingMemory = VkDeviceMemory.Null;
        CreateBuffer(uploadSize, VkBufferUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            &stagingBuffer, &stagingMemory);

        void* pData = null;
        Vk.vkMapMemory(_device, stagingMemory, 0, uploadSize, 0, &pData);
        System.Buffer.MemoryCopy(pixels, pData, (long)uploadSize, (long)uploadSize);
        Vk.vkUnmapMemory(_device, stagingMemory);

        var imageInfo = new VkImageCreateInfo
        {
            sType = VkStructureType.ImageCreateInfo,
            imageType = VkImageType.Type2D,
            format = VkFormat.R8G8B8A8Unorm,
            extent = new VkExtent3D { Width = (uint)width, Height = (uint)height, Depth = 1 },
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
        };

        var img = VkImage.Null;
        Vk.vkCreateImage(_device, &imageInfo, 0, &img);
        _fontImage = img;

        var reqs = new VkMemoryRequirements();
        Vk.vkGetImageMemoryRequirements(_device, _fontImage, &reqs);
        var memTypeIndex = _ctx.FindMemoryType(reqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

        var allocInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = reqs.size,
            memoryTypeIndex = memTypeIndex,
        };

        var fmem = VkDeviceMemory.Null;
        Vk.vkAllocateMemory(_device, &allocInfo, 0, &fmem);
        _fontImageMemory = fmem;
        Vk.vkBindImageMemory(_device, _fontImage, _fontImageMemory, 0);

        var cmd = BeginOneTimeCommands();
        TransitionImageLayoutCmd(cmd, _fontImage,
            VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal,
            0, 0, 0x1000, 0x2000);

        var region = new VkBufferImageCopyRegion
        {
            bufferOffset = 0,
            bufferRowLength = 0,
            bufferImageHeight = 0,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1,
            },
            imageOffset = new VkOffset3D { X = 0, Y = 0, Z = 0 },
            imageExtent = new VkExtent3D { Width = (uint)width, Height = (uint)height, Depth = 1 },
        };
        Vk.vkCmdCopyBufferToImage(cmd, stagingBuffer, _fontImage, VkImageLayout.TransferDstOptimal, 1, &region);

        TransitionImageLayoutCmd(cmd, _fontImage,
            VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal,
            0x1000, 0x2000, 0x8, 0x20);

        EndOneTimeCommands(cmd);

        Vk.vkDestroyBuffer(_device, stagingBuffer, 0);
        Vk.vkFreeMemory(_device, stagingMemory, 0);

        var viewInfo = new VkImageViewCreateInfo
        {
            sType = VkStructureType.ImageViewCreateInfo,
            image = _fontImage,
            viewType = VkImageViewType.Type2D,
            format = VkFormat.R8G8B8A8Unorm,
            components = new VkComponentMapping { R = VkComponentSwizzle.Identity, G = VkComponentSwizzle.Identity, B = VkComponentSwizzle.Identity, A = VkComponentSwizzle.Identity },
            subresourceRange = new VkImageSubresourceRange { AspectMask = VkImageAspectFlags.Color, BaseMipLevel = 0, LevelCount = 1, BaseArrayLayer = 0, LayerCount = 1 },
        };

        var fview = VkImageView.Null;
        Vk.vkCreateImageView(_device, &viewInfo, 0, &fview);
        _fontImageView = fview;

        var samplerInfo = new VkSamplerCreateInfo
        {
            sType = VkStructureType.SamplerCreateInfo,
            magFilter = VkFilter.Linear,
            minFilter = VkFilter.Linear,
            mipmapMode = VkSamplerMipmapMode.Linear,
            addressModeU = VkSamplerAddressMode.Repeat,
            addressModeV = VkSamplerAddressMode.Repeat,
            addressModeW = VkSamplerAddressMode.Repeat,
            minLod = -1000,
            maxLod = 1000,
        };

        var fsamp = VkSampler.Null;
        Vk.vkCreateSampler(_device, &samplerInfo, 0, &fsamp);
        _fontSampler = fsamp;

        io.Fonts.SetTexID((nint)1);
    }

    private void CreateDescriptorSetLayout()
    {
        var binding = new VkDescriptorSetLayoutBinding
        {
            binding = 0,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Fragment,
        };

        var info = new VkDescriptorSetLayoutCreateInfo
        {
            sType = VkStructureType.DescriptorSetLayoutCreateInfo,
            bindingCount = 1,
            pBindings = &binding,
        };

        var layout = VkDescriptorSetLayout.Null;
        Vk.vkCreateDescriptorSetLayout(_device, &info, 0, &layout);
        _descriptorSetLayout = layout;
    }

    private void CreateDescriptorPoolAndSet()
    {
        var poolSize = new VkDescriptorPoolSize
        {
            type = VkDescriptorType.CombinedImageSampler,
            descriptorCount = 1,
        };

        var poolInfo = new VkDescriptorPoolCreateInfo
        {
            sType = VkStructureType.DescriptorPoolCreateInfo,
            maxSets = 1,
            poolSizeCount = 1,
            pPoolSizes = &poolSize,
        };

        var pool = VkDescriptorPool.Null;
        Vk.vkCreateDescriptorPool(_device, &poolInfo, 0, &pool);
        _descriptorPool = pool;

        var layout = _descriptorSetLayout;
        var allocInfo = new VkDescriptorSetAllocateInfo
        {
            sType = VkStructureType.DescriptorSetAllocateInfo,
            descriptorPool = _descriptorPool,
            descriptorSetCount = 1,
            pSetLayouts = &layout,
        };

        var set = VkDescriptorSet.Null;
        Vk.vkAllocateDescriptorSets(_device, &allocInfo, &set);
        _descriptorSet = set;

        var imageInfo = new VkDescriptorImageInfo
        {
            sampler = _fontSampler,
            imageView = _fontImageView,
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
        };

        var write = new VkWriteDescriptorSet
        {
            sType = VkStructureType.WriteDescriptorSet,
            dstSet = _descriptorSet,
            dstBinding = 0,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            pImageInfo = (nint)(&imageInfo),
        };

        Vk.vkUpdateDescriptorSets(_device, 1, &write, 0, 0);
    }

    private void CreatePipeline()
    {
        _vertModule = CreateShaderModule(LoadShader("Shaders/imgui.vert.spv"));
        _fragModule = CreateShaderModule(LoadShader("Shaders/imgui.frag.spv"));

        fixed (byte* pName = "main\0"u8)
        {
            var stages = stackalloc VkPipelineShaderStageCreateInfo[2];
            stages[0] = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Vertex,
                module = _vertModule,
                pName = pName,
            };
            stages[1] = new VkPipelineShaderStageCreateInfo
            {
                sType = VkStructureType.PipelineShaderStageCreateInfo,
                stage = VkShaderStageFlags.Fragment,
                module = _fragModule,
                pName = pName,
            };

            var bindings = stackalloc VkVertexInputBindingDescription[1];
            bindings[0] = new VkVertexInputBindingDescription
            {
                binding = 0,
                stride = 20,
                inputRate = VkVertexInputRate.Vertex,
            };

            var attributes = stackalloc VkVertexInputAttributeDescription[3];
            attributes[0] = new VkVertexInputAttributeDescription { location = 0, binding = 0, format = VkFormat.R32G32Sfloat, offset = 0 };
            attributes[1] = new VkVertexInputAttributeDescription { location = 1, binding = 0, format = VkFormat.R32G32Sfloat, offset = 8 };
            attributes[2] = new VkVertexInputAttributeDescription { location = 2, binding = 0, format = VkFormat.R8G8B8A8Unorm, offset = 16 };

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
                scissorCount = 1,
            };

            var rasterizationState = new VkPipelineRasterizationStateCreateInfo
            {
                sType = VkStructureType.PipelineRasterizationStateCreateInfo,
                polygonMode = VkPolygonMode.Fill,
                cullMode = VkCullModeFlags.None,
                frontFace = VkFrontFace.CounterClockwise,
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
                depthTestEnable = VkBool32.False,
                depthWriteEnable = VkBool32.False,
            };

            var blendAttachment = new VkPipelineColorBlendAttachmentState
            {
                blendEnable = VkBool32.True,
                srcColorBlendFactor = VkBlendFactor.SrcAlpha,
                dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                colorBlendOp = VkBlendOp.Add,
                srcAlphaBlendFactor = VkBlendFactor.One,
                dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                alphaBlendOp = VkBlendOp.Add,
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
                stageFlags = VkShaderStageFlags.Vertex,
                offset = 0,
                size = 64,
            };

            var descLayout = _descriptorSetLayout;
            var layoutInfo = new VkPipelineLayoutCreateInfo
            {
                sType = VkStructureType.PipelineLayoutCreateInfo,
                setLayoutCount = 1,
                pSetLayouts = &descLayout,
                pushConstantRangeCount = 1,
                pPushConstantRanges = (nint)(&pushConstantRange),
            };

            var pl = VkPipelineLayout.Null;
            Vk.vkCreatePipelineLayout(_device, &layoutInfo, 0, &pl);
            _pipelineLayout = pl;

            var cf = _colorFormat;
            var df = _depthFormat;
            var renderingInfo = new VkPipelineRenderingCreateInfo
            {
                sType = VkStructureType.PipelineRenderingCreateInfo,
                colorAttachmentCount = 1,
                pColorAttachmentFormats = &cf,
                depthAttachmentFormat = df,
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
                layout = _pipelineLayout,
                renderPass = new VkRenderPass { Handle = 0 },
                subpass = 0,
            };

            var pp = VkPipeline.Null;
            Vk.vkCreateGraphicsPipelines(_device, 0, 1, &pipelineInfo, 0, &pp);
            _pipeline = pp;
        }
    }

    private void CreateBuffers()
    {
        _vertexBufferSize = 65536;
        _indexBufferSize = 65536 * 2;

        var vb = VkBuffer.Null;
        var vm = VkDeviceMemory.Null;
        CreateBuffer(_vertexBufferSize, VkBufferUsageFlags.VertexBuffer,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            &vb, &vm);
        _vertexBuffer = vb;
        _vertexMemory = vm;

        var ib = VkBuffer.Null;
        var im = VkDeviceMemory.Null;
        CreateBuffer(_indexBufferSize, VkBufferUsageFlags.IndexBuffer,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            &ib, &im);
        _indexBuffer = ib;
        _indexMemory = im;
    }

    public void NewFrame()
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(1280, 720);
        io.DeltaTime = 0.016f;
        ImGui.NewFrame();
    }

    public void Render(VkCommandBuffer cmd, uint width, uint height)
    {
        var drawData = ImGui.GetDrawData();
        if (drawData.CmdListsCount == 0)
            return;

        var io = ImGui.GetIO();
        var scale = Matrix4x4.CreateScale(2f / io.DisplaySize.X, 2f / io.DisplaySize.Y, 1f);
        var trans = Matrix4x4.CreateTranslation(-1f, -1f, 0f);
        var mvp = scale * trans;

        EnsureBufferSize(ref _vertexBuffer, ref _vertexMemory, ref _vertexBufferSize,
            (ulong)(drawData.TotalVtxCount * 20), VkBufferUsageFlags.VertexBuffer);
        EnsureBufferSize(ref _indexBuffer, ref _indexMemory, ref _indexBufferSize,
            (ulong)(drawData.TotalIdxCount * 2), VkBufferUsageFlags.IndexBuffer);

        void* pVtx = null;
        var totalVtxSize = (ulong)(drawData.TotalVtxCount * 20);
        Vk.vkMapMemory(_device, _vertexMemory, 0, totalVtxSize, 0, &pVtx);
        void* pIdx = null;
        var totalIdxSize = (ulong)(drawData.TotalIdxCount * 2);
        Vk.vkMapMemory(_device, _indexMemory, 0, totalIdxSize, 0, &pIdx);

        var vtxPtr = (byte*)pVtx;
        var idxPtr = (byte*)pIdx;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var list = drawData.CmdLists[n];
            var vtxBytes = list.VtxBuffer.Size * 20;
            System.Buffer.MemoryCopy((void*)list.VtxBuffer.Data, vtxPtr, vtxBytes, vtxBytes);
            vtxPtr += vtxBytes;

            var idxBytes = list.IdxBuffer.Size * 2;
            System.Buffer.MemoryCopy((void*)list.IdxBuffer.Data, idxPtr, idxBytes, idxBytes);
            idxPtr += idxBytes;
        }

        Vk.vkUnmapMemory(_device, _vertexMemory);
        Vk.vkUnmapMemory(_device, _indexMemory);

        Vk.vkCmdBindPipeline(cmd, VkPipelineBindPoint.Graphics, _pipeline);

        var vp = new VkViewport { X = 0, Y = 0, Width = width, Height = height, MinDepth = 0, MaxDepth = 1 };
        Vk.vkCmdSetViewport(cmd, 0, 1, &vp);

        var descSet = _descriptorSet;
        Vk.vkCmdBindDescriptorSets(cmd, VkPipelineBindPoint.Graphics, _pipelineLayout,
            0, 1, &descSet, 0, null);

        var vb = _vertexBuffer;
        ulong voff = 0;
        Vk.vkCmdBindVertexBuffers(cmd, 0, 1, &vb, &voff);
        Vk.vkCmdBindIndexBuffer(cmd, _indexBuffer, 0, 0);

        Vk.vkCmdPushConstants(cmd, _pipelineLayout, VkShaderStageFlags.Vertex, 0, 64, &mvp);

        int vtxOffset = 0;
        int idxOffset = 0;
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var list = drawData.CmdLists[n];
            for (int i = 0; i < list.CmdBuffer.Size; i++)
            {
                var pcmd = list.CmdBuffer[i];
                var clip = pcmd.ClipRect;
                var scissor = new VkRect2D
                {
                    Offset = new VkOffset2D { X = (int)clip.X, Y = (int)clip.Y },
                    Extent = new VkExtent2D
                    {
                        Width = (uint)(clip.Z - clip.X),
                        Height = (uint)(clip.W - clip.Y),
                    },
                };
                Vk.vkCmdSetScissor(cmd, 0, 1, &scissor);
                Vk.vkCmdDrawIndexed(cmd, pcmd.ElemCount, 1, (uint)(idxOffset + pcmd.IdxOffset), (int)(vtxOffset + pcmd.VtxOffset), 0);
            }
            vtxOffset += list.VtxBuffer.Size;
            idxOffset += list.IdxBuffer.Size;
        }
    }

    private void EnsureBufferSize(ref VkBuffer buffer, ref VkDeviceMemory memory, ref ulong currentSize,
        ulong neededSize, VkBufferUsageFlags usage)
    {
        if (neededSize <= currentSize) return;
        GrowBuffer(ref buffer, ref memory, ref currentSize, neededSize, usage);
    }

    private void CreateBuffer(ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags props,
        VkBuffer* pBuffer, VkDeviceMemory* pMemory)
    {
        var info = new VkBufferCreateInfo
        {
            sType = VkStructureType.BufferCreateInfo,
            size = size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive,
        };

        Vk.vkCreateBuffer(_device, &info, 0, pBuffer);

        var reqs = new VkMemoryRequirements();
        Vk.vkGetBufferMemoryRequirements(_device, *pBuffer, &reqs);

        var memTypeIndex = _ctx.FindMemoryType(reqs.memoryTypeBits, props);

        var allocInfo = new VkMemoryAllocateInfo
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = reqs.size,
            memoryTypeIndex = memTypeIndex,
        };

        Vk.vkAllocateMemory(_device, &allocInfo, 0, pMemory);
        Vk.vkBindBufferMemory(_device, *pBuffer, *pMemory, 0);
    }

    private void GrowBuffer(ref VkBuffer buffer, ref VkDeviceMemory memory, ref ulong currentSize,
        ulong neededSize, VkBufferUsageFlags usage)
    {
        Vk.vkDeviceWaitIdle(_device);

        if (buffer.Handle != 0) Vk.vkDestroyBuffer(_device, buffer, 0);
        if (memory.Handle != 0) Vk.vkFreeMemory(_device, memory, 0);

        currentSize = neededSize * 2;
        var b = VkBuffer.Null;
        var m = VkDeviceMemory.Null;
        CreateBuffer(currentSize, usage,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            &b, &m);
        buffer = b;
        memory = m;
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

    private VkCommandBuffer BeginOneTimeCommands()
    {
        var allocInfo = new VkCommandBufferAllocateInfo
        {
            sType = VkStructureType.CommandBufferAllocateInfo,
            commandPool = _commandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = 1,
        };

        var cmd = VkCommandBuffer.Null;
        Vk.vkAllocateCommandBuffers(_device, &allocInfo, &cmd);

        var beginInfo = new VkCommandBufferBeginInfo
        {
            sType = VkStructureType.CommandBufferBeginInfo,
            flags = VkCommandBufferUsageFlags.OneTimeSubmit,
        };

        Vk.vkBeginCommandBuffer(cmd, &beginInfo);
        return cmd;
    }

    private void EndOneTimeCommands(VkCommandBuffer cmd)
    {
        Vk.vkEndCommandBuffer(cmd);

        var cmdInfo = new VkCommandBufferSubmitInfo
        {
            sType = VkStructureType.CommandBufferSubmitInfo,
            commandBuffer = cmd,
        };

        var submitInfo = new VkSubmitInfo2
        {
            sType = VkStructureType.SubmitInfo2,
            commandBufferInfoCount = 1,
            pCommandBufferInfos = &cmdInfo,
        };

        Vk.vkQueueSubmit2(_queue, 1, &submitInfo, VkFence.Null);
        Vk.vkQueueWaitIdle(_queue);

        Vk.vkFreeCommandBuffers(_device, _commandPool, 1, &cmd);
    }

    private static void TransitionImageLayoutCmd(VkCommandBuffer cmd, VkImage image,
        VkImageLayout oldLayout, VkImageLayout newLayout,
        ulong srcStage, ulong srcAccess, ulong dstStage, ulong dstAccess)
    {
        var barrier = new VkImageMemoryBarrier2
        {
            sType = VkStructureType.ImageMemoryBarrier2,
            srcStageMask = srcStage,
            srcAccessMask = srcAccess,
            dstStageMask = dstStage,
            dstAccessMask = dstAccess,
            oldLayout = oldLayout,
            newLayout = newLayout,
            image = image,
            subresourceRange = new VkImageSubresourceRange
            {
                AspectMask = VkImageAspectFlags.Color,
                LevelCount = 1,
                LayerCount = 1,
            },
        };

        var depInfo = new VkDependencyInfo
        {
            sType = VkStructureType.DependencyInfo,
            imageMemoryBarrierCount = 1,
            pImageMemoryBarriers = &barrier,
        };

        Vk.vkCmdPipelineBarrier2(cmd, &depInfo);
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

        Vk.vkDeviceWaitIdle(_device);

        if (_pipeline.Handle != 0) Vk.vkDestroyPipeline(_device, _pipeline, 0);
        if (_pipelineLayout.Handle != 0) Vk.vkDestroyPipelineLayout(_device, _pipelineLayout, 0);
        if (_descriptorPool.Handle != 0) Vk.vkDestroyDescriptorPool(_device, _descriptorPool, 0);
        if (_descriptorSetLayout.Handle != 0) Vk.vkDestroyDescriptorSetLayout(_device, _descriptorSetLayout, 0);
        if (_fontSampler.Handle != 0) Vk.vkDestroySampler(_device, _fontSampler, 0);
        if (_fontImageView.Handle != 0) Vk.vkDestroyImageView(_device, _fontImageView, 0);
        if (_fontImage.Handle != 0) Vk.vkDestroyImage(_device, _fontImage, 0);
        if (_fontImageMemory.Handle != 0) Vk.vkFreeMemory(_device, _fontImageMemory, 0);
        if (_vertexBuffer.Handle != 0) Vk.vkDestroyBuffer(_device, _vertexBuffer, 0);
        if (_vertexMemory.Handle != 0) Vk.vkFreeMemory(_device, _vertexMemory, 0);
        if (_indexBuffer.Handle != 0) Vk.vkDestroyBuffer(_device, _indexBuffer, 0);
        if (_indexMemory.Handle != 0) Vk.vkFreeMemory(_device, _indexMemory, 0);
        if (_vertModule.Handle != 0) Vk.vkDestroyShaderModule(_device, _vertModule, 0);
        if (_fragModule.Handle != 0) Vk.vkDestroyShaderModule(_device, _fragModule, 0);
    }
}
