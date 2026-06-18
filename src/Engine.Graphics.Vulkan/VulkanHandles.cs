using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

[StructLayout(LayoutKind.Sequential)]
public struct VkInstance { public nint Handle; public static readonly VkInstance Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkPhysicalDevice { public nint Handle; public static readonly VkPhysicalDevice Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkDevice { public nint Handle; public static readonly VkDevice Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkQueue { public nint Handle; public static readonly VkQueue Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkCommandPool { public nint Handle; public static readonly VkCommandPool Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkCommandBuffer { public nint Handle; public static readonly VkCommandBuffer Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkSwapchainKHR { public nint Handle; public static readonly VkSwapchainKHR Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkSurfaceKHR { public nint Handle; public static readonly VkSurfaceKHR Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkImage { public nint Handle; public static readonly VkImage Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkImageView { public nint Handle; public static readonly VkImageView Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkBuffer { public nint Handle; public static readonly VkBuffer Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkDeviceMemory { public nint Handle; public static readonly VkDeviceMemory Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkShaderModule { public nint Handle; public static readonly VkShaderModule Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkPipelineLayout { public nint Handle; public static readonly VkPipelineLayout Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkPipeline { public nint Handle; public static readonly VkPipeline Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkSemaphore { public nint Handle; public static readonly VkSemaphore Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkFence { public nint Handle; public static readonly VkFence Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkDebugUtilsMessengerEXT { public nint Handle; public static readonly VkDebugUtilsMessengerEXT Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkDescriptorSetLayout { public nint Handle; public static readonly VkDescriptorSetLayout Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkDescriptorPool { public nint Handle; public static readonly VkDescriptorPool Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkDescriptorSet { public nint Handle; public static readonly VkDescriptorSet Null = new() { Handle = 0 }; }
[StructLayout(LayoutKind.Sequential)]
public struct VkSampler { public nint Handle; public static readonly VkSampler Null = new() { Handle = 0 }; }
