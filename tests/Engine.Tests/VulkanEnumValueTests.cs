using Engine.Graphics.Vulkan;

namespace Engine.Tests;

/// <summary>
/// Verifies Vulkan enum values match the C Vulkan spec (vk.xml).
/// Wrong sType values caused the synchronization2 feature to not be enabled,
/// and wrong access flag values caused validation errors.
/// </summary>
public class VulkanEnumValueTests
{
    [Fact]
    public void VkResult_Success_Is_Zero()
    {
        Assert.Equal(0, (int)VkResult.Success);
    }

    [Fact]
    public void VkResult_ErrorOutOfDateKHR_Is_Negative()
    {
        Assert.True((int)VkResult.ErrorOutOfDateKHR < 0);
    }

    [Fact]
    public void VkResult_SuboptimalKHR_Is_Positive()
    {
        Assert.True((int)VkResult.SuboptimalKHR > 0);
    }

    [Theory]
    [InlineData(VkStructureType.ApplicationInfo, 0)]
    [InlineData(VkStructureType.InstanceCreateInfo, 1)]
    [InlineData(VkStructureType.DeviceQueueCreateInfo, 2)]
    [InlineData(VkStructureType.DeviceCreateInfo, 3)]
    [InlineData(VkStructureType.MemoryAllocateInfo, 5)]
    [InlineData(VkStructureType.BufferCreateInfo, 12)]
    [InlineData(VkStructureType.ImageCreateInfo, 14)]
    [InlineData(VkStructureType.ImageViewCreateInfo, 15)]
    [InlineData(VkStructureType.ShaderModuleCreateInfo, 16)]
    [InlineData(VkStructureType.DescriptorSetLayoutCreateInfo, 32)]
    [InlineData(VkStructureType.DescriptorPoolCreateInfo, 33)]
    [InlineData(VkStructureType.DescriptorSetAllocateInfo, 34)]
    [InlineData(VkStructureType.WriteDescriptorSet, 35)]
    [InlineData(VkStructureType.PipelineLayoutCreateInfo, 30)]
    [InlineData(VkStructureType.GraphicsPipelineCreateInfo, 28)]
    [InlineData(VkStructureType.CommandPoolCreateInfo, 39)]
    [InlineData(VkStructureType.CommandBufferAllocateInfo, 40)]
    [InlineData(VkStructureType.CommandBufferBeginInfo, 42)]
    [InlineData(VkStructureType.SemaphoreCreateInfo, 9)]
    [InlineData(VkStructureType.FenceCreateInfo, 8)]
    public void VkStructureType_Core_Values_Match_Spec(VkStructureType sType, int expected)
    {
        Assert.Equal(expected, (int)sType);
    }

    [Theory]
    [InlineData(VkStructureType.SwapchainCreateInfoKHR, 1000001000)]
    [InlineData(VkStructureType.PresentInfoKHR, 1000001001)]
    [InlineData(VkStructureType.DebugUtilsMessengerCreateInfoEXT, 1000128004)]
    public void VkStructureType_Extension_Values_Match_Spec(VkStructureType sType, int expected)
    {
        Assert.Equal(expected, (int)sType);
    }

    [Theory]
    [InlineData(VkStructureType.RenderingInfo, 1000044000)]
    [InlineData(VkStructureType.RenderingAttachmentInfo, 1000044001)]
    [InlineData(VkStructureType.PipelineRenderingCreateInfo, 1000044002)]
    [InlineData(VkStructureType.PhysicalDeviceDynamicRenderingFeatures, 1000044003)]
    public void VkStructureType_DynamicRendering_Values_Match_Spec(VkStructureType sType, int expected)
    {
        Assert.Equal(expected, (int)sType);
    }

    [Theory]
    [InlineData(VkStructureType.ImageMemoryBarrier2, 1000314002)]
    [InlineData(VkStructureType.BufferMemoryBarrier2, 1000314001)]
    [InlineData(VkStructureType.DependencyInfo, 1000314003)]
    [InlineData(VkStructureType.SubmitInfo2, 1000314004)]
    [InlineData(VkStructureType.SemaphoreSubmitInfo, 1000314005)]
    [InlineData(VkStructureType.CommandBufferSubmitInfo, 1000314006)]
    public void VkStructureType_Sync2_Values_Match_Spec(VkStructureType sType, int expected)
    {
        Assert.Equal(expected, (int)sType);
    }

    [Fact]
    public void VkStructureType_PhysicalDeviceSynchronization2Features_Matches_Spec()
    {
        Assert.Equal(1000314007, (int)VkStructureType.PhysicalDeviceSynchronization2Features);
    }

    [Theory]
    [InlineData(VkFormat.Undefined, 0)]
    [InlineData(VkFormat.R8G8B8A8Unorm, 37)]
    [InlineData(VkFormat.B8G8R8A8Unorm, 44)]
    [InlineData(VkFormat.R8G8B8A8Srgb, 43)]
    [InlineData(VkFormat.B8G8R8A8Srgb, 50)]
    [InlineData(VkFormat.R32G32Sfloat, 103)]
    [InlineData(VkFormat.R32G32B32Sfloat, 106)]
    [InlineData(VkFormat.R32G32B32A32Sfloat, 109)]
    [InlineData(VkFormat.D32Sfloat, 126)]
    public void VkFormat_Values_Match_Spec(VkFormat format, int expected)
    {
        Assert.Equal(expected, (int)format);
    }

    [Theory]
    [InlineData(VkPresentModeKHR.Immediate, 0)]
    [InlineData(VkPresentModeKHR.Mailbox, 1)]
    [InlineData(VkPresentModeKHR.Fifo, 2)]
    [InlineData(VkPresentModeKHR.FifoRelaxed, 3)]
    public void VkPresentModeKHR_Values_Match_Spec(VkPresentModeKHR mode, int expected)
    {
        Assert.Equal(expected, (int)mode);
    }

    [Theory]
    [InlineData(VkImageLayout.Undefined, 0)]
    [InlineData(VkImageLayout.General, 1)]
    [InlineData(VkImageLayout.ColorAttachmentOptimal, 2)]
    [InlineData(VkImageLayout.DepthStencilAttachmentOptimal, 3)]
    [InlineData(VkImageLayout.ShaderReadOnlyOptimal, 5)]
    [InlineData(VkImageLayout.TransferSrcOptimal, 6)]
    [InlineData(VkImageLayout.TransferDstOptimal, 7)]
    [InlineData(VkImageLayout.PresentSrcKHR, 1000001002)]
    public void VkImageLayout_Values_Match_Spec(VkImageLayout layout, int expected)
    {
        Assert.Equal(expected, (int)layout);
    }

    [Theory]
    [InlineData(VkPrimitiveTopology.TriangleList, 3)]
    [InlineData(VkPrimitiveTopology.TriangleStrip, 4)]
    [InlineData(VkPrimitiveTopology.TriangleFan, 5)]
    [InlineData(VkPrimitiveTopology.PointList, 0)]
    [InlineData(VkPrimitiveTopology.LineList, 1)]
    public void VkPrimitiveTopology_Values_Match_Spec(VkPrimitiveTopology topology, int expected)
    {
        Assert.Equal(expected, (int)topology);
    }

    [Theory]
    [InlineData(VkCompareOp.Never, 0)]
    [InlineData(VkCompareOp.Less, 1)]
    [InlineData(VkCompareOp.Equal, 2)]
    [InlineData(VkCompareOp.LessOrEqual, 3)]
    [InlineData(VkCompareOp.Greater, 4)]
    [InlineData(VkCompareOp.Always, 7)]
    public void VkCompareOp_Values_Match_Spec(VkCompareOp op, int expected)
    {
        Assert.Equal(expected, (int)op);
    }

    [Theory]
    [InlineData(VkDescriptorType.UniformBuffer, 6)]
    [InlineData(VkDescriptorType.StorageBuffer, 7)]
    [InlineData(VkDescriptorType.CombinedImageSampler, 1)]
    public void VkDescriptorType_Values_Match_Spec(VkDescriptorType type, int expected)
    {
        Assert.Equal(expected, (int)type);
    }
}

/// <summary>
/// Verifies sync2 pipeline stage and access flag values match the C Vulkan spec.
/// Wrong values here caused validation errors (TRANSFER_READ used instead of COLOR_ATTACHMENT_WRITE).
/// </summary>
public class VulkanSync2FlagTests
{
    [Theory]
    [InlineData((ulong)VkPipelineStageFlags2.None, 0UL)]
    [InlineData((ulong)VkPipelineStageFlags2.ColorAttachmentOutput, 0x400UL)]
    [InlineData((ulong)VkPipelineStageFlags2.AllGraphics, 0x8000UL)]
    [InlineData((ulong)VkPipelineStageFlags2.Transfer, 0x1000UL)]
    public void VkPipelineStageFlags2_Values_Match_Spec(ulong value, ulong expected)
    {
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData((ulong)VkAccessFlags2.None, 0UL)]
    [InlineData((ulong)VkAccessFlags2.ColorAttachmentWrite, 0x100UL)]
    [InlineData((ulong)VkAccessFlags2.ColorAttachmentRead, 0x80UL)]
    [InlineData((ulong)VkAccessFlags2.TransferRead, 0x800UL)]
    [InlineData((ulong)VkAccessFlags2.TransferWrite, 0x1000UL)]
    public void VkAccessFlags2_Values_Match_Spec(ulong value, ulong expected)
    {
        Assert.Equal(expected, value);
    }
}
