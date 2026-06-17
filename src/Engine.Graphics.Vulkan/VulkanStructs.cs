using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkExtent2D
{
    public uint Width;
    public uint Height;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkExtent3D
{
    public uint Width;
    public uint Height;
    public uint Depth;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkOffset2D
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkOffset3D
{
    public int X;
    public int Y;
    public int Z;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkRect2D
{
    public VkOffset2D Offset;
    public VkExtent2D Extent;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkViewport
{
    public float X;
    public float Y;
    public float Width;
    public float Height;
    public float MinDepth;
    public float MaxDepth;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkComponentMapping
{
    public VkComponentSwizzle R;
    public VkComponentSwizzle G;
    public VkComponentSwizzle B;
    public VkComponentSwizzle A;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkImageSubresourceRange
{
    public VkImageAspectFlags AspectMask;
    public uint BaseMipLevel;
    public uint LevelCount;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkClearColorValue
{
    public float Float0;
    public float Float1;
    public float Float2;
    public float Float3;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkClearDepthStencilValue
{
    public float Depth;
    public uint Stencil;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VkClearValue
{
    [FieldOffset(0)] public VkClearColorValue Color;
    [FieldOffset(0)] public VkClearDepthStencilValue DepthStencil;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkApplicationInfo
{
    public VkStructureType sType;
    public nint pNext;
    public byte* pApplicationName;
    public uint applicationVersion;
    public byte* pEngineName;
    public uint engineVersion;
    public uint apiVersion;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkInstanceCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkApplicationInfo* pApplicationInfo;
    public uint enabledLayerCount;
    public byte** ppEnabledLayerNames;
    public uint enabledExtensionCount;
    public byte** ppEnabledExtensionNames;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDebugUtilsMessengerCreateInfoEXT
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkDebugUtilsMessageSeverityFlagsEXT messageSeverity;
    public VkDebugUtilsMessageTypeFlagsEXT messageType;
    public nint pfnUserCallback;
    public nint pUserData;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDeviceQueueCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint queueFamilyIndex;
    public uint queueCount;
    public float* pQueuePriorities;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPhysicalDeviceFeatures
{
    public VkBool32 robustBufferAccess;
    public VkBool32 fullDrawIndexUint32;
    public VkBool32 imageCubeArray;
    public VkBool32 independentBlend;
    public VkBool32 geometryShader;
    public VkBool32 tessellationShader;
    public VkBool32 sampleRateShading;
    public VkBool32 dualSrcBlend;
    public VkBool32 logicOp;
    public VkBool32 multiDrawIndirect;
    public VkBool32 drawIndirectFirstInstance;
    public VkBool32 depthClamp;
    public VkBool32 depthBiasClamp;
    public VkBool32 fillModeNonSolid;
    public VkBool32 depthBounds;
    public VkBool32 wideLines;
    public VkBool32 largePoints;
    public VkBool32 alphaToOne;
    public VkBool32 multiViewport;
    public VkBool32 samplerAnisotropy;
    public VkBool32 textureCompressionETC2;
    public VkBool32 textureCompressionASTC_LDR;
    public VkBool32 textureCompressionBC;
    public VkBool32 occlusionQueryPrecise;
    public VkBool32 pipelineStatisticsQuery;
    public VkBool32 vertexPipelineStoresAndAtomics;
    public VkBool32 fragmentStoresAndAtomics;
    public VkBool32 shaderTessellationAndGeometryPointSize;
    public VkBool32 shaderImageGatherExtended;
    public VkBool32 shaderStorageImageExtendedFormats;
    public VkBool32 shaderStorageImageMultisample;
    public VkBool32 shaderStorageImageReadWithoutFormat;
    public VkBool32 shaderStorageImageWriteWithoutFormat;
    public VkBool32 shaderUniformBufferArrayDynamicIndexing;
    public VkBool32 shaderSampledImageArrayDynamicIndexing;
    public VkBool32 shaderStorageBufferArrayDynamicIndexing;
    public VkBool32 shaderStorageImageArrayDynamicIndexing;
    public VkBool32 shaderClipDistance;
    public VkBool32 shaderCullDistance;
    public VkBool32 shaderFloat64;
    public VkBool32 shaderInt64;
    public VkBool32 shaderInt16;
    public VkBool32 shaderResourceResidency;
    public VkBool32 shaderResourceMinLod;
    public VkBool32 sparseBinding;
    public VkBool32 sparseResidencyBuffer;
    public VkBool32 sparseResidencyImage2D;
    public VkBool32 sparseResidencyImage3D;
    public VkBool32 sparseResidency2Samples;
    public VkBool32 sparseResidency4Samples;
    public VkBool32 sparseResidency8Samples;
    public VkBool32 sparseResidency16Samples;
    public VkBool32 sparseResidencyAliased;
    public VkBool32 variableMultisampleRate;
    public VkBool32 inheritedQueries;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPhysicalDeviceDynamicRenderingFeatures
{
    public VkStructureType sType;
    public nint pNext;
    public VkBool32 dynamicRendering;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPhysicalDeviceSynchronization2Features
{
    public VkStructureType sType;
    public nint pNext;
    public VkBool32 synchronization2;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDeviceCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint queueCreateInfoCount;
    public VkDeviceQueueCreateInfo* pQueueCreateInfos;
    public uint enabledLayerCount;
    public byte** ppEnabledLayerNames;
    public uint enabledExtensionCount;
    public byte** ppEnabledExtensionNames;
    public VkPhysicalDeviceFeatures* pEnabledFeatures;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPhysicalDeviceProperties
{
    public uint apiVersion;
    public uint driverVersion;
    public uint vendorID;
    public uint deviceID;
    public VkPhysicalDeviceType deviceType;
    public fixed byte deviceName[256];
    public fixed byte pipelineCacheUUID[16];
    public VkPhysicalDeviceLimits Limits;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPhysicalDeviceLimits
{
    public uint maxImageDimension1D;
    public uint maxImageDimension2D;
    public uint maxImageDimension3D;
    public uint maxImageDimensionCube;
    public uint maxImageArrayLayers;
    public uint maxTexelBufferElements;
    public uint maxUniformBufferRange;
    public uint maxStorageBufferRange;
    public uint maxPushConstantsSize;
    public uint maxMemoryAllocationCount;
    public uint maxSamplerAllocationCount;
    public uint bufferImageGranularity;
    public uint sparseAddressSpaceSize;
    public uint maxBoundDescriptorSets;
    public uint maxPerStageDescriptorSamplers;
    public uint maxPerStageDescriptorUniformBuffers;
    public uint maxPerStageDescriptorStorageBuffers;
    public uint maxPerStageDescriptorSampledImages;
    public uint maxPerStageDescriptorStorageImages;
    public uint maxPerStageDescriptorInputAttachments;
    public uint maxPerStageResources;
    public uint maxDescriptorSetSamplers;
    public uint maxDescriptorSetUniformBuffers;
    public uint maxDescriptorSetUniformBuffersDynamic;
    public uint maxDescriptorSetStorageBuffers;
    public uint maxDescriptorSetStorageBuffersDynamic;
    public uint maxDescriptorSetSampledImages;
    public uint maxDescriptorSetStorageImages;
    public uint maxDescriptorSetInputAttachments;
    public uint maxVertexInputAttributes;
    public uint maxVertexInputBindings;
    public uint maxVertexInputAttributeOffset;
    public uint maxVertexInputBindingStride;
    public uint maxVertexOutputComponents;
    public uint maxTessellationGenerationLevel;
    public uint maxTessellationPatchSize;
    public uint maxTessellationControlPerVertexInputComponents;
    public uint maxTessellationControlPerVertexOutputComponents;
    public uint maxTessellationControlPerPatchOutputComponents;
    public uint maxTessellationControlTotalOutputComponents;
    public uint maxTessellationEvaluationInputComponents;
    public uint maxTessellationEvaluationOutputComponents;
    public uint maxGeometryShaderInvocations;
    public uint maxGeometryInputComponents;
    public uint maxGeometryOutputComponents;
    public uint maxGeometryOutputVertices;
    public uint maxGeometryTotalOutputComponents;
    public uint maxFragmentInputComponents;
    public uint maxFragmentOutputAttachments;
    public uint maxFragmentDualSrcAttachments;
    public uint maxFragmentCombinedOutputResources;
    public uint maxComputeSharedMemorySize;
    public uint maxComputeWorkGroupCount0;
    public uint maxComputeWorkGroupCount1;
    public uint maxComputeWorkGroupCount2;
    public uint maxComputeWorkGroupInvocations;
    public uint maxComputeWorkGroupSize0;
    public uint maxComputeWorkGroupSize1;
    public uint maxComputeWorkGroupSize2;
    public uint subPixelPrecisionBits;
    public uint subTexelPrecisionBits;
    public uint mipMapPrecisionBits;
    public uint maxDrawIndexedIndexValue;
    public uint maxDrawIndirectCount;
    public float maxSamplerLodBias;
    public float maxSamplerAnisotropy;
    public uint maxViewports;
    public uint maxViewportDimensions0;
    public uint maxViewportDimensions1;
    public float viewportBoundsRange0;
    public float viewportBoundsRange1;
    public uint viewportSubPixelBits;
    public ulong minMemoryMapAlignment;
    public ulong minTexelBufferOffsetAlignment;
    public ulong minUniformBufferOffsetAlignment;
    public ulong minStorageBufferOffsetAlignment;
    public int minTexelOffset;
    public uint maxTexelOffset;
    public int minTexelGatherOffset;
    public uint maxTexelGatherOffset;
    public float minInterpolationOffset;
    public float maxInterpolationOffset;
    public uint subPixelInterpolationOffsetBits;
    public uint maxFramebufferWidth;
    public uint maxFramebufferHeight;
    public uint maxFramebufferLayers;
    public uint maxColorAttachments;
    public uint maxSampleMaskWords;
    public float timestampPeriod;
    public uint maxClipDistances;
    public uint maxCullDistances;
    public uint maxCombinedClipAndCullDistances;
    public uint discreteQueuePriorities;
    public float pointSizeRange0;
    public float pointSizeRange1;
    public float lineWidthRange0;
    public float lineWidthRange1;
    public float pointSizeGranularity;
    public float lineWidthGranularity;
    public VkBool32 strictLines;
    public VkBool32 standardSampleLocations;
    public ulong optimalBufferCopyOffsetAlignment;
    public ulong optimalBufferCopyRowPitchAlignment;
    public ulong nonCoherentAtomSize;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkQueueFamilyProperties
{
    public VkQueueFlags queueFlags;
    public uint queueCount;
    public uint timestampValidBits;
    public VkExtent3D minImageTransferGranularity;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkMemoryType
{
    public VkMemoryPropertyFlags propertyFlags;
    public uint heapIndex;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkMemoryHeap
{
    public ulong size;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPhysicalDeviceMemoryProperties
{
    public uint memoryTypeCount;
    public VkMemoryType memoryTypes0;
    public VkMemoryType memoryTypes1;
    public VkMemoryType memoryTypes2;
    public VkMemoryType memoryTypes3;
    public VkMemoryType memoryTypes4;
    public VkMemoryType memoryTypes5;
    public VkMemoryType memoryTypes6;
    public VkMemoryType memoryTypes7;
    public VkMemoryType memoryTypes8;
    public VkMemoryType memoryTypes9;
    public VkMemoryType memoryTypes10;
    public VkMemoryType memoryTypes11;
    public VkMemoryType memoryTypes12;
    public VkMemoryType memoryTypes13;
    public VkMemoryType memoryTypes14;
    public VkMemoryType memoryTypes15;
    public VkMemoryType memoryTypes16;
    public VkMemoryType memoryTypes17;
    public VkMemoryType memoryTypes18;
    public VkMemoryType memoryTypes19;
    public VkMemoryType memoryTypes20;
    public VkMemoryType memoryTypes21;
    public VkMemoryType memoryTypes22;
    public VkMemoryType memoryTypes23;
    public VkMemoryType memoryTypes24;
    public VkMemoryType memoryTypes25;
    public VkMemoryType memoryTypes26;
    public VkMemoryType memoryTypes27;
    public VkMemoryType memoryTypes28;
    public VkMemoryType memoryTypes29;
    public VkMemoryType memoryTypes30;
    public VkMemoryType memoryTypes31;
    public uint memoryHeapCount;
    public VkMemoryHeap memoryHeaps0;
    public VkMemoryHeap memoryHeaps1;
    public VkMemoryHeap memoryHeaps2;
    public VkMemoryHeap memoryHeaps3;
    public VkMemoryHeap memoryHeaps4;
    public VkMemoryHeap memoryHeaps5;
    public VkMemoryHeap memoryHeaps6;
    public VkMemoryHeap memoryHeaps7;
    public VkMemoryHeap memoryHeaps8;
    public VkMemoryHeap memoryHeaps9;
    public VkMemoryHeap memoryHeaps10;
    public VkMemoryHeap memoryHeaps11;
    public VkMemoryHeap memoryHeaps12;
    public VkMemoryHeap memoryHeaps13;
    public VkMemoryHeap memoryHeaps14;
    public VkMemoryHeap memoryHeaps15;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSurfaceCapabilitiesKHR
{
    public uint minImageCount;
    public uint maxImageCount;
    public VkExtent2D currentExtent;
    public VkExtent2D minImageExtent;
    public VkExtent2D maxImageExtent;
    public uint maxImageArrayLayers;
    public VkSurfaceTransformFlagsKHR supportedTransforms;
    public VkSurfaceTransformFlagsKHR currentTransform;
    public VkCompositeAlphaFlagsKHR supportedCompositeAlpha;
    public VkImageUsageFlags supportedUsageFlags;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSurfaceFormatKHR
{
    public VkFormat format;
    public VkColorSpaceKHR colorSpace;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSwapchainCreateInfoKHR
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkSurfaceKHR surface;
    public uint minImageCount;
    public VkFormat imageFormat;
    public VkColorSpaceKHR imageColorSpace;
    public VkExtent2D imageExtent;
    public uint imageArrayLayers;
    public VkImageUsageFlags imageUsage;
    public VkSharingMode imageSharingMode;
    public uint queueFamilyIndexCount;
    public uint* pQueueFamilyIndices;
    public VkSurfaceTransformFlagsKHR preTransform;
    public VkCompositeAlphaFlagsKHR compositeAlpha;
    public VkPresentModeKHR presentMode;
    public VkBool32 clipped;
    public VkSwapchainKHR oldSwapchain;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkImageViewCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkImage image;
    public VkImageViewType viewType;
    public VkFormat format;
    public VkComponentMapping components;
    public VkImageSubresourceRange subresourceRange;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkShaderModuleCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public nuint codeSize;
    public uint* pCode;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineShaderStageCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkShaderStageFlags stage;
    public VkShaderModule module;
    public byte* pName;
    public nint pSpecializationInfo;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkVertexInputBindingDescription
{
    public uint binding;
    public uint stride;
    public VkVertexInputRate inputRate;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkVertexInputAttributeDescription
{
    public uint location;
    public uint binding;
    public VkFormat format;
    public uint offset;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineVertexInputStateCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint vertexBindingDescriptionCount;
    public VkVertexInputBindingDescription* pVertexBindingDescriptions;
    public uint vertexAttributeDescriptionCount;
    public VkVertexInputAttributeDescription* pVertexAttributeDescriptions;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineInputAssemblyStateCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkPrimitiveTopology topology;
    public VkBool32 primitiveRestartEnable;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineViewportStateCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint viewportCount;
    public VkViewport* pViewports;
    public uint scissorCount;
    public VkRect2D* pScissors;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineRasterizationStateCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkBool32 depthClampEnable;
    public VkBool32 rasterizerDiscardEnable;
    public VkPolygonMode polygonMode;
    public VkCullModeFlags cullMode;
    public VkFrontFace frontFace;
    public VkBool32 depthBiasEnable;
    public float depthBiasConstantFactor;
    public float depthBiasClamp;
    public float depthBiasSlopeFactor;
    public float lineWidth;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineMultisampleStateCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkSampleCountFlags rasterizationSamples;
    public VkBool32 sampleShadingEnable;
    public float minSampleShading;
    public nint pSampleMask;
    public VkBool32 alphaToCoverageEnable;
    public VkBool32 alphaToOneEnable;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineColorBlendAttachmentState
{
    public VkBool32 blendEnable;
    public VkBlendFactor srcColorBlendFactor;
    public VkBlendFactor dstColorBlendFactor;
    public VkBlendOp colorBlendOp;
    public VkBlendFactor srcAlphaBlendFactor;
    public VkBlendFactor dstAlphaBlendFactor;
    public VkBlendOp alphaBlendOp;
    public VkColorComponentFlags colorWriteMask;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineColorBlendStateCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkBool32 logicOpEnable;
    public int logicOp;
    public uint attachmentCount;
    public VkPipelineColorBlendAttachmentState* pAttachments;
    public float blendConstants0;
    public float blendConstants1;
    public float blendConstants2;
    public float blendConstants3;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineDynamicStateCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint dynamicStateCount;
    public VkDynamicState* pDynamicStates;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineLayoutCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint setLayoutCount;
    public VkDescriptorSetLayout* pSetLayouts;
    public uint pushConstantRangeCount;
    public nint pPushConstantRanges;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineRenderingCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint viewMask;
    public uint colorAttachmentCount;
    public VkFormat* pColorAttachmentFormats;
    public VkFormat depthAttachmentFormat;
    public VkFormat stencilAttachmentFormat;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkGraphicsPipelineCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint stageCount;
    public VkPipelineShaderStageCreateInfo* pStages;
    public VkPipelineVertexInputStateCreateInfo* pVertexInputState;
    public VkPipelineInputAssemblyStateCreateInfo* pInputAssemblyState;
    public nint pTessellationState;
    public VkPipelineViewportStateCreateInfo* pViewportState;
    public VkPipelineRasterizationStateCreateInfo* pRasterizationState;
    public VkPipelineMultisampleStateCreateInfo* pMultisampleState;
    public VkPipelineDepthStencilStateCreateInfo* pDepthStencilState;
    public VkPipelineColorBlendStateCreateInfo* pColorBlendState;
    public VkPipelineDynamicStateCreateInfo* pDynamicState;
    public VkPipelineLayout layout;
    public VkRenderPass renderPass;
    public uint subpass;
    public VkPipeline basePipelineHandle;
    public int basePipelineIndex;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkRenderPass { public nint Handle; }

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkCommandPoolCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkCommandPoolCreateFlags flags;
    public uint queueFamilyIndex;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkCommandBufferAllocateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkCommandPool commandPool;
    public VkCommandBufferLevel level;
    public uint commandBufferCount;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkCommandBufferBeginInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkCommandBufferUsageFlags flags;
    public nint pInheritanceInfo;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSemaphoreCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkFenceCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkFenceCreateFlags flags;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkBufferCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public ulong size;
    public VkBufferUsageFlags usage;
    public VkSharingMode sharingMode;
    public uint queueFamilyIndexCount;
    public uint* pQueueFamilyIndices;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkMemoryAllocateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public ulong allocationSize;
    public uint memoryTypeIndex;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkMemoryRequirements
{
    public ulong size;
    public ulong alignment;
    public uint memoryTypeBits;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkBufferCopy
{
    public ulong srcOffset;
    public ulong dstOffset;
    public ulong size;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkRenderingAttachmentInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkImageView imageView;
    public VkImageLayout imageLayout;
    public int resolveMode;
    public VkImageView resolveImageView;
    public VkImageLayout resolveImageLayout;
    public VkAttachmentLoadOp loadOp;
    public VkAttachmentStoreOp storeOp;
    public VkClearValue clearValue;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkRenderingInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkRenderingFlags flags;
    public VkRect2D renderArea;
    public uint layerCount;
    public uint viewMask;
    public uint colorAttachmentCount;
    public VkRenderingAttachmentInfo* pColorAttachments;
    public VkRenderingAttachmentInfo* pDepthAttachment;
    public nint pStencilAttachment;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VkImageMemoryBarrier2
{
    [FieldOffset(0)] public VkStructureType sType;
    [FieldOffset(8)] public nint pNext;
    [FieldOffset(16)] public ulong srcStageMask;
    [FieldOffset(24)] public ulong srcAccessMask;
    [FieldOffset(32)] public ulong dstStageMask;
    [FieldOffset(40)] public ulong dstAccessMask;
    [FieldOffset(48)] public VkImageLayout oldLayout;
    [FieldOffset(52)] public VkImageLayout newLayout;
    [FieldOffset(56)] public uint srcQueueFamilyIndex;
    [FieldOffset(60)] public uint dstQueueFamilyIndex;
    [FieldOffset(64)] public VkImage image;
    [FieldOffset(72)] public VkImageSubresourceRange subresourceRange;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkBufferMemoryBarrier2
{
    public VkStructureType sType;
    public nint pNext;
    public VkPipelineStageFlags2 srcStageMask;
    public VkAccessFlags2 srcAccessMask;
    public VkPipelineStageFlags2 dstStageMask;
    public VkAccessFlags2 dstAccessMask;
    public uint srcQueueFamilyIndex;
    public uint dstQueueFamilyIndex;
    public VkBuffer buffer;
    public ulong offset;
    public ulong size;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VkDependencyInfo
{
    [FieldOffset(0)] public VkStructureType sType;
    [FieldOffset(8)] public nint pNext;
    [FieldOffset(16)] public VkDependencyFlags dependencyFlags;
    [FieldOffset(20)] public uint memoryBarrierCount;
    [FieldOffset(24)] public nint pMemoryBarriers;
    [FieldOffset(32)] public uint bufferMemoryBarrierCount;
    [FieldOffset(40)] public VkBufferMemoryBarrier2* pBufferMemoryBarriers;
    [FieldOffset(48)] public uint imageMemoryBarrierCount;
    [FieldOffset(56)] public VkImageMemoryBarrier2* pImageMemoryBarriers;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSemaphoreSubmitInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkSemaphore semaphore;
    public ulong value;
    public ulong stageMask;
    public uint deviceIndex;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkCommandBufferSubmitInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkCommandBuffer commandBuffer;
    public uint deviceMask;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkSubmitInfo2
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint waitSemaphoreInfoCount;
    public VkSemaphoreSubmitInfo* pWaitSemaphoreInfos;
    public uint commandBufferInfoCount;
    public VkCommandBufferSubmitInfo* pCommandBufferInfos;
    public uint signalSemaphoreInfoCount;
    public VkSemaphoreSubmitInfo* pSignalSemaphoreInfos;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPresentInfoKHR
{
    public VkStructureType sType;
    public nint pNext;
    public uint waitSemaphoreCount;
    public VkSemaphore* pWaitSemaphores;
    public uint swapchainCount;
    public VkSwapchainKHR* pSwapchains;
    public uint* pImageIndices;
    public VkResult* pResults;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDebugUtilsMessengerCallbackDataEXT
{
    public VkStructureType sType;
    public nint pNext;
    public uint messageId;
    public byte* pMessageIdName;
    public uint messageSeverity;
    public uint messageTypes;
    public byte* pMessage;
    public uint queueLabelCount;
    public nint pQueueLabels;
    public uint cmdBufLabelCount;
    public nint pCmdBufLabels;
    public uint objectCount;
    public nint pObjects;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkStencilOpState
{
    public int failOp;
    public int passOp;
    public int depthFailOp;
    public int compareOp;
    public uint compareMask;
    public uint writeMask;
    public uint reference;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPipelineDepthStencilStateCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkBool32 depthTestEnable;
    public VkBool32 depthWriteEnable;
    public VkCompareOp depthCompareOp;
    public VkBool32 depthBoundsTestEnable;
    public VkBool32 stencilTestEnable;
    public VkStencilOpState front;
    public VkStencilOpState back;
    public float minDepthBounds;
    public float maxDepthBounds;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkImageCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public VkImageType imageType;
    public VkFormat format;
    public VkExtent3D extent;
    public uint mipLevels;
    public uint arrayLayers;
    public VkSampleCountFlags samples;
    public VkImageTiling tiling;
    public VkImageUsageFlags usage;
    public VkSharingMode sharingMode;
    public uint queueFamilyIndexCount;
    public uint* pQueueFamilyIndices;
    public VkImageLayout initialLayout;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkPushConstantRange
{
    public VkShaderStageFlags stageFlags;
    public uint offset;
    public uint size;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorSetLayoutBinding
{
    public uint binding;
    public VkDescriptorType descriptorType;
    public uint descriptorCount;
    public VkShaderStageFlags stageFlags;
    public nint pImmutableSamplers;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorSetLayoutCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public uint flags;
    public uint bindingCount;
    public VkDescriptorSetLayoutBinding* pBindings;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorPoolSize
{
    public VkDescriptorType type;
    public uint descriptorCount;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorPoolCreateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkDescriptorPoolCreateFlags flags;
    public uint maxSets;
    public uint poolSizeCount;
    public VkDescriptorPoolSize* pPoolSizes;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorSetAllocateInfo
{
    public VkStructureType sType;
    public nint pNext;
    public VkDescriptorPool descriptorPool;
    public uint descriptorSetCount;
    public VkDescriptorSetLayout* pSetLayouts;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkDescriptorBufferInfo
{
    public VkBuffer buffer;
    public ulong offset;
    public ulong range;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct VkWriteDescriptorSet
{
    public VkStructureType sType;
    public nint pNext;
    public VkDescriptorSet dstSet;
    public uint dstBinding;
    public uint dstArrayElement;
    public uint descriptorCount;
    public VkDescriptorType descriptorType;
    public nint pImageInfo;
    public VkDescriptorBufferInfo* pBufferInfo;
    public nint pTexelBufferView;
}
