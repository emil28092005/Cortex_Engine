using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Engine.Core;
using SDL;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Engine.Graphics;

/// <summary>
/// Owns the Vulkan instance, physical device, logical device, queues, and surface.
/// Uses Silk.NET.Vulkan because Vortice.Vulkan's loader segfaulted on this Kubuntu setup.
/// </summary>
public sealed unsafe class VulkanContext : IDisposable
{
    private bool _disposed;

    public Vk Vk { get; }
    public KhrSurface? KhrSurface { get; private set; }
    public KhrSwapchain? KhrSwapchain { get; private set; }
    public Instance Instance { get; private set; }
    public PhysicalDevice PhysicalDevice { get; private set; }
    public Device Device { get; private set; }
    public Queue GraphicsQueue { get; private set; }
    public Queue PresentQueue { get; private set; }
    public SurfaceKHR Surface { get; private set; }
    public uint GraphicsFamilyIndex { get; private set; }
    public uint PresentFamilyIndex { get; private set; }
    public CommandPool CommandPool { get; private set; }

    public VulkanContext(IWindow window, bool enableValidation = true)
    {
        Vk = Vk.GetApi();
        CreateInstance(window, enableValidation);
        LoadInstanceExtensions();
        CreateSurface(window);
        PickPhysicalDevice();
        CreateLogicalDevice();
        LoadDeviceExtensions();
        GetQueues();
        CreateCommandPool();
    }

    private void CreateCommandPool()
    {
        var createInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = GraphicsFamilyIndex,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };

        CommandPool commandPool;
        var result = Vk.CreateCommandPool(Device, &createInfo, null, &commandPool);
        if (result != Result.Success)
            throw new InvalidOperationException($"vkCreateCommandPool failed: {result}");
        CommandPool = commandPool;
    }

    private void CreateInstance(IWindow window, bool enableValidation)
    {
        var requiredExtensions = new List<string>(window.GetRequiredVulkanExtensions());
        if (enableValidation)
        {
            requiredExtensions.Add("VK_EXT_debug_utils");
        }

        var layerNames = enableValidation
            ? new[] { "VK_LAYER_KHRONOS_validation" }
            : Array.Empty<string>();

        var appName = SilkMarshal.StringToMemory("Cortex Engine", NativeStringEncoding.UTF8);
        var engineName = SilkMarshal.StringToMemory("CortexEngine", NativeStringEncoding.UTF8);
        var extensionMemory = SilkMarshal.StringArrayToMemory(requiredExtensions, NativeStringEncoding.UTF8);
        var layerMemory = SilkMarshal.StringArrayToMemory(layerNames, NativeStringEncoding.UTF8);

        try
        {
            var appInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)appName.Handle,
                PEngineName = (byte*)engineName.Handle,
                ApiVersion = Vk.Version13
            };

            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo,
                EnabledExtensionCount = (uint)requiredExtensions.Count,
                PpEnabledExtensionNames = (byte**)extensionMemory.Handle,
                EnabledLayerCount = (uint)layerNames.Length,
                PpEnabledLayerNames = (byte**)layerMemory.Handle
            };

            Instance instance;
            var result = Vk.CreateInstance(&createInfo, null, &instance);
            if (result != Result.Success)
                throw new InvalidOperationException($"vkCreateInstance failed: {result}");
            Instance = instance;
        }
        finally
        {
            appName.Dispose();
            engineName.Dispose();
            extensionMemory.Dispose();
            layerMemory.Dispose();
        }
    }

    private void LoadInstanceExtensions()
    {
        if (!Vk.TryGetInstanceExtension(Instance, out KhrSurface khrSurface))
            throw new InvalidOperationException("VK_KHR_surface not available.");
        KhrSurface = khrSurface;
    }

    private void LoadDeviceExtensions()
    {
        if (!Vk.TryGetDeviceExtension(Instance, Device, out KhrSwapchain khrSwapchain))
            throw new InvalidOperationException("VK_KHR_swapchain not available.");
        KhrSwapchain = khrSwapchain;
    }

    private void CreateSurface(IWindow window)
    {
        var sdlInstance = (SDL.VkInstance_T*)Instance.Handle;
        var sdlSurface = (SDL.VkSurfaceKHR_T*)null;
        var sdlResult = SDL3.SDL_Vulkan_CreateSurface(
            (SDL_Window*)window.Handle,
            sdlInstance,
            null,
            &sdlSurface);

        if (sdlResult != true)
            throw new InvalidOperationException($"SDL_Vulkan_CreateSurface failed: {SDL3.SDL_GetError()}");

        Surface = new SurfaceKHR((ulong)sdlSurface);
    }

    private void PickPhysicalDevice()
    {
        var devices = EnumeratePhysicalDevices();
        if (devices.Length == 0)
            throw new InvalidOperationException("No Vulkan physical devices found.");

        foreach (var device in devices)
        {
            var properties = Vk.GetPhysicalDeviceProperties(device);
            var queueFamilies = GetPhysicalDeviceQueueFamilyProperties(device);

            var hasGraphics = false;
            var hasPresent = false;
            for (var i = 0; i < queueFamilies.Length; i++)
            {
                if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                    hasGraphics = true;

                Bool32 supported;
                KhrSurface!.GetPhysicalDeviceSurfaceSupport(device, (uint)i, Surface, &supported);
                if (supported)
                    hasPresent = true;
            }

            if (hasGraphics && hasPresent)
            {
                PhysicalDevice = device;
                if (properties.DeviceType == PhysicalDeviceType.DiscreteGpu)
                    break;
            }
        }

        if (PhysicalDevice.Handle == 0)
            throw new InvalidOperationException("No suitable Vulkan physical device found.");
    }

    private PhysicalDevice[] EnumeratePhysicalDevices()
    {
        uint count = 0;
        Vk.EnumeratePhysicalDevices(Instance, &count, null);
        if (count == 0)
            return Array.Empty<PhysicalDevice>();

        var devices = new PhysicalDevice[count];
        fixed (PhysicalDevice* p = devices)
        {
            var result = Vk.EnumeratePhysicalDevices(Instance, &count, p);
            if (result != Result.Success)
                throw new InvalidOperationException($"vkEnumeratePhysicalDevices failed: {result}");
        }
        return devices;
    }

    private QueueFamilyProperties[] GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice device)
    {
        uint count = 0;
        Vk.GetPhysicalDeviceQueueFamilyProperties(device, &count, null);
        var properties = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* p = properties)
        {
            Vk.GetPhysicalDeviceQueueFamilyProperties(device, &count, p);
        }
        return properties;
    }

    private void CreateLogicalDevice()
    {
        var queueFamilies = GetPhysicalDeviceQueueFamilyProperties(PhysicalDevice);
        GraphicsFamilyIndex = FindQueueFamilyIndex(queueFamilies, QueueFlags.GraphicsBit);
        PresentFamilyIndex = FindPresentQueueFamilyIndex(queueFamilies);

        var uniqueFamilies = new HashSet<uint> { GraphicsFamilyIndex, PresentFamilyIndex };
        var queueCreateInfos = uniqueFamilies.Select(family => new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = family,
            QueueCount = 1
        }).ToArray();

        var extensionNames = new[] { "VK_KHR_swapchain" };
        var extensionMemory = SilkMarshal.StringArrayToMemory(extensionNames, NativeStringEncoding.UTF8);

        var priorityHandles = new GCHandle[queueCreateInfos.Length];
        try
        {
            var deviceFeatures = new PhysicalDeviceFeatures();

            for (var i = 0; i < queueCreateInfos.Length; i++)
            {
                var priority = new[] { 1.0f };
                var handle = GCHandle.Alloc(priority, GCHandleType.Pinned);
                priorityHandles[i] = handle;
                queueCreateInfos[i].PQueuePriorities = (float*)handle.AddrOfPinnedObject();
            }

            fixed (DeviceQueueCreateInfo* pQueue = queueCreateInfos)
            {
                var createInfo = new DeviceCreateInfo
                {
                    SType = StructureType.DeviceCreateInfo,
                    QueueCreateInfoCount = (uint)queueCreateInfos.Length,
                    PQueueCreateInfos = pQueue,
                    PEnabledFeatures = &deviceFeatures,
                    EnabledExtensionCount = 1,
                    PpEnabledExtensionNames = (byte**)extensionMemory.Handle
                };

                Device device;
                var result = Vk.CreateDevice(PhysicalDevice, &createInfo, null, &device);
                if (result != Result.Success)
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
            extensionMemory.Dispose();
        }
    }

    private void GetQueues()
    {
        Queue graphicsQueue;
        Vk.GetDeviceQueue(Device, GraphicsFamilyIndex, 0, &graphicsQueue);
        GraphicsQueue = graphicsQueue;

        Queue presentQueue;
        Vk.GetDeviceQueue(Device, PresentFamilyIndex, 0, &presentQueue);
        PresentQueue = presentQueue;
    }

    private uint FindQueueFamilyIndex(QueueFamilyProperties[] properties, QueueFlags flags)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            if (properties[i].QueueFlags.HasFlag(flags))
                return (uint)i;
        }
        throw new InvalidOperationException($"No queue family with flags {flags} found.");
    }

    private uint FindPresentQueueFamilyIndex(QueueFamilyProperties[] properties)
    {
        for (var i = 0; i < properties.Length; i++)
        {
            Bool32 supported;
            KhrSurface!.GetPhysicalDeviceSurfaceSupport(PhysicalDevice, (uint)i, Surface, &supported);
            if (supported)
                return (uint)i;
        }
        throw new InvalidOperationException("No present queue family found.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Vk.DeviceWaitIdle(Device);
        if (CommandPool.Handle != 0)
            Vk.DestroyCommandPool(Device, CommandPool, null);
        if (Device.Handle != 0)
            Vk.DestroyDevice(Device, null);
        if (Surface.Handle != 0)
            KhrSurface?.DestroySurface(Instance, Surface, null);
        if (Instance.Handle != 0)
            Vk.DestroyInstance(Instance, null);
        Vk.Dispose();
    }
}
