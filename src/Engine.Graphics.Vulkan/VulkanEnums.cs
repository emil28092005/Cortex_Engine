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
    ErrorIncompatibleDriver = -8,
    ErrorTooManyObjects = -9,
    ErrorFormatNotSupported = -10,
    ErrorFragmentedPool = -11,
    ErrorUnknown = -13,
    ErrorOutOfPoolMemory = -1000069000,
    ErrorInvalidExternalHandle = -1000072003,
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
    BufferCreateInfo = 12,
    ShaderModuleCreateInfo = 16,
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
    PipelineLayoutCreateInfo = 30,
    RenderPassCreateInfo = 38,
    CommandPoolCreateInfo = 39,
    CommandBufferAllocateInfo = 40,
    CommandBufferBeginInfo = 42,
    RenderPassBeginInfo = 43,
    ImageViewCreateInfo = 15,
    SemaphoreCreateInfo = 9,
    FenceCreateInfo = 8,
    SwapchainCreateInfoKHR = 1000001000,
    PresentInfoKHR = 1000001001,
    DebugUtilsMessengerCreateInfoEXT = 1000128004,
    SubmitInfo2 = 1000314004,
    CommandBufferSubmitInfo = 1000314006,
    SemaphoreSubmitInfo = 1000314005,
    PipelineRenderingCreateInfo = 1000044002,
    RenderingInfo = 1000044000,
    RenderingAttachmentInfo = 1000044001,
    ImageMemoryBarrier2 = 1000314002,
    BufferMemoryBarrier2 = 1000314001,
    DependencyInfo = 1000314003,
    PhysicalDeviceDynamicRenderingFeatures = 1000044003,
    PhysicalDeviceSynchronization2Features = 1000314007,
}

public enum VkFormat : int
{
    Undefined = 0,
    R8G8B8A8Unorm = 37,
    B8G8R8A8Unorm = 44,
    R8G8B8A8Srgb = 43,
    B8G8R8A8Srgb = 50,
    R32G32Sfloat = 103,
    R32G32B32Sfloat = 106,
    R32G32B32A32Sfloat = 109,
    D32Sfloat = 126,
}

public enum VkColorSpaceKHR : int
{
    SrgbNonlinearKHR = 0,
}

public enum VkPresentModeKHR : int
{
    Immediate = 0,
    Mailbox = 1,
    Fifo = 2,
    FifoRelaxed = 3,
}

public enum VkImageUsageFlags : uint
{
    TransferSrc = 0x00000001,
    TransferDst = 0x00000002,
    Sampled = 0x00000004,
    Storage = 0x00000008,
    ColorAttachment = 0x00000010,
    DepthStencilAttachment = 0x00000020,
    TransientAttachment = 0x00000040,
    InputAttachment = 0x00000080,
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

public enum VkImageAspectFlags : uint
{
    Color = 0x00000001,
    Depth = 0x00000002,
    Stencil = 0x00000004,
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
    None = 1000301000,
}

public enum VkSharingMode : int
{
    Exclusive = 0,
    Concurrent = 1,
}

public enum VkCompositeAlphaFlagsKHR : uint
{
    Opaque = 0x00000001,
    PreMultiplied = 0x00000002,
    PostMultiplied = 0x00000004,
    Inherit = 0x00000008,
}

public enum VkSurfaceTransformFlagsKHR : uint
{
    Identity = 0x00000001,
    Rotate90 = 0x00000002,
    Rotate180 = 0x00000004,
    Rotate270 = 0x00000008,
    HorizontalMirror = 0x00000010,
    Inherit = 0x00000100,
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
    ConstantColor = 10,
    OneMinusConstantColor = 11,
    ConstantAlpha = 12,
    OneMinusConstantAlpha = 13,
    SrcAlphaSaturate = 14,
    Src1Color = 15,
    OneMinusSrc1Color = 16,
    Src1Alpha = 17,
    OneMinusSrc1Alpha = 18,
}

public enum VkBlendOp : int
{
    Add = 0,
    Subtract = 1,
    ReverseSubtract = 2,
    Min = 3,
    Max = 4,
}

public enum VkColorComponentFlags : uint
{
    R = 0x00000001,
    G = 0x00000002,
    B = 0x00000004,
    A = 0x00000008,
}

public enum VkShaderStageFlags : uint
{
    Vertex = 0x00000001,
    TessellationControl = 0x00000002,
    TessellationEvaluation = 0x00000004,
    Geometry = 0x00000008,
    Fragment = 0x00000010,
    Compute = 0x00000020,
    AllGraphics = 0x0000001F,
}

public enum VkPipelineStageFlags2 : ulong
{
    None = 0,
    TopOfPipe = 0x00000001,
    DrawIndirect = 0x00000002,
    VertexInput = 0x00000004,
    VertexShader = 0x00000008,
    TessellationControlShader = 0x00000010,
    TessellationEvaluationShader = 0x00000020,
    GeometryShader = 0x00000040,
    FragmentShader = 0x00000080,
    EarlyFragmentTests = 0x00000100,
    LateFragmentTests = 0x00000200,
    ColorAttachmentOutput = 0x00000400,
    ComputeShader = 0x00000800,
    Transfer = 0x00001000,
    BottomOfPipe = 0x00002000,
    Host = 0x00004000,
    AllGraphics = 0x00008000,
    AllCommands = 0x00010000,
}

public enum VkAccessFlags2 : ulong
{
    None = 0,
    ColorAttachmentRead = 0x00000080,
    ColorAttachmentWrite = 0x00000100,
    TransferRead = 0x00000800,
    TransferWrite = 0x00001000,
    ShaderRead = 0x100000000,
    ShaderWrite = 0x200000000,
}

public enum VkDynamicState : int
{
    Viewport = 0,
    Scissor = 1,
    LineWidth = 2,
    DepthBias = 3,
    BlendConstants = 4,
    DepthBounds = 5,
    StencilCompareMask = 6,
    StencilWriteMask = 7,
    StencilReference = 8,
}

public enum VkCommandBufferLevel : int
{
    Primary = 0,
    Secondary = 1,
}

public enum VkCommandBufferUsageFlags : uint
{
    None = 0,
    OneTimeSubmit = 0x00000001,
    RenderPassContinue = 0x00000002,
    SimultaneousUse = 0x00000004,
}

public enum VkFenceCreateFlags : uint
{
    None = 0,
    Signaled = 0x00000001,
}

public enum VkMemoryPropertyFlags : uint
{
    None = 0,
    DeviceLocal = 0x00000001,
    HostVisible = 0x00000002,
    HostCoherent = 0x00000004,
    HostCached = 0x00000008,
    LazilyAllocated = 0x00000010,
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

public enum VkQueueFlags : uint
{
    Graphics = 0x00000001,
    Compute = 0x00000002,
    Transfer = 0x00000004,
    SparseBinding = 0x00000008,
    Protected = 0x00000010,
}

public enum VkPhysicalDeviceType : int
{
    Other = 0,
    IntegratedGpu = 1,
    DiscreteGpu = 2,
    VirtualGpu = 3,
    Cpu = 4,
}

public enum VkSampleCountFlags : uint
{
    Count1 = 0x00000001,
    Count2 = 0x00000002,
    Count4 = 0x00000004,
    Count8 = 0x00000008,
    Count16 = 0x00000010,
    Count32 = 0x00000020,
    Count64 = 0x00000040,
}

public enum VkImageViewType : int
{
    Type1D = 0,
    Type2D = 1,
    Type3D = 2,
    TypeCube = 3,
    Type1DArray = 4,
    Type2DArray = 5,
    TypeCubeArray = 6,
}

public enum VkComponentSwizzle : int
{
    Identity = 0,
    Zero = 1,
    One = 2,
    R = 3,
    G = 4,
    B = 5,
    A = 6,
}

public enum VkBool32 : uint
{
    False = 0,
    True = 1,
}

public enum VkRenderingFlags : uint
{
    None = 0,
    ContentsSecondaryCommandBuffers = 1,
    Suspending = 2,
    Resuming = 4,
}

public enum VkPipelineBindPoint : int
{
    Graphics = 0,
    Compute = 1,
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
    InputAttachment = 10,
}

public enum VkDescriptorPoolCreateFlags : uint
{
    None = 0,
    FreeDescriptorSet = 0x00000001,
}

public enum VkVertexInputRate : int
{
    Vertex = 0,
    Instance = 1,
}

public enum VkCommandPoolCreateFlags : uint
{
    None = 0,
    ResetCommandBuffer = 0x00000002,
    Transient = 0x00000001,
}

public enum VkDebugUtilsMessageSeverityFlagsEXT : uint
{
    Verbose = 0x00000001,
    Info = 0x00000010,
    Warning = 0x00000100,
    Error = 0x00001000,
}

public enum VkDebugUtilsMessageTypeFlagsEXT : uint
{
    General = 0x00000001,
    Validation = 0x00000002,
    Performance = 0x00000004,
}

public enum VkObjectType : int
{
    Unknown = 0,
    Instance = 1,
    PhysicalDevice = 2,
    Device = 3,
    Queue = 4,
    Semaphore = 5,
    CommandBuffer = 6,
    Fence = 7,
    DeviceMemory = 8,
    Buffer = 9,
    Image = 10,
    Event = 11,
    QueryPool = 12,
    BufferView = 13,
    ImageView = 14,
    ShaderModule = 15,
    PipelineCache = 16,
    PipelineLayout = 17,
    Pipeline = 19,
    CommandPool = 22,
    SurfaceKHR = 26,
    SwapchainKHR = 27,
    DebugUtilsMessengerEXT = 28,
}

public enum VkDependencyFlags : uint
{
    None = 0,
    ByRegion = 0x00000001,
    DeviceGroup = 0x00000004,
    ViewLocal = 0x00000002,
}
