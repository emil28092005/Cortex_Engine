using System.Runtime.InteropServices;
using Engine.Graphics.Vulkan;

namespace Engine.Tests;

/// <summary>
/// Verifies C# Vulkan struct sizes match the C Vulkan header sizes.
/// Size mismatches were the root cause of multiple crashes during development.
/// </summary>
public unsafe class VulkanStructSizeTests
{
    [Fact]
    public void VkExtent2D_Is_8_Bytes()
    {
        Assert.Equal(8, sizeof(VkExtent2D));
    }

    [Fact]
    public void VkExtent3D_Is_12_Bytes()
    {
        Assert.Equal(12, sizeof(VkExtent3D));
    }

    [Fact]
    public void VkOffset2D_Is_8_Bytes()
    {
        Assert.Equal(8, sizeof(VkOffset2D));
    }

    [Fact]
    public void VkOffset3D_Is_12_Bytes()
    {
        Assert.Equal(12, sizeof(VkOffset3D));
    }

    [Fact]
    public void VkRect2D_Is_16_Bytes()
    {
        Assert.Equal(16, sizeof(VkRect2D));
    }

    [Fact]
    public void VkViewport_Is_24_Bytes()
    {
        Assert.Equal(24, sizeof(VkViewport));
    }

    [Fact]
    public void VkClearValue_Is_16_Bytes()
    {
        Assert.Equal(16, sizeof(VkClearValue));
    }

    [Fact]
    public void VkApplicationInfo_Is_48_Bytes()
    {
        Assert.Equal(48, sizeof(VkApplicationInfo));
    }

    [Fact]
    public void VkInstanceCreateInfo_Is_64_Bytes()
    {
        Assert.Equal(64, sizeof(VkInstanceCreateInfo));
    }

    [Fact]
    public void VkDeviceQueueCreateInfo_Is_40_Bytes()
    {
        Assert.Equal(40, sizeof(VkDeviceQueueCreateInfo));
    }

    [Fact]
    public void VkDeviceCreateInfo_Is_72_Bytes()
    {
        Assert.Equal(72, sizeof(VkDeviceCreateInfo));
    }

    [Fact]
    public void VkSwapchainCreateInfoKHR_Is_104_Bytes()
    {
        Assert.Equal(104, sizeof(VkSwapchainCreateInfoKHR));
    }

    [Fact]
    public void VkImageViewCreateInfo_Is_80_Bytes()
    {
        Assert.Equal(80, sizeof(VkImageViewCreateInfo));
    }

    [Fact]
    public void VkShaderModuleCreateInfo_Is_40_Bytes()
    {
        Assert.Equal(40, sizeof(VkShaderModuleCreateInfo));
    }

    [Fact]
    public void VkPipelineShaderStageCreateInfo_Is_48_Bytes()
    {
        Assert.Equal(48, sizeof(VkPipelineShaderStageCreateInfo));
    }

    [Fact]
    public void VkVertexInputBindingDescription_Is_12_Bytes()
    {
        Assert.Equal(12, sizeof(VkVertexInputBindingDescription));
    }

    [Fact]
    public void VkVertexInputAttributeDescription_Is_16_Bytes()
    {
        Assert.Equal(16, sizeof(VkVertexInputAttributeDescription));
    }

    [Fact]
    public void VkGraphicsPipelineCreateInfo_Is_144_Bytes()
    {
        Assert.Equal(144, sizeof(VkGraphicsPipelineCreateInfo));
    }

    [Fact]
    public void VkCommandBufferBeginInfo_Is_32_Bytes()
    {
        Assert.Equal(32, sizeof(VkCommandBufferBeginInfo));
    }

    [Fact]
    public void VkBufferCreateInfo_Is_56_Bytes()
    {
        Assert.Equal(56, sizeof(VkBufferCreateInfo));
    }

    [Fact]
    public void VkMemoryAllocateInfo_Is_32_Bytes()
    {
        Assert.Equal(32, sizeof(VkMemoryAllocateInfo));
    }

    [Fact]
    public void VkMemoryRequirements_Is_24_Bytes()
    {
        Assert.Equal(24, sizeof(VkMemoryRequirements));
    }

    [Fact]
    public void VkBufferCopy_Is_24_Bytes()
    {
        Assert.Equal(24, sizeof(VkBufferCopy));
    }

    [Fact]
    public void VkPresentInfoKHR_Is_64_Bytes()
    {
        Assert.Equal(64, sizeof(VkPresentInfoKHR));
    }

    [Fact]
    public void VkSurfaceFormatKHR_Is_8_Bytes()
    {
        Assert.Equal(8, sizeof(VkSurfaceFormatKHR));
    }

    [Fact]
    public void VkSurfaceCapabilitiesKHR_Is_52_Bytes()
    {
        Assert.Equal(52, sizeof(VkSurfaceCapabilitiesKHR));
    }

    [Fact]
    public void VkQueueFamilyProperties_Is_24_Bytes()
    {
        Assert.Equal(24, sizeof(VkQueueFamilyProperties));
    }

    [Fact]
    public void VkMemoryType_Is_8_Bytes()
    {
        Assert.Equal(8, sizeof(VkMemoryType));
    }

    [Fact]
    public void VkMemoryHeap_Is_16_Bytes()
    {
        Assert.Equal(16, sizeof(VkMemoryHeap));
    }

    [Fact]
    public void VkPhysicalDeviceMemoryProperties_Is_520_Bytes()
    {
        Assert.Equal(520, sizeof(VkPhysicalDeviceMemoryProperties));
    }

    [Fact]
    public void VkPushConstantRange_Is_12_Bytes()
    {
        Assert.Equal(12, sizeof(VkPushConstantRange));
    }

    [Fact]
    public void VkDescriptorSetLayoutBinding_Is_24_Bytes()
    {
        Assert.Equal(24, sizeof(VkDescriptorSetLayoutBinding));
    }

    [Fact]
    public void VkDescriptorSetLayoutCreateInfo_Is_32_Bytes()
    {
        Assert.Equal(32, sizeof(VkDescriptorSetLayoutCreateInfo));
    }

    [Fact]
    public void VkDescriptorPoolSize_Is_8_Bytes()
    {
        Assert.Equal(8, sizeof(VkDescriptorPoolSize));
    }

    [Fact]
    public void VkDescriptorPoolCreateInfo_Is_40_Bytes()
    {
        Assert.Equal(40, sizeof(VkDescriptorPoolCreateInfo));
    }

    [Fact]
    public void VkDescriptorSetAllocateInfo_Is_40_Bytes()
    {
        Assert.Equal(40, sizeof(VkDescriptorSetAllocateInfo));
    }

    [Fact]
    public void VkDescriptorBufferInfo_Is_24_Bytes()
    {
        Assert.Equal(24, sizeof(VkDescriptorBufferInfo));
    }

    [Fact]
    public void VkWriteDescriptorSet_Is_64_Bytes()
    {
        Assert.Equal(64, sizeof(VkWriteDescriptorSet));
    }

    [Fact]
    public void VkImageCreateInfo_Is_88_Bytes()
    {
        Assert.Equal(88, sizeof(VkImageCreateInfo));
    }

    [Fact]
    public void VkPipelineDepthStencilStateCreateInfo_Is_104_Bytes()
    {
        Assert.Equal(104, sizeof(VkPipelineDepthStencilStateCreateInfo));
    }

    [Fact]
    public void VkStencilOpState_Is_28_Bytes()
    {
        Assert.Equal(28, sizeof(VkStencilOpState));
    }

    [Fact]
    public void VkImageMemoryBarrier2_Is_96_Bytes()
    {
        Assert.Equal(96, sizeof(VkImageMemoryBarrier2));
    }

    [Fact]
    public void VkDependencyInfo_Is_64_Bytes()
    {
        Assert.Equal(64, sizeof(VkDependencyInfo));
    }

    [Fact]
    public void VkSubmitInfo2_Is_64_Bytes()
    {
        Assert.Equal(64, sizeof(VkSubmitInfo2));
    }

    [Fact]
    public void VkSemaphoreSubmitInfo_Is_48_Bytes()
    {
        Assert.Equal(48, sizeof(VkSemaphoreSubmitInfo));
    }

    [Fact]
    public void VkCommandBufferSubmitInfo_Is_32_Bytes()
    {
        Assert.Equal(32, sizeof(VkCommandBufferSubmitInfo));
    }

    [Fact]
    public void VkRenderingInfo_Is_72_Bytes()
    {
        Assert.Equal(72, sizeof(VkRenderingInfo));
    }

    [Fact]
    public void VkRenderingAttachmentInfo_Is_72_Bytes()
    {
        Assert.Equal(72, sizeof(VkRenderingAttachmentInfo));
    }

    [Fact]
    public void VkPipelineRenderingCreateInfo_Is_40_Bytes()
    {
        Assert.Equal(40, sizeof(VkPipelineRenderingCreateInfo));
    }
}
