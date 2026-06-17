using System.Runtime.InteropServices;
using System.Text;
using Engine.Core;
using SDL;

namespace Engine.Graphics.Vulkan;

public sealed unsafe class VulkanContext : IDisposable
{
    public VkInstance Instance;
    public VkPhysicalDevice PhysicalDevice;
    public VkDevice Device;
    public VkSurfaceKHR Surface;
    public VkQueue GraphicsQueue;
    public VkQueue PresentQueue;
    public uint GraphicsFamily;
    public uint PresentFamily;
    public VkPhysicalDeviceMemoryProperties MemoryProperties;
    private readonly bool _validation;
    private bool _disposed;

    public VulkanContext(Sdl3Window window, bool enableValidation)
    {
        _validation = enableValidation;
        Vk.LoadGlobalFunctions();
        CreateInstance(window.GetRequiredVulkanExtensions());
        CreateSurface(window);
        PickPhysicalDevice();
        CreateLogicalDevice();
    }

    private unsafe void CreateInstance(string[] requiredExtensions)
    {
        var layers = Array.Empty<string>();
        if (_validation)
        {
            uint layerCount = 0;
            VulkanNative.vkEnumerateInstanceLayerProperties(&layerCount, null);
            if (layerCount > 0)
            {
                var availableLayers = new VkLayerProperties[layerCount];
                fixed (VkLayerProperties* pLayers = availableLayers)
                {
                    VulkanNative.vkEnumerateInstanceLayerProperties(&layerCount, pLayers);
                }

                for (var i = 0; i < layerCount; i++)
                {
                    fixed (VkLayerProperties* pLayer = &availableLayers[i])
                    {
                        var nameLen = 0;
                        while (nameLen < 256 && pLayer->layerName[nameLen] != 0) nameLen++;
                        var layerName = Encoding.UTF8.GetString(pLayer->layerName, nameLen);

                        if (layerName == "VK_LAYER_KHRONOS_validation")
                        {
                            layers = new[] { "VK_LAYER_KHRONOS_validation" };
                            Console.WriteLine("[Vulkan] Validation layers enabled.");
                            break;
                        }
                    }
                }
            }

            if (layers.Length == 0)
                Console.WriteLine("[Vulkan] Validation layers requested but not available.");
        }

        var extensions = requiredExtensions;

        var appNameBytes = Encoding.UTF8.GetBytes("Cortex Engine\0");
        var engineNameBytes = Encoding.UTF8.GetBytes("CortexEngine\0");

        VkApplicationInfo appInfo;
        appInfo.sType = VkStructureType.ApplicationInfo;
        appInfo.pNext = null;
        fixed (byte* pAppName = appNameBytes, pEngineName = engineNameBytes)
        {
            appInfo.pApplicationName = pAppName;
            appInfo.applicationVersion = 0;
            appInfo.pEngineName = pEngineName;
            appInfo.engineVersion = 0;
            appInfo.apiVersion = (1 << 22) | (3 << 12);

            var extPtrs = Vk.AllocStringArray(extensions);
            var layerPtrs = Vk.AllocStringArray(layers);

            VkInstanceCreateInfo createInfo;
            createInfo.sType = VkStructureType.InstanceCreateInfo;
            createInfo.pNext = null;
            createInfo.flags = 0;
            createInfo.pApplicationInfo = &appInfo;
            createInfo.enabledLayerCount = (uint)layers.Length;
            createInfo.ppEnabledLayerNames = layerPtrs;
            createInfo.enabledExtensionCount = (uint)extensions.Length;
            createInfo.ppEnabledExtensionNames = extPtrs;

            VkResult result;
            fixed (VkInstance* pInstance = &Instance)
            {
                result = Vk.vkCreateInstance(&createInfo, null, pInstance);
            }

            Vk.FreeStringArray(extPtrs, extensions.Length);
            Vk.FreeStringArray(layerPtrs, layers.Length);

            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateInstance failed: {result}. " +
                    $"Extensions: [{string.Join(", ", extensions)}], Layers: [{string.Join(", ", layers)}]");
        }

        Vk.LoadInstanceFunctions(Instance);
        Console.WriteLine("[Vulkan] Instance created.");
    }

    private unsafe void CreateSurface(Sdl3Window window)
    {
        var sdlWindow = (SDL_Window*)window.Handle;

        var instancePtr = (SDL.VkInstance_T*)Instance.Value;
        SDL.VkSurfaceKHR_T* surfacePtr;
        if (!SDL3.SDL_Vulkan_CreateSurface(sdlWindow, instancePtr, null, &surfacePtr))
            throw new InvalidOperationException($"SDL_Vulkan_CreateSurface failed: {SDL3.SDL_GetError()}");

        Surface = new VkSurfaceKHR { Value = (ulong)surfacePtr };
        Console.WriteLine("[Vulkan] Surface created.");
    }

    private unsafe void PickPhysicalDevice()
    {
        uint deviceCount = 0;
        Vk.vkEnumeratePhysicalDevices(Instance, &deviceCount, null);
        if (deviceCount == 0)
            throw new InvalidOperationException("No GPU with Vulkan support found.");

        var devices = new VkPhysicalDevice[deviceCount];
        fixed (VkPhysicalDevice* pDevices = devices)
        {
            Vk.vkEnumeratePhysicalDevices(Instance, &deviceCount, pDevices);
        }

        VkPhysicalDevice bestDevice = default;
        uint bestGraphicsFamily = uint.MaxValue;
        uint bestPresentFamily = uint.MaxValue;
        int bestScore = -1;

        for (uint i = 0; i < deviceCount; i++)
        {
            VkPhysicalDeviceProperties props;
            Vk.vkGetPhysicalDeviceProperties(devices[i], &props);

            byte* pName = props.deviceName;
            var nameLen = 0;
            while (nameLen < 256 && pName[nameLen] != 0) nameLen++;
            var deviceName = Encoding.UTF8.GetString(pName, nameLen);

            var score = (int)props.deviceType;
            if (props.deviceType == VkPhysicalDeviceType.DiscreteGpu) score = 1000;
            else if (props.deviceType == VkPhysicalDeviceType.IntegratedGpu) score = 500;

            if (!FindQueueFamilies(devices[i], out var graphicsFamily, out var presentFamily))
                continue;

            if (score > bestScore)
            {
                bestScore = score;
                bestDevice = devices[i];
                bestGraphicsFamily = graphicsFamily;
                bestPresentFamily = presentFamily;
                Console.WriteLine($"[Vulkan] Selected GPU: {deviceName} (score {score})");
            }
        }

        if (bestScore < 0)
            throw new InvalidOperationException("No suitable GPU found with graphics + present queues.");

        PhysicalDevice = bestDevice;
        GraphicsFamily = bestGraphicsFamily;
        PresentFamily = bestPresentFamily;

        VkPhysicalDeviceMemoryProperties memProps;
        Vk.vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, &memProps);
        MemoryProperties = memProps;
    }

    private unsafe bool FindQueueFamilies(VkPhysicalDevice device, out uint graphicsFamily, out uint presentFamily)
    {
        graphicsFamily = uint.MaxValue;
        presentFamily = uint.MaxValue;

        uint count = 0;
        Vk.vkGetPhysicalDeviceQueueFamilyProperties(device, &count, null);
        if (count == 0) return false;

        var props = new VkQueueFamilyProperties[count];
        fixed (VkQueueFamilyProperties* pProps = props)
        {
            Vk.vkGetPhysicalDeviceQueueFamilyProperties(device, &count, pProps);
        }

        for (uint i = 0; i < count; i++)
        {
            if ((props[i].queueFlags & VkQueueFlags.Graphics) != 0)
                graphicsFamily = i;

            uint supported = 0;
            Vk.vkGetPhysicalDeviceSurfaceSupportKHR(device, i, Surface, &supported);
            if (supported != 0)
                presentFamily = i;

            if (graphicsFamily != uint.MaxValue && presentFamily != uint.MaxValue)
                return true;
        }

        return false;
    }

    private unsafe void CreateLogicalDevice()
    {
        var queueIndices = new HashSet<uint> { GraphicsFamily, PresentFamily };
        var queueCreateInfos = new VkDeviceQueueCreateInfo[queueIndices.Count];
        var priorities = new float[] { 1.0f };

        fixed (float* pPrio = priorities)
        {
            var idx = 0;
            foreach (var qfi in queueIndices)
            {
                queueCreateInfos[idx] = new VkDeviceQueueCreateInfo
                {
                    sType = VkStructureType.DeviceQueueCreateInfo,
                    pNext = null,
                    flags = 0,
                    queueFamilyIndex = qfi,
                    queueCount = 1,
                    pQueuePriorities = pPrio
                };
                idx++;
            }

            var extNameBytes = System.Text.Encoding.UTF8.GetBytes("VK_KHR_swapchain\0");
            var extNamePtr = (byte*)Marshal.AllocHGlobal(extNameBytes.Length);
            Marshal.Copy(extNameBytes, 0, (nint)extNamePtr, extNameBytes.Length);

            fixed (VkDeviceQueueCreateInfo* pQueueCreateInfos = queueCreateInfos)
            {
                byte* features = stackalloc byte[228];
                VkDeviceCreateInfo createInfo;
                createInfo.sType = VkStructureType.DeviceCreateInfo;
                createInfo.pNext = null;
                createInfo.flags = 0;
                createInfo.queueCreateInfoCount = (uint)queueCreateInfos.Length;
                createInfo.pQueueCreateInfos = pQueueCreateInfos;
                createInfo.enabledLayerCount = 0;
                createInfo.ppEnabledLayerNames = null;
                createInfo.enabledExtensionCount = 1;
                createInfo.ppEnabledExtensionNames = &extNamePtr;
                createInfo.pEnabledFeatures = features;

                VkDevice device;
                var result = Vk.vkCreateDevice(PhysicalDevice, &createInfo, null, &device);
                Vk.CheckResult(result, "vkCreateDevice");
                Device = device;
            }

            Marshal.FreeHGlobal((nint)extNamePtr);
        }

        Vk.LoadDeviceFunctions(Device);

        fixed (VkQueue* pGfxQueue = &GraphicsQueue)
        {
            Vk.vkGetDeviceQueue(Device, GraphicsFamily, 0, pGfxQueue);
        }
        fixed (VkQueue* pPresentQueue = &PresentQueue)
        {
            Vk.vkGetDeviceQueue(Device, PresentFamily, 0, pPresentQueue);
        }

        Console.WriteLine("[Vulkan] Logical device created.");
    }

    public unsafe uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        for (uint i = 0; i < MemoryProperties.memoryTypeCount; i++)
        {
            var memType = GetMemoryType(i);
            if ((typeFilter & (1u << (int)i)) != 0 && (memType.propertyFlags & properties) == properties)
                return i;
        }

        throw new InvalidOperationException($"Failed to find memory type with filter={typeFilter:X} props={properties}");
    }

    private VkMemoryType GetMemoryType(uint index)
    {
        return index switch
        {
            0 => MemoryProperties.memoryTypes0,
            1 => MemoryProperties.memoryTypes1,
            2 => MemoryProperties.memoryTypes2,
            3 => MemoryProperties.memoryTypes3,
            4 => MemoryProperties.memoryTypes4,
            5 => MemoryProperties.memoryTypes5,
            6 => MemoryProperties.memoryTypes6,
            7 => MemoryProperties.memoryTypes7,
            8 => MemoryProperties.memoryTypes8,
            9 => MemoryProperties.memoryTypes9,
            10 => MemoryProperties.memoryTypes10,
            11 => MemoryProperties.memoryTypes11,
            12 => MemoryProperties.memoryTypes12,
            13 => MemoryProperties.memoryTypes13,
            14 => MemoryProperties.memoryTypes14,
            15 => MemoryProperties.memoryTypes15,
            _ => throw new IndexOutOfRangeException()
        };
    }

    public unsafe void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Device.Value != 0)
        {
            Vk.vkQueueWaitIdle(GraphicsQueue);
            Vk.vkDestroyDevice(Device, null);
        }
        if (Surface.Value != 0)
            Vk.vkDestroySurfaceKHR(Instance, Surface, null);
        if (Instance.Value != 0)
            Vk.vkDestroyInstance(Instance, null);
    }
}
