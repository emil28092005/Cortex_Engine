using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

public static unsafe partial class VulkanNative
{
    private const string VulkanLib = "vulkan-1.dll";
    private const string VulkanLibLinux = "libvulkan.so.1";

    private static nint _libHandle;

    public static nint LoadLibrary()
    {
        if (_libHandle != 0) return _libHandle;

        if (OperatingSystem.IsWindows())
            _libHandle = NativeLibrary.Load(VulkanLib);
        else
            _libHandle = NativeLibrary.Load(VulkanLibLinux);

        if (_libHandle == 0)
            throw new InvalidOperationException("Failed to load Vulkan library.");

        return _libHandle;
    }

    public static void* GetInstanceProcAddr(VkInstance instance, byte* pName)
    {
        LoadLibrary();
        var ptr = NativeLibrary.GetExport(_libHandle, "vkGetInstanceProcAddr");
        var func = Marshal.GetDelegateForFunctionPointer<PFN_vkGetInstanceProcAddr>(ptr);
        return func(instance, pName);
    }

    public static void* GetDeviceProcAddr(VkDevice device, byte* pName)
    {
        var ptr = NativeLibrary.GetExport(_libHandle, "vkGetDeviceProcAddr");
        var func = Marshal.GetDelegateForFunctionPointer<PFN_vkGetDeviceProcAddr>(ptr);
        return func(device, pName);
    }

    public static T LoadInstanceFunction<T>(VkInstance instance, string name) where T : Delegate
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* pName = nameBytes)
        {
            var addr = GetInstanceProcAddr(instance, pName);
            if (addr == null)
                throw new InvalidOperationException($"Failed to load Vulkan instance function: {name}");
            return Marshal.GetDelegateForFunctionPointer<T>((nint)addr);
        }
    }

    public static T LoadDeviceFunction<T>(VkDevice device, string name) where T : Delegate
    {
        var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        fixed (byte* pName = nameBytes)
        {
            var addr = GetDeviceProcAddr(device, pName);
            if (addr == null)
                throw new InvalidOperationException($"Failed to load Vulkan device function: {name}");
            return Marshal.GetDelegateForFunctionPointer<T>((nint)addr);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void* PFN_vkGetInstanceProcAddr(VkInstance instance, byte* pName);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate void* PFN_vkGetDeviceProcAddr(VkDevice device, byte* pName);

    [LibraryImport("vulkan-1.dll", EntryPoint = "vkGetInstanceProcAddr", StringMarshalling = StringMarshalling.Utf8)]
    public static partial void* vkGetInstanceProcAddr_Win(VkInstance instance, string pName);

    [DllImport("libvulkan.so.1", EntryPoint = "vkGetInstanceProcAddr", CharSet = CharSet.Ansi)]
    public static extern void* vkGetInstanceProcAddr_Linux(VkInstance instance, string pName);

    public static VkResult vkEnumerateInstanceExtensionProperties(byte* pLayerName, uint* pPropertyCount, VkExtensionProperties* pProperties)
    {
        LoadLibrary();
        var ptr = NativeLibrary.GetExport(_libHandle, "vkEnumerateInstanceExtensionProperties");
        var func = Marshal.GetDelegateForFunctionPointer<PFN_vkEnumerateInstanceExtensionProperties>(ptr);
        return func(pLayerName, pPropertyCount, pProperties);
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate VkResult PFN_vkEnumerateInstanceExtensionProperties(byte* pLayerName, uint* pPropertyCount, VkExtensionProperties* pProperties);

    public static VkResult vkEnumerateInstanceLayerProperties(uint* pPropertyCount, VkLayerProperties* pProperties)
    {
        LoadLibrary();
        var ptr = NativeLibrary.GetExport(_libHandle, "vkEnumerateInstanceLayerProperties");
        var func = Marshal.GetDelegateForFunctionPointer<PFN_vkEnumerateInstanceLayerProperties>(ptr);
        return func(pPropertyCount, pProperties);
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    public delegate VkResult PFN_vkEnumerateInstanceLayerProperties(uint* pPropertyCount, VkLayerProperties* pProperties);
}
