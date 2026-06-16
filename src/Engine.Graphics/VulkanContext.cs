using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Engine.Core;
using SDL;
using Vortice.Vulkan;

namespace Engine.Graphics;

/// <summary>
/// Owns the Vulkan instance, physical device, logical device, queues, and API handles.
/// Created once per application lifetime.
/// </summary>
public sealed unsafe class VulkanContext : IDisposable
{
    private bool _disposed;

    public VkInstance Instance { get; private set; }
    public VkInstanceApi InstanceApi { get; private set; }
    public VkPhysicalDevice PhysicalDevice { get; private set; }
    public VkDevice Device { get; private set; }
    public VkDeviceApi DeviceApi { get; private set; }
    public VkQueue GraphicsQueue { get; private set; }
    public VkQueue PresentQueue { get; private set; }
    public uint GraphicsFamilyIndex { get; private set; }
    public uint PresentFamilyIndex { get; private set; }
    public VkSurfaceKHR Surface { get; private set; }

    public VulkanContext(Sdl3Window window, bool enableValidation = true)
    {
        CreateInstance(window, enableValidation);
        InstanceApi = Vulkan.GetApi(Instance);
        CreateSurface(window);
        PickPhysicalDevice();
        CreateLogicalDevice();
        DeviceApi = Vulkan.GetApi(Instance, Device);
        GetQueues();
    }

    private void CreateInstance(Sdl3Window window, bool enableValidation)
    {
        var requiredExtensions = new List<string>(window.GetRequiredInstanceExtensions());
        if (enableValidation)
        {
            requiredExtensions.Add("VK_EXT_debug_utils");
        }

        var layerNames = enableValidation
            ? new[] { "VK_LAYER_KHRONOS_validation" }
            : Array.Empty<string>();

        var appName = VkStringInterop.ConvertToUnmanaged("Cortex Engine");
        var engineName = VkStringInterop.ConvertToUnmanaged("CortexEngine");

        var appInfo = new VkApplicationInfo
        {
            sType = VkStructureType.ApplicationInfo,
            pApplicationName = appName,
            pEngineName = engineName,
            apiVersion = VkVersion.Version_1_3
        };

        using var extensionPin = new StringArrayPin(requiredExtensions);
        using var layerPin = new StringArrayPin(layerNames);

        {
            var createInfo = new VkInstanceCreateInfo
            {
                sType = VkStructureType.InstanceCreateInfo,
                pApplicationInfo = &appInfo,
                enabledExtensionCount = (uint)requiredExtensions.Count,
                ppEnabledExtensionNames = extensionPin.Pointers,
                enabledLayerCount = (uint)layerNames.Length,
                ppEnabledLayerNames = layerPin.Pointers
            };

            var result = Vulkan.vkCreateInstance(&createInfo, null, out var instance);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkCreateInstance failed: {result}");
            Instance = instance;
        }

        VkStringInterop.Free(appName);
        VkStringInterop.Free(engineName);
    }

    private void CreateSurface(Sdl3Window window)
    {
        var sdlInstance = (SDL.VkInstance_T*)Instance.Handle;
        var sdlSurface = (SDL.VkSurfaceKHR_T*)null;
        var result = SDL3.SDL_Vulkan_CreateSurface(
            (SDL_Window*)window.Handle,
            sdlInstance,
            null,
            &sdlSurface);

        if (result != true)
            throw new InvalidOperationException($"SDL_Vulkan_CreateSurface failed: {SDL3.SDL_GetError()}");

        Surface = new VkSurfaceKHR((ulong)sdlSurface);
    }

    private void PickPhysicalDevice()
    {
        var devices = EnumeratePhysicalDevices();
        if (devices.Length == 0)
            throw new InvalidOperationException("No Vulkan physical devices found.");

        foreach (var device in devices)
        {
            var properties = InstanceApi.vkGetPhysicalDeviceProperties(device);
            var queueFamilies = GetPhysicalDeviceQueueFamilyProperties(device);

            var hasGraphics = false;
            var hasPresent = false;
            for (var i = 0; i < queueFamilies.Length; i++)
            {
                if (queueFamilies[i].queueFlags.HasFlag(VkQueueFlags.Graphics))
                    hasGraphics = true;
                var supportResult = InstanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(device, (uint)i, Surface, out VkBool32 supported);
                if (supportResult == VkResult.Success && supported)
                    hasPresent = true;
            }

            if (hasGraphics && hasPresent)
            {
                PhysicalDevice = device;
                if (properties.deviceType == VkPhysicalDeviceType.DiscreteGpu)
                    break;
            }
        }

        if (PhysicalDevice == VkPhysicalDevice.Null)
            throw new InvalidOperationException("No suitable Vulkan physical device found.");
    }

    private VkPhysicalDevice[] EnumeratePhysicalDevices()
    {
        uint count = 0;
        InstanceApi.vkEnumeratePhysicalDevices(&count, null);
        if (count == 0)
            return Array.Empty<VkPhysicalDevice>();

        var devices = new VkPhysicalDevice[count];
        fixed (VkPhysicalDevice* p = devices)
        {
            var result = InstanceApi.vkEnumeratePhysicalDevices(&count, p);
            if (result != VkResult.Success)
                throw new InvalidOperationException($"vkEnumeratePhysicalDevices failed: {result}");
        }
        return devices;
    }

    private void CreateLogicalDevice()
    {
        var queueFamilies = GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice);
        GraphicsFamilyIndex = FindQueueFamilyIndex(queueFamilies, VkQueueFlags.Graphics);
        PresentFamilyIndex = FindPresentQueueFamilyIndex(queueFamilies);

        var uniqueFamilies = new HashSet<uint> { GraphicsFamilyIndex, PresentFamilyIndex };
        var queueCreateInfos = uniqueFamilies.Select(family => new VkDeviceQueueCreateInfo
        {
            sType = VkStructureType.DeviceQueueCreateInfo,
            queueFamilyIndex = family,
            queueCount = 1
        }).ToArray();

        var priorityHandles = new GCHandle[queueCreateInfos.Length];
        var extensionNames = new[] { "VK_KHR_swapchain" };
        using var extensionPin = new StringArrayPin(extensionNames);

        try
        {
            var deviceFeatures = new VkPhysicalDeviceFeatures();

            for (var i = 0; i < queueCreateInfos.Length; i++)
            {
                var priority = new[] { 1.0f };
                var handle = GCHandle.Alloc(priority, GCHandleType.Pinned);
                priorityHandles[i] = handle;
                queueCreateInfos[i].pQueuePriorities = (float*)handle.AddrOfPinnedObject();
            }

            fixed (VkDeviceQueueCreateInfo* pQueue = queueCreateInfos)
            {
                var createInfo = new VkDeviceCreateInfo
                {
                    sType = VkStructureType.DeviceCreateInfo,
                    queueCreateInfoCount = (uint)queueCreateInfos.Length,
                    pQueueCreateInfos = pQueue,
                    pEnabledFeatures = &deviceFeatures,
                    enabledExtensionCount = 1,
                    ppEnabledExtensionNames = extensionPin.Pointers
                };

                var result = InstanceApi.vkCreateDevice(PhysicalDevice, &createInfo, null, out var device);
                if (result != VkResult.Success)
                    throw new InvalidOperationException($"vkCreateDevice failed: {result}");
                Device = device;
            }
        }
        finally
        {
            foreach (var handle in priorityHandles)
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }
    }

    private void GetQueues()
    {
        DeviceApi.vkGetDeviceQueue(GraphicsFamilyIndex, 0, out var graphicsQueue);
        DeviceApi.vkGetDeviceQueue(PresentFamilyIndex, 0, out var presentQueue);
        GraphicsQueue = graphicsQueue;
        PresentQueue = presentQueue;
    }

    private uint FindQueueFamilyIndex(VkQueueFamilyProperties[] properties, VkQueueFlags flags)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            if (properties[i].queueFlags.HasFlag(flags))
                return (uint)i;
        }
        throw new InvalidOperationException($"No queue family with flags {flags} found.");
    }

    private uint FindPresentQueueFamilyIndex(VkQueueFamilyProperties[] properties)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            var supportResult = InstanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(PhysicalDevice, (uint)i, Surface, out VkBool32 supported);
            if (supportResult == VkResult.Success && supported)
                return (uint)i;
        }
        throw new InvalidOperationException("No present queue family found.");
    }

    private VkQueueFamilyProperties[] GetPhysicalDeviceQueueFamilyProperties(VkPhysicalDevice device)
    {
        uint count = 0;
        InstanceApi.vkGetPhysicalDeviceQueueFamilyProperties(device, &count, null);
        var properties = new VkQueueFamilyProperties[count];
        fixed (VkQueueFamilyProperties* p = properties)
        {
            InstanceApi.vkGetPhysicalDeviceQueueFamilyProperties(device, &count, p);
        }
        return properties;
    }

    private sealed unsafe class StringArrayPin : IDisposable
    {
        public byte** Pointers;
        private readonly GCHandle[] _handles;

        public StringArrayPin(IReadOnlyList<string> strings)
        {
            if (strings.Count == 0)
            {
                Pointers = null;
                _handles = Array.Empty<GCHandle>();
                return;
            }

            Pointers = (byte**)Marshal.AllocHGlobal(strings.Count * sizeof(byte*));
            _handles = new GCHandle[strings.Count];

            for (var i = 0; i < strings.Count; i++)
            {
                var bytes = Encoding.UTF8.GetBytes(strings[i] + '\0');
                _handles[i] = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                Pointers[i] = (byte*)_handles[i].AddrOfPinnedObject();
            }
        }

        public void Dispose()
        {
            if (Pointers == null)
                return;

            foreach (var handle in _handles)
            {
                if (handle.IsAllocated)
                    handle.Free();
            }

            Marshal.FreeHGlobal((nint)Pointers);
            Pointers = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (Device != VkDevice.Null)
            DeviceApi.vkDestroyDevice();
        if (Instance != VkInstance.Null && Surface != VkSurfaceKHR.Null)
            InstanceApi.vkDestroySurfaceKHR(Surface);
        if (Instance != VkInstance.Null)
            InstanceApi.vkDestroyInstance();
    }
}
