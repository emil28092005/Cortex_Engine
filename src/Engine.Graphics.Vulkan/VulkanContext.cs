using System.Runtime.InteropServices;
using SDL;
using Engine.Core;

namespace Engine.Graphics.Vulkan;

internal static unsafe class SdlVulkan
{
    [DllImport("SDL3", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_Vulkan_CreateSurface(nint window, nint instance, nint allocator, VkSurfaceKHR* surface);

    public static void Create(IWindow window, VkInstance instance, VkSurfaceKHR* surface)
    {
        if (SDL_Vulkan_CreateSurface(window.Handle, instance.Handle, 0, surface) == 0)
            throw new InvalidOperationException("SDL_Vulkan_CreateSurface failed");
    }
}

internal sealed unsafe class VulkanContext : IDisposable
{
    public VkInstance Instance;
    public VkPhysicalDevice PhysicalDevice;
    public VkDevice Device;
    public VkQueue GraphicsQueue;
    public VkSurfaceKHR Surface;
    public uint GraphicsQueueFamilyIndex;
    public VkPhysicalDeviceMemoryProperties MemoryProperties;
    public VkFormat SurfaceFormat;
    public VkColorSpaceKHR SurfaceColorSpace;
    public VkExtent2D SurfaceExtent;
    public bool ValidationEnabled;

    private VkDebugUtilsMessengerEXT _debugMessenger;
    private bool _disposed;
    private static DebugCallbackDelegate? _debugCallbackDelegate;

    private static readonly uint VK_API_VERSION_1_3 = (1u << 22) | (3u << 12);

    private static bool IsLayerAvailable(string layerName)
    {
        var enumInstanceProps = VulkanNative.GetExport<EnumInstanceLayerPropertiesDelegate>("vkEnumerateInstanceLayerProperties");
        uint count = 0;
        enumInstanceProps(&count, null);
        if (count == 0) return false;

        var props = stackalloc VkLayerProperties[(int)count];
        enumInstanceProps(&count, props);

        var targetBytes = VulkanString.ToUtf8Terminated(layerName);
        for (uint i = 0; i < count; i++)
        {
            var namePtr = (byte*)props[(int)i].layerName;
            if (CompareUtf8(namePtr, targetBytes))
                return true;
        }
        return false;
    }

    private static bool CompareUtf8(byte* a, byte[] b)
    {
        for (int i = 0; i < b.Length; i++)
        {
            if (a == null || a[i] != b[i]) return false;
            if (b[i] == 0) return true;
        }
        return false;
    }

    [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
    private delegate VkResult EnumInstanceLayerPropertiesDelegate(uint* pPropertyCount, VkLayerProperties* pProperties);

    [StructLayout(LayoutKind.Sequential)]
    private struct VkLayerProperties
    {
        public fixed byte layerName[256];
        public uint specVersion;
        public uint implementationVersion;
        public fixed byte description[256];
    }

    public VulkanContext(IWindow window, bool enableValidation)
    {
        ValidationEnabled = enableValidation;
        _debugCallbackDelegate = DebugCallback;

        CreateInstance(window, enableValidation);
        CreateSurface(window);
        PickPhysicalDevice();
        CreateLogicalDevice(enableValidation);

        Console.WriteLine($"[Vulkan] Instance created, API version 1.3");
        Console.WriteLine($"[Vulkan] Validation layers: {(ValidationEnabled ? "enabled" : "disabled")}");
    }

    private void CreateInstance(IWindow window, bool enableValidation)
    {
        var sdlExtensions = window.GetRequiredVulkanExtensions();
        var extensionList = new List<string>(sdlExtensions);

        var useValidation = enableValidation && IsLayerAvailable("VK_LAYER_KHRONOS_validation");
        if (enableValidation && !useValidation)
            Console.WriteLine("[Vulkan] WARNING: VK_LAYER_KHRONOS_validation not found, running without validation");

        var layerNames = useValidation
            ? new[] { "VK_LAYER_KHRONOS_validation" }
            : Array.Empty<string>();

        if (useValidation)
            extensionList.Add("VK_EXT_debug_utils");
        ValidationEnabled = useValidation;

        var extPtrs = AllocStringArray(extensionList);
        var layerPtrs = AllocStringArray(layerNames);

        fixed (byte* appName = "Cortex Engine\0"u8)
        fixed (byte* engineName = "Cortex\0"u8)
        {
            var appInfo = new VkApplicationInfo
            {
                sType = VkStructureType.ApplicationInfo,
                pApplicationName = appName,
                applicationVersion = 1,
                pEngineName = engineName,
                engineVersion = 1,
                apiVersion = VK_API_VERSION_1_3,
            };

            var debugInfo = new VkDebugUtilsMessengerCreateInfoEXT
            {
                sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT,
                messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose |
                                  VkDebugUtilsMessageSeverityFlagsEXT.Warning |
                                  VkDebugUtilsMessageSeverityFlagsEXT.Error,
                messageType = VkDebugUtilsMessageTypeFlagsEXT.General |
                              VkDebugUtilsMessageTypeFlagsEXT.Validation |
                              VkDebugUtilsMessageTypeFlagsEXT.Performance,
                pfnUserCallback = Marshal.GetFunctionPointerForDelegate(_debugCallbackDelegate!),
            };

            var createInfo = new VkInstanceCreateInfo
            {
                sType = VkStructureType.InstanceCreateInfo,
                pApplicationInfo = &appInfo,
                enabledLayerCount = (uint)layerNames.Length,
                ppEnabledLayerNames = layerPtrs,
                enabledExtensionCount = (uint)extensionList.Count,
                ppEnabledExtensionNames = extPtrs,
            };

        if (useValidation && Vk.vkCreateDebugUtilsMessengerEXT != null)
                createInfo.pNext = (nint)(&debugInfo);

            VkResult result;
            fixed (VkInstance* instPtr = &Instance)
            {
                result = VulkanNative.vkGetInstanceProcAddr == null
                    ? VkResult.ErrorInitializationFailed
                    : default;

                var vkCreateInstance = VulkanNative.GetExport<Vk.VkCreateInstance>("vkCreateInstance");
                result = vkCreateInstance(&createInfo, 0, instPtr);
            }

            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateInstance failed: {result}");
        }

        Vk.LoadInstanceFunctions(Instance);

        if (useValidation)
        {
            fixed (VkDebugUtilsMessengerEXT* msgPtr = &_debugMessenger)
            {
                var dbgInfo = new VkDebugUtilsMessengerCreateInfoEXT
                {
                    sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT,
                    messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose |
                                      VkDebugUtilsMessageSeverityFlagsEXT.Warning |
                                      VkDebugUtilsMessageSeverityFlagsEXT.Error,
                    messageType = VkDebugUtilsMessageTypeFlagsEXT.General |
                                  VkDebugUtilsMessageTypeFlagsEXT.Validation |
                                  VkDebugUtilsMessageTypeFlagsEXT.Performance,
                    pfnUserCallback = Marshal.GetFunctionPointerForDelegate(_debugCallbackDelegate!),
                };
                Vk.vkCreateDebugUtilsMessengerEXT(Instance, &dbgInfo, 0, msgPtr);
            }
        }

        FreeStringArray(extPtrs, extensionList.Count);
        FreeStringArray(layerPtrs, layerNames.Length);
    }

    private static uint DebugCallback(uint messageSeverity, uint messageTypes,
        nint pCallbackData, nint pUserData)
    {
        var data = Marshal.PtrToStructure<VkDebugUtilsMessengerCallbackDataEXT>(pCallbackData);
        var msg = data.pMessage != null ? Marshal.PtrToStringUTF8((nint)data.pMessage) : "unknown";
        var severity = messageSeverity switch
        {
            0x00000001 => "VERBOSE",
            0x00000010 => "INFO",
            0x00000100 => "WARNING",
            0x00001000 => "ERROR",
            _ => "UNKNOWN"
        };
        Console.Error.WriteLine($"[Vulkan:{severity}] {msg}");
        return 0;
    }

    [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
    private delegate uint DebugCallbackDelegate(uint messageSeverity, uint messageTypes,
        nint pCallbackData, nint pUserData);

    private void CreateSurface(IWindow window)
    {
        fixed (VkSurfaceKHR* surfacePtr = &Surface)
        {
            SdlVulkan.Create(window, Instance, surfacePtr);
        }
    }

    private void PickPhysicalDevice()
    {
        uint count = 0;
        Vk.vkEnumeratePhysicalDevices(Instance, &count, null);
        if (count == 0)
            throw new InvalidOperationException("No Vulkan physical devices found");

        var devices = stackalloc VkPhysicalDevice[(int)count];
        Vk.vkEnumeratePhysicalDevices(Instance, &count, devices);

        VkPhysicalDevice best = VkPhysicalDevice.Null;
        VkPhysicalDeviceType bestType = VkPhysicalDeviceType.Other;

        for (uint i = 0; i < count; i++)
        {
            var propsBytes = stackalloc byte[824];
            Vk.vkGetPhysicalDeviceProperties(devices[(int)i], (VkPhysicalDeviceProperties*)propsBytes);
            var nameBytes = new byte[256];
            Marshal.Copy((nint)(propsBytes + 20), nameBytes, 0, 256);
            var nameLen = Array.IndexOf(nameBytes, (byte)0);
            if (nameLen < 0) nameLen = 256;
            var devType = (VkPhysicalDeviceType)Marshal.ReadInt32((nint)propsBytes, 16);
            var gpuName = System.Text.Encoding.UTF8.GetString(nameBytes, 0, nameLen);

            if (best.Handle == 0 || (devType == VkPhysicalDeviceType.DiscreteGpu && bestType != VkPhysicalDeviceType.DiscreteGpu))
            {
                best = devices[(int)i];
                bestType = devType;
                Console.WriteLine($"[Vulkan] Selected GPU: {gpuName} (type={devType})");
            }
        }

        if (best.Handle == 0)
            best = devices[0];

        PhysicalDevice = best;
        var memProps = new VkPhysicalDeviceMemoryProperties();
        Vk.vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, &memProps);
        MemoryProperties = memProps;

        uint queueCount = 0;
        Vk.vkGetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, &queueCount, null);
        var queueProps = stackalloc VkQueueFamilyProperties[(int)queueCount];
        Vk.vkGetPhysicalDeviceQueueFamilyProperties(PhysicalDevice, &queueCount, queueProps);

        GraphicsQueueFamilyIndex = uint.MaxValue;
        for (uint i = 0; i < queueCount; i++)
        {
            if ((queueProps[(int)i].queueFlags & VkQueueFlags.Graphics) != 0)
            {
                VkBool32 supported = VkBool32.False;
                Vk.vkGetPhysicalDeviceSurfaceSupportKHR(PhysicalDevice, i, Surface, &supported);
                if (supported == VkBool32.True)
                {
                    GraphicsQueueFamilyIndex = i;
                    break;
                }
            }
        }

        if (GraphicsQueueFamilyIndex == uint.MaxValue)
            throw new InvalidOperationException("No graphics queue family with surface support found");
    }

    private void CreateLogicalDevice(bool enableValidation)
    {
        var priorities = stackalloc float[1];
        priorities[0] = 1.0f;

        var queueInfo = new VkDeviceQueueCreateInfo
        {
            sType = VkStructureType.DeviceQueueCreateInfo,
            queueFamilyIndex = GraphicsQueueFamilyIndex,
            queueCount = 1,
            pQueuePriorities = priorities,
        };

        var extNames = new[] { "VK_KHR_swapchain" };
        var extPtrs = AllocStringArray(extNames);

        var sync2Features = new VkPhysicalDeviceSynchronization2Features
        {
            sType = VkStructureType.PhysicalDeviceSynchronization2Features,
            synchronization2 = VkBool32.True,
        };

        var renderingFeatures = new VkPhysicalDeviceDynamicRenderingFeatures
        {
            sType = VkStructureType.PhysicalDeviceDynamicRenderingFeatures,
            pNext = (nint)(&sync2Features),
            dynamicRendering = VkBool32.True,
        };


        var deviceInfo = new VkDeviceCreateInfo
        {
            sType = VkStructureType.DeviceCreateInfo,
            pQueueCreateInfos = &queueInfo,
            queueCreateInfoCount = 1,
            enabledExtensionCount = (uint)extNames.Length,
            ppEnabledExtensionNames = extPtrs,
            pEnabledFeatures = null,
            pNext = (nint)(&renderingFeatures),
        };

        var dev = VkDevice.Null;
        {
            var result = Vk.vkCreateDevice(PhysicalDevice, &deviceInfo, 0, &dev);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateDevice failed: {result}");
        }
        Device = dev;

        Vk.LoadDeviceFunctions(Device);

        fixed (VkQueue* queuePtr = &GraphicsQueue)
        {
            Vk.vkGetDeviceQueue(Device, GraphicsQueueFamilyIndex, 0, queuePtr);
        }

        FreeStringArray(extPtrs, extNames.Length);

        QuerySurfaceFormat();
    }

    private void QuerySurfaceFormat()
    {
        uint formatCount = 0;
        Vk.vkGetPhysicalDeviceSurfaceFormatsKHR(PhysicalDevice, Surface, &formatCount, null);
        if (formatCount == 0)
            throw new InvalidOperationException("No surface formats available");

        var formats = stackalloc VkSurfaceFormatKHR[(int)formatCount];
        Vk.vkGetPhysicalDeviceSurfaceFormatsKHR(PhysicalDevice, Surface, &formatCount, formats);

        SurfaceFormat = VkFormat.B8G8R8A8Srgb;
        SurfaceColorSpace = VkColorSpaceKHR.SrgbNonlinearKHR;

        for (uint i = 0; i < formatCount; i++)
        {
            if (formats[(int)i].format == VkFormat.B8G8R8A8Srgb &&
                formats[(int)i].colorSpace == VkColorSpaceKHR.SrgbNonlinearKHR)
            {
                SurfaceFormat = formats[(int)i].format;
                SurfaceColorSpace = formats[(int)i].colorSpace;
                break;
            }
        }

        if (SurfaceFormat == VkFormat.B8G8R8A8Srgb)
        {
            SurfaceFormat = formats[0].format;
            SurfaceColorSpace = formats[0].colorSpace;
        }

    }

    public uint FindMemoryType(uint memoryTypeBits, VkMemoryPropertyFlags desiredFlags)
    {
        for (uint i = 0; i < MemoryProperties.memoryTypeCount; i++)
        {
            if ((memoryTypeBits & (1u << (int)i)) != 0)
            {
                var flags = GetMemoryTypeFlags(i);
                if ((flags & desiredFlags) == desiredFlags)
                    return i;
            }
        }
        throw new InvalidOperationException($"No memory type found for flags {desiredFlags}");
    }

    private VkMemoryPropertyFlags GetMemoryTypeFlags(uint index)
    {
        if (index >= 32) return (VkMemoryPropertyFlags)0;
        fixed (VkPhysicalDeviceMemoryProperties* p = &MemoryProperties)
        {
            var memTypes = &p->memoryTypes0;
            return memTypes[index].propertyFlags;
        }
    }

    private static byte** AllocStringArray(IList<string> strings)
    {
        var ptr = (byte**)Marshal.AllocHGlobal(strings.Count * nint.Size);
        for (var i = 0; i < strings.Count; i++)
        {
            var bytes = VulkanString.ToUtf8Terminated(strings[i]);
            ptr[i] = (byte*)Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, (nint)ptr[i], bytes.Length);
        }
        return ptr;
    }

    private static void FreeStringArray(byte** ptr, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (ptr[i] != null)
                Marshal.FreeHGlobal((nint)ptr[i]);
        }
        Marshal.FreeHGlobal((nint)ptr);
    }

    private static string ParseDeviceName(VkPhysicalDeviceProperties* props)
    {
        var bytes = new byte[256];
        fixed (byte* dest = bytes)
        {
            Buffer.MemoryCopy(props->deviceName, dest, 256, 256);
        }
        var len = Array.IndexOf(bytes, (byte)0);
        if (len < 0) len = 256;
        return System.Text.Encoding.UTF8.GetString(bytes, 0, len);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Device.Handle != 0)
        {
            Vk.vkDeviceWaitIdle(Device);
            Vk.vkDestroyDevice(Device, 0);
        }

        if (_debugMessenger.Handle != 0 && Vk.vkDestroyDebugUtilsMessengerEXT != null)
            Vk.vkDestroyDebugUtilsMessengerEXT(Instance, _debugMessenger, 0);

        if (Surface.Handle != 0)
            Vk.vkDestroySurfaceKHR(Instance, Surface, 0);

        if (Instance.Handle != 0)
            Vk.vkDestroyInstance(Instance, 0);
    }
}
