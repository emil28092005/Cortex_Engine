using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

public enum VkResult : int
{
    Success = 0,
    NotReady = 1,
    Timeout = 2,
    EventSet = 3,
    EventReset = 4,
    Incomplete = 5,
    ErrorOutOfHostMemory = -1,
    ErrorOutOfDeviceMemory = -2,
    ErrorInitializationFailed = -3,
    ErrorDeviceLost = -4,
    ErrorMemoryMapFailed = -5,
    ErrorLayerNotPresent = -6,
    ErrorExtensionNotPresent = -7,
    ErrorFeatureNotPresent = -8,
    ErrorIncompatibleDriver = -9,
    ErrorTooManyObjects = -10,
    ErrorFormatNotSupported = -11,
    ErrorSurfaceLostKHR = -1000000000,
    ErrorNativeWindowInUseKHR = -1000000001,
    SuboptimalKHR = 1000001003,
    ErrorOutOfDateKHR = -1000001004,
    ErrorValidationFailedEXT = -1000011001,
}

public enum VkStructureType : int
{
    ApplicationInfo = 0,
    InstanceCreateInfo = 1,
    DeviceQueueCreateInfo = 2,
    DeviceCreateInfo = 3,
    SubmitInfo = 4,
    MemoryAllocateInfo = 5,
    MappedMemoryRange = 6,
    BindSparseInfo = 7,
    FenceCreateInfo = 8,
    SemaphoreCreateInfo = 9,
    EventCreateInfo = 10,
    QueryPoolCreateInfo = 11,
    BufferCreateInfo = 12,
    BufferViewCreateInfo = 13,
    ImageCreateInfo = 14,
    ImageViewCreateInfo = 15,
    ShaderModuleCreateInfo = 16,
    PipelineCacheCreateInfo = 17,
    PipelineShaderStageCreateInfo = 18,
    PipelineVertexInputStateCreateInfo = 19,
    PipelineInputAssemblyStateCreateInfo = 20,
    PipelineTessellationStateCreateInfo = 21,
    PipelineViewportStateCreateInfo = 22,
    PipelineRasterizationStateCreateInfo = 23,
    PipelineMultisampleStateCreateInfo = 24,
    PipelineDepthStencilStateCreateInfo = 25,
    PipelineColorBlendStateCreateInfo = 26,
    PipelineDynamicStateCreateInfo = 27,
    GraphicsPipelineCreateInfo = 28,
    ComputePipelineCreateInfo = 29,
    PipelineLayoutCreateInfo = 30,
    SamplerCreateInfo = 31,
    DescriptorSetLayoutCreateInfo = 32,
    DescriptorPoolCreateInfo = 33,
    DescriptorSetAllocateInfo = 34,
    WriteDescriptorSet = 35,
    CopyDescriptorSet = 36,
    FramebufferCreateInfo = 37,
    RenderPassCreateInfo = 38,
    CommandPoolCreateInfo = 39,
    CommandBufferAllocateInfo = 40,
    CommandBufferInheritanceInfo = 41,
    CommandBufferBeginInfo = 42,
    RenderPassBeginInfo = 43,
    BufferMemoryBarrier = 44,
    ImageMemoryBarrier = 45,
    SwapchainCreateInfoKHR = 1000001000,
    PresentInfoKHR = 1000001001,
    SurfaceCapabilitiesKHR = 1000000000,
    SurfaceFormatKHR = 1000000001,
}

public enum VkFormat : int
{
    Undefined = 0,
    R8G8B8A8Unorm = 37,
    B8G8R8A8Unorm = 44,
    R8G8B8A8Srgb = 43,
    B8G8R8A8Srgb = 50,
    R32G32B32A32Sfloat = 109,
    R32G32B32Sfloat = 106,
    R16G16B16A16Sfloat = 97,
    R32G32Sfloat = 103,
    D32Sfloat = 126,
    D32SfloatS8Uint = 127,
    D24UnormS8Uint = 129,
    D16Unorm = 124,
}

public enum VkImageUsageFlags : uint
{
    TransferSrc = 0x00000001,
    TransferDst = 0x00000002,
    Sampled = 0x00000004,
    Storage = 0x00000008,
    ColorAttachment = 0x00000010,
    DepthStencilAttachment = 0x00000020,
}

public enum VkImageAspectFlags : uint
{
    Color = 0x00000001,
    Depth = 0x00000002,
    Stencil = 0x00000004,
}

public enum VkImageLayout : int
{
    Undefined = 0,
    General = 1,
    ColorAttachmentOptimal = 2,
    DepthStencilAttachmentOptimal = 3,
    DepthStencilReadOnlyOptimal = 4,
    ShaderReadOnlyOptimal = 5,
    TransferSrcOptimal = 6,
    TransferDstOptimal = 7,
    Preinitialized = 8,
    PresentSrcKHR = 1000001002,
}

public enum VkPipelineStageFlags : uint
{
    TopOfPipe = 0x00000001,
    DrawIndirect = 0x00000002,
    VertexInput = 0x00000004,
    VertexShader = 0x00000008,
    FragmentShader = 0x00000010,
    EarlyFragmentTests = 0x00000040,
    LateFragmentTests = 0x00000080,
    ColorAttachmentOutput = 0x00000100,
    Transfer = 0x00001000,
    BottomOfPipe = 0x00002000,
    Host = 0x00004000,
    AllGraphics = 0x00008000,
    AllCommands = 0x00010000,
}

public enum VkAccessFlags : uint
{
    IndirectCommandRead = 0x00000001,
    IndexRead = 0x00000002,
    VertexAttributeRead = 0x00000004,
    UniformRead = 0x00000008,
    InputAttachmentRead = 0x00000010,
    ShaderRead = 0x00000020,
    ShaderWrite = 0x00000040,
    ColorAttachmentRead = 0x00000080,
    ColorAttachmentWrite = 0x00000100,
    DepthStencilAttachmentRead = 0x00000200,
    DepthStencilAttachmentWrite = 0x00000400,
    TransferRead = 0x00000800,
    TransferWrite = 0x00001000,
    HostRead = 0x00002000,
    HostWrite = 0x00004000,
    MemoryRead = 0x00008000,
    MemoryWrite = 0x00010000,
}

public enum VkCommandBufferLevel : int
{
    Primary = 0,
    Secondary = 1,
}

public enum VkCommandBufferUsageFlags : uint
{
    OneTimeSubmit = 0x00000001,
    RenderPassContinue = 0x00000002,
    SimultaneousUse = 0x00000004,
}

public enum VkIndexType : int
{
    Uint16 = 0,
    Uint32 = 1,
}

public enum VkPrimitiveTopology : int
{
    PointList = 0,
    LineList = 1,
    LineStrip = 2,
    TriangleList = 3,
    TriangleStrip = 4,
    TriangleFan = 5,
}

public enum VkPolygonMode : int
{
    Fill = 0,
    Line = 1,
    Point = 2,
}

public enum VkCullModeFlags : uint
{
    None = 0,
    Front = 0x00000001,
    Back = 0x00000002,
    FrontAndBack = 0x00000003,
}

public enum VkDynamicState : int
{
    Viewport = 0,
    Scissor = 1,
}

public enum VkFrontFace : int
{
    CounterClockwise = 0,
    Clockwise = 1,
}

public enum VkBlendFactor : int
{
    Zero = 0,
    One = 1,
    SrcColor = 2,
    OneMinusSrcColor = 3,
    DstColor = 4,
    OneMinusDstColor = 5,
    SrcAlpha = 6,
    OneMinusSrcAlpha = 7,
    DstAlpha = 8,
    OneMinusDstAlpha = 9,
}

public enum VkBlendOp : int
{
    Add = 0,
    Subtract = 1,
    ReverseSubtract = 2,
}

public enum VkColorComponentFlags : uint
{
    R = 0x00000001,
    G = 0x00000002,
    B = 0x00000004,
    A = 0x00000008,
}

public enum VkCompareOp : int
{
    Never = 0,
    Less = 1,
    Equal = 2,
    LessOrEqual = 3,
    Greater = 4,
    NotEqual = 5,
    GreaterOrEqual = 6,
    Always = 7,
}

public enum VkDescriptorType : int
{
    Sampler = 0,
    CombinedImageSampler = 1,
    SampledImage = 2,
    StorageImage = 3,
    UniformTexelBuffer = 4,
    StorageTexelBuffer = 5,
    UniformBuffer = 6,
    StorageBuffer = 7,
    UniformBufferDynamic = 8,
    StorageBufferDynamic = 9,
}

public enum VkShaderStageFlags : uint
{
    Vertex = 0x00000001,
    Fragment = 0x00000010,
    AllGraphics = 0x0000001F,
}

public enum VkBufferUsageFlags : uint
{
    TransferSrc = 0x00000001,
    TransferDst = 0x00000002,
    UniformTexelBuffer = 0x00000004,
    StorageTexelBuffer = 0x00000008,
    UniformBuffer = 0x00000010,
    StorageBuffer = 0x00000020,
    IndexBuffer = 0x00000040,
    VertexBuffer = 0x00000080,
    IndirectBuffer = 0x00000100,
}

public enum VkMemoryPropertyFlags : uint
{
    DeviceLocal = 0x00000001,
    HostVisible = 0x00000002,
    HostCoherent = 0x00000004,
    HostCached = 0x00000008,
    LazilyAllocated = 0x00000010,
}

public enum VkQueueFlags : uint
{
    Graphics = 0x00000001,
    Compute = 0x00000002,
    Transfer = 0x00000004,
    SparseBinding = 0x00000008,
}

public enum VkPhysicalDeviceType : int
{
    Other = 0,
    IntegratedGpu = 1,
    DiscreteGpu = 2,
    VirtualGpu = 3,
    Cpu = 4,
}

public enum VkPresentModeKHR : int
{
    Immediate = 0,
    Fifo = 1,
    FifoRelaxed = 2,
    Mailbox = 3,
}

public enum VkColorSpaceKHR : int
{
    SrgbNonlinear = 0,
}

public enum VkSurfaceTransformFlagsKHR : uint
{
    Identity = 0x00000001,
}

public enum VkAttachmentLoadOp : int
{
    Load = 0,
    Clear = 1,
    DontCare = 2,
}

public enum VkAttachmentStoreOp : int
{
    Store = 0,
    DontCare = 1,
}

public enum VkSampleCountFlags : uint
{
    One = 0x00000001,
}

public enum VkSharingMode : int
{
    Exclusive = 0,
    Concurrent = 1,
}

public enum VkFenceCreateFlags : uint
{
    Signaled = 0x00000001,
}

public enum VkDescriptorPoolCreateFlags : uint
{
    FreeDescriptorSet = 0x00000001,
}

public enum VkSubpassContents : int
{
    Inline = 0,
    SecondaryCommandBuffers = 1,
}

public enum VkImageViewType : int
{
    _1D = 0,
    _2D = 1,
    _3D = 2,
    Cube = 3,
    _1DArray = 4,
    _2DArray = 5,
    CubeArray = 6,
}

public enum VkImageType : int
{
    _1D = 0,
    _2D = 1,
    _3D = 2,
}

public enum VkSamplerAddressMode : int
{
    Repeat = 0,
    MirroredRepeat = 1,
    ClampToEdge = 2,
    ClampToBorder = 3,
}

public enum VkFilter : int
{
    Nearest = 0,
    Linear = 1,
}

public enum VkSamplerMipmapMode : int
{
    Nearest = 0,
    Linear = 1,
}

public enum VkBorderColor : int
{
    FloatTransparentBlack = 4,
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkHandle { public ulong Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkInstance { public ulong Value; public static implicit operator ulong(VkInstance h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPhysicalDevice { public ulong Value; public static implicit operator ulong(VkPhysicalDevice h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDevice { public ulong Value; public static implicit operator ulong(VkDevice h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkQueue { public ulong Value; public static implicit operator ulong(VkQueue h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkCommandBuffer { public ulong Value; public static implicit operator ulong(VkCommandBuffer h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkCommandPool { public ulong Value; public static implicit operator ulong(VkCommandPool h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkBuffer { public ulong Value; public static implicit operator ulong(VkBuffer h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDeviceMemory { public ulong Value; public static implicit operator ulong(VkDeviceMemory h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkImage { public ulong Value; public static implicit operator ulong(VkImage h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkImageView { public ulong Value; public static implicit operator ulong(VkImageView h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkShaderModule { public ulong Value; public static implicit operator ulong(VkShaderModule h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineLayout { public ulong Value; public static implicit operator ulong(VkPipelineLayout h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipeline { public ulong Value; public static implicit operator ulong(VkPipeline h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSampler { public ulong Value; public static implicit operator ulong(VkSampler h) => h.Value; }

unsafe public struct VkRenderPass { public ulong Value; public static implicit operator ulong(VkRenderPass h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkFramebuffer { public ulong Value; public static implicit operator ulong(VkFramebuffer h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorSetLayout { public ulong Value; public static implicit operator ulong(VkDescriptorSetLayout h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorPool { public ulong Value; public static implicit operator ulong(VkDescriptorPool h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorSet { public ulong Value; public static implicit operator ulong(VkDescriptorSet h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSemaphore { public ulong Value; public static implicit operator ulong(VkSemaphore h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkFence { public ulong Value; public static implicit operator ulong(VkFence h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSwapchainKHR { public ulong Value; public static implicit operator ulong(VkSwapchainKHR h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSurfaceKHR { public ulong Value; public static implicit operator ulong(VkSurfaceKHR h) => h.Value; }

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkApplicationInfo
{
    public VkStructureType sType;
    public void* pNext;
    public byte* pApplicationName;
    public uint applicationVersion;
    public byte* pEngineName;
    public uint engineVersion;
    public uint apiVersion;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkInstanceCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public VkApplicationInfo* pApplicationInfo;
    public uint enabledLayerCount;
    public byte** ppEnabledLayerNames;
    public uint enabledExtensionCount;
    public byte** ppEnabledExtensionNames;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDeviceQueueCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint queueFamilyIndex;
    public uint queueCount;
    public float* pQueuePriorities;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDeviceCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint queueCreateInfoCount;
    public VkDeviceQueueCreateInfo* pQueueCreateInfos;
    public uint enabledLayerCount;
    public byte** ppEnabledLayerNames;
    public uint enabledExtensionCount;
    public byte** ppEnabledExtensionNames;
    public void* pEnabledFeatures;
}

[StructLayout(LayoutKind.Sequential, Size = 228)]
public struct VkPhysicalDeviceFeatures
{
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
    public fixed uint pipelineCacheUUID[16];
    public VkPhysicalDeviceLimits limits;
    public VkPhysicalDeviceSparseProperties sparseProperties;
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
    public ulong bufferImageGranularity;
    public ulong sparseAddressSpaceSize;
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
    public fixed uint maxComputeWorkGroupCount[3];
    public uint maxComputeWorkGroupInvocations;
    public fixed uint maxComputeWorkGroupSize[3];
    public float subPixelPrecisionBits;
    public float subTexelPrecisionBits;
    public float mipmapPrecisionBits;
    public uint maxDrawIndexedIndexValue;
    public uint maxDrawIndirectCount;
    public float maxSamplerLodBias;
    public float maxSamplerAnisotropy;
    public uint maxViewports;
    public fixed uint maxViewportDimensions[2];
    public fixed float viewportBoundsRange[2];
    public uint viewportSubPixelBits;
    public uint minMemoryMapAlignment;
    public uint minTexelBufferOffsetAlignment;
    public uint minUniformBufferOffsetAlignment;
    public uint minStorageBufferOffsetAlignment;
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
    public uint framebufferColorSampleCounts;
    public uint framebufferDepthSampleCounts;
    public uint framebufferStencilSampleCounts;
    public uint framebufferNoAttachmentsSampleCounts;
    public uint maxColorAttachments;
    public uint sampledImageColorSampleCounts;
    public uint sampledImageIntegerSampleCounts;
    public uint sampledImageDepthSampleCounts;
    public uint sampledImageStencilSampleCounts;
    public uint storageImageSampleCounts;
    public uint maxSampleMaskWords;
    public uint timestampComputeAndGraphics;
    public float timestampPeriod;
    public uint maxClipDistances;
    public uint maxCullDistances;
    public uint maxCombinedClipAndCullDistances;
    public uint discreteQueuePriorities;
    public fixed float pointSizeRange[2];
    public fixed float lineWidthRange[2];
    public float pointSizeGranularity;
    public float lineWidthGranularity;
    public uint strictLines;
    public uint standardSampleLocations;
    public uint optimalBufferCopyOffsetAlignment;
    public uint optimalBufferCopyRowPitchAlignment;
    public uint nonCoherentAtomSize;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPhysicalDeviceSparseProperties
{
    public uint residencyStandard2DBlockShape;
    public uint residencyStandard2DMultisampleBlockShape;
    public uint residencyStandard3DBlockShape;
    public uint residencyAlignedMipSize;
    public uint residencyNonResidentStrict;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkQueueFamilyProperties
{
    public VkQueueFlags queueFlags;
    public uint queueCount;
    public uint timestampValidBits;
    public VkExtent3D minImageTransferGranularity;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkExtent3D
{
    public int width;
    public int height;
    public int depth;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkExtent2D
{
    public int width;
    public int height;
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
    public uint memoryHeapCount;
    public VkMemoryHeap memoryHeaps0;
    public VkMemoryHeap memoryHeaps1;
    public VkMemoryHeap memoryHeaps2;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkMemoryType
{
    public VkMemoryPropertyFlags propertyFlags;
    public uint heapIndex;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkMemoryHeap
{
    public ulong size;
    public uint flags;
    public uint _pad;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSurfaceCapabilitiesKHR
{
    public uint minImageCount;
    public uint maxImageCount;
    public VkExtent2D currentExtent;
    public VkExtent2D minImageExtent;
    public VkExtent2D maxImageExtent;
    public uint maxImageArrayLayers;
    public VkSurfaceTransformFlagsKHR currentTransform;
    public uint supportedTransforms;
    public uint supportedCompositeAlpha;
    public uint supportedUsageFlags;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSurfaceFormatKHR
{
    public VkFormat format;
    public VkColorSpaceKHR colorSpace;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSwapchainCreateInfoKHR
{
    public VkStructureType sType;
    public void* pNext;
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
    public uint compositeAlpha;
    public VkPresentModeKHR presentMode;
    public uint clipped;
    public VkSwapchainKHR oldSwapchain;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPresentInfoKHR
{
    public VkStructureType sType;
    public void* pNext;
    public uint waitSemaphoreCount;
    public VkSemaphore* pWaitSemaphores;
    public uint swapchainCount;
    public VkSwapchainKHR* pSwapchains;
    public uint* pImageIndices;
    public VkResult* pResults;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSubmitInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint waitSemaphoreCount;
    public VkSemaphore* pWaitSemaphores;
    public ulong* pWaitDstStageMask;
    public uint commandBufferCount;
    public VkCommandBuffer* pCommandBuffers;
    public uint signalSemaphoreCount;
    public VkSemaphore* pSignalSemaphores;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkImageCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public VkImageType imageType;
    public VkFormat format;
    public VkExtent3D extent;
    public uint mipLevels;
    public uint arrayLayers;
    public VkSampleCountFlags samples;
    public uint tiling;
    public VkImageUsageFlags usage;
    public VkSharingMode sharingMode;
    public uint queueFamilyIndexCount;
    public uint* pQueueFamilyIndices;
    public int initialLayout;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkImageViewCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public VkImage image;
    public VkImageViewType viewType;
    public VkFormat format;
    public VkComponentMapping components;
    public VkImageSubresourceRange subresourceRange;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkComponentMapping
{
    public int r;
    public int g;
    public int b;
    public int a;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkImageSubresourceRange
{
    public VkImageAspectFlags aspectMask;
    public uint baseMipLevel;
    public uint levelCount;
    public uint baseArrayLayer;
    public uint layerCount;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkMemoryAllocateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public ulong allocationSize;
    public uint memoryTypeIndex;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkBufferCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public ulong size;
    public VkBufferUsageFlags usage;
    public VkSharingMode sharingMode;
    public uint queueFamilyIndexCount;
    public uint* pQueueFamilyIndices;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkShaderModuleCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public ulong codeSize;
    public uint* pCode;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineShaderStageCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public VkShaderStageFlags stage;
    public VkShaderModule module;
    public byte* pName;
    public void* pSpecializationInfo;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineLayoutCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint setLayoutCount;
    public VkDescriptorSetLayout* pSetLayouts;
    public uint pushConstantRangeCount;
    public VkPushConstantRange* pPushConstantRanges;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPushConstantRange
{
    public VkShaderStageFlags stageFlags;
    public uint offset;
    public uint size;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorSetLayoutCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint bindingCount;
    public VkDescriptorSetLayoutBinding* pBindings;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorSetLayoutBinding
{
    public uint binding;
    public VkDescriptorType descriptorType;
    public uint descriptorCount;
    public VkShaderStageFlags stageFlags;
    public void* pImmutableSamplers;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorPoolCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public VkDescriptorPoolCreateFlags flags;
    public uint maxSets;
    public uint poolSizeCount;
    public VkDescriptorPoolSize* pPoolSizes;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorPoolSize
{
    public VkDescriptorType type;
    public uint descriptorCount;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorSetAllocateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public VkDescriptorPool descriptorPool;
    public uint descriptorSetCount;
    public VkDescriptorSetLayout* pSetLayouts;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkWriteDescriptorSet
{
    public VkStructureType sType;
    public void* pNext;
    public VkDescriptorSet dstSet;
    public uint dstBinding;
    public uint dstArrayElement;
    public uint descriptorCount;
    public VkDescriptorType descriptorType;
    public void* pImageInfo;
    public void* pBufferInfo;
    public void* pTexelBufferView;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkDescriptorBufferInfo
{
    public VkBuffer buffer;
    public ulong offset;
    public ulong range;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkVertexInputBindingDescription
{
    public uint binding;
    public uint stride;
    public int inputRate;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkVertexInputAttributeDescription
{
    public uint location;
    public uint binding;
    public VkFormat format;
    public uint offset;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineVertexInputStateCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint vertexBindingDescriptionCount;
    public VkVertexInputBindingDescription* pVertexBindingDescriptions;
    public uint vertexAttributeDescriptionCount;
    public VkVertexInputAttributeDescription* pVertexAttributeDescriptions;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineInputAssemblyStateCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public VkPrimitiveTopology topology;
    public uint primitiveRestartEnable;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkViewport
{
    public float x;
    public float y;
    public float width;
    public float height;
    public float minDepth;
    public float maxDepth;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkRect2D
{
    public VkOffset2D offset;
    public VkExtent2D extent;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkOffset2D
{
    public int x;
    public int y;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineViewportStateCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint viewportCount;
    public VkViewport* pViewports;
    public uint scissorCount;
    public VkRect2D* pScissors;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineRasterizationStateCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint depthClampEnable;
    public uint rasterizerDiscardEnable;
    public VkPolygonMode polygonMode;
    public VkCullModeFlags cullMode;
    public VkFrontFace frontFace;
    public uint depthBiasEnable;
    public float depthBiasConstantFactor;
    public float depthBiasClamp;
    public float depthBiasSlopeFactor;
    public float lineWidth;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineMultisampleStateCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public VkSampleCountFlags rasterizationSamples;
    public uint sampleShadingEnable;
    public float minSampleShading;
    public void* pSampleMask;
    public uint alphaToCoverageEnable;
    public uint alphaToOneEnable;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineDepthStencilStateCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint depthTestEnable;
    public uint depthWriteEnable;
    public VkCompareOp depthCompareOp;
    public uint depthBoundsTestEnable;
    public uint stencilTestEnable;
    public VkStencilOpState front;
    public VkStencilOpState back;
    public float minDepthBounds;
    public float maxDepthBounds;
}

[StructLayout(LayoutKind.Sequential)]
public struct VkStencilOpState
{
    public int failOp;
    public int passOp;
    public int depthFailOp;
    public VkCompareOp compareOp;
    public uint compareMask;
    public uint writeMask;
    public uint reference;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineColorBlendAttachmentState
{
    public uint blendEnable;
    public VkBlendFactor srcColorBlendFactor;
    public VkBlendFactor dstColorBlendFactor;
    public VkBlendOp colorBlendOp;
    public VkBlendFactor srcAlphaBlendFactor;
    public VkBlendFactor dstAlphaBlendFactor;
    public VkBlendOp alphaBlendOp;
    public VkColorComponentFlags colorWriteMask;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineColorBlendStateCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint logicOpEnable;
    public int logicOp;
    public uint attachmentCount;
    public VkPipelineColorBlendAttachmentState* pAttachments;
    public fixed float blendConstants[4];
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkPipelineDynamicStateCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint dynamicStateCount;
    public VkDynamicState* pDynamicStates;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkGraphicsPipelineCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint stageCount;
    public VkPipelineShaderStageCreateInfo* pStages;
    public VkPipelineVertexInputStateCreateInfo* pVertexInputState;
    public VkPipelineInputAssemblyStateCreateInfo* pInputAssemblyState;
    public void* pTessellationState;
    public VkPipelineViewportStateCreateInfo* pViewportState;
    public VkPipelineRasterizationStateCreateInfo* pRasterizationState;
    public VkPipelineMultisampleStateCreateInfo* pMultisampleState;
    public VkPipelineDepthStencilStateCreateInfo* pDepthStencilState;
    public VkPipelineColorBlendStateCreateInfo* pColorBlendState;
    public void* pDynamicState;
    public VkPipelineLayout layout;
    public VkRenderPass renderPass;
    public uint subpass;
    public VkPipeline basePipelineHandle;
    public int basePipelineIndex;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkRenderPassCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint attachmentCount;
    public VkAttachmentDescription* pAttachments;
    public uint subpassCount;
    public VkSubpassDescription* pSubpasses;
    public uint dependencyCount;
    public VkSubpassDependency* pDependencies;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkAttachmentDescription
{
    public uint flags;
    public VkFormat format;
    public uint samples;
    public VkAttachmentLoadOp loadOp;
    public VkAttachmentStoreOp storeOp;
    public VkAttachmentLoadOp stencilLoadOp;
    public VkAttachmentStoreOp stencilStoreOp;
    public VkImageLayout initialLayout;
    public VkImageLayout finalLayout;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkAttachmentReference
{
    public uint attachment;
    public VkImageLayout layout;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSubpassDescription
{
    public uint flags;
    public int pipelineBindPoint;
    public uint inputAttachmentCount;
    public VkAttachmentReference* pInputAttachments;
    public uint colorAttachmentCount;
    public VkAttachmentReference* pColorAttachments;
    public VkAttachmentReference* pResolveAttachments;
    public VkAttachmentReference* pDepthStencilAttachment;
    public uint preserveAttachmentCount;
    public uint* pPreserveAttachments;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSubpassDependency
{
    public uint srcSubpass;
    public uint dstSubpass;
    public VkPipelineStageFlags srcStageMask;
    public VkPipelineStageFlags dstStageMask;
    public VkAccessFlags srcAccessMask;
    public VkAccessFlags dstAccessMask;
    public int dependencyFlags;
    public int viewOffset;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkFramebufferCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public VkRenderPass renderPass;
    public uint attachmentCount;
    public VkImageView* pAttachments;
    public uint width;
    public uint height;
    public uint layers;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkCommandPoolCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public uint queueFamilyIndex;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkCommandBufferAllocateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public VkCommandPool commandPool;
    public VkCommandBufferLevel level;
    public uint commandBufferCount;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkCommandBufferBeginInfo
{
    public VkStructureType sType;
    public void* pNext;
    public VkCommandBufferUsageFlags flags;
    public void* pInheritanceInfo;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkRenderPassBeginInfo
{
    public VkStructureType sType;
    public void* pNext;
    public VkRenderPass renderPass;
    public VkFramebuffer framebuffer;
    public VkRect2D renderArea;
    public uint clearValueCount;
    public VkClearValue* pClearValues;
}

[StructLayout(LayoutKind.Explicit)]
public struct VkClearValue
{
    [FieldOffset(0)] public VkClearColorValue color;
    [FieldOffset(0)] public VkClearDepthStencilValue depthStencil;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkClearColorValue
{
    public float r, g, b, a;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkClearDepthStencilValue
{
    public float depth;
    public uint stencil;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkBufferCopy
{
    public ulong srcOffset;
    public ulong dstOffset;
    public ulong size;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkBufferImageCopy
{
    public ulong bufferOffset;
    public uint bufferRowLength;
    public uint bufferImageHeight;
    public VkImageSubresourceLayers imageSubresource;
    public VkOffset3D imageOffset;
    public VkExtent3D imageExtent;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkImageSubresourceLayers
{
    public VkImageAspectFlags aspectMask;
    public uint mipLevel;
    public uint baseArrayLayer;
    public uint layerCount;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkOffset3D
{
    public int x;
    public int y;
    public int z;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkImageMemoryBarrier
{
    public VkStructureType sType;
    public void* pNext;
    public VkAccessFlags srcAccessMask;
    public VkAccessFlags dstAccessMask;
    public VkImageLayout oldLayout;
    public VkImageLayout newLayout;
    public uint srcQueueFamilyIndex;
    public uint dstQueueFamilyIndex;
    public VkImage image;
    public VkImageSubresourceRange subresourceRange;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkBufferMemoryBarrier
{
    public VkStructureType sType;
    public void* pNext;
    public VkAccessFlags srcAccessMask;
    public VkAccessFlags dstAccessMask;
    public uint srcQueueFamilyIndex;
    public uint dstQueueFamilyIndex;
    public VkBuffer buffer;
    public ulong offset;
    public ulong size;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSemaphoreCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkFenceCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public VkFenceCreateFlags flags;
}

[StructLayout(LayoutKind.Sequential, Size = 260)]
public unsafe struct VkExtensionProperties
{
    public fixed byte extensionName[256];
    public uint specVersion;
}

[StructLayout(LayoutKind.Sequential, Size = 264)]
public unsafe struct VkLayerProperties
{
    public fixed byte layerName[256];
    public uint specVersion;
    public uint implementationVersion;
    public uint _pad;
}

[StructLayout(LayoutKind.Sequential)]
unsafe public struct VkSamplerCreateInfo
{
    public VkStructureType sType;
    public void* pNext;
    public uint flags;
    public VkFilter magFilter;
    public VkFilter minFilter;
    public VkSamplerMipmapMode mipmapMode;
    public VkSamplerAddressMode addressModeU;
    public VkSamplerAddressMode addressModeV;
    public VkSamplerAddressMode addressModeW;
    public float mipLodBias;
    public uint anisotropyEnable;
    public float maxAnisotropy;
    public uint compareEnable;
    public VkCompareOp compareOp;
    public float minLod;
    public float maxLod;
    public VkBorderColor borderColor;
    public uint unnormalizedCoordinates;
}
