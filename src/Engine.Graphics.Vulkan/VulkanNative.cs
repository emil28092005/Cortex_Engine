using System.Runtime.InteropServices;

namespace Engine.Graphics.Vulkan;

internal static unsafe class VulkanNative
{
    private static readonly nint _handle;
    public static readonly nint NullHandle = 0;

    static VulkanNative()
    {
        var libName = OperatingSystem.IsWindows() ? "vulkan-1.dll" : "libvulkan.so.1";
        _handle = NativeLibrary.Load(libName);
        if (_handle == 0)
            throw new DllNotFoundException($"Failed to load Vulkan loader: {libName}");

        vkGetInstanceProcAddr = GetExport<PFN_vkGetInstanceProcAddr>("vkGetInstanceProcAddr");
    }

    public static T GetExport<T>(string name) where T : Delegate
    {
        if (!NativeLibrary.TryGetExport(_handle, name, out var address))
            throw new EntryPointNotFoundException($"Vulkan export not found: {name}");
        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    public static nint GetExportPointer(string name)
    {
        NativeLibrary.TryGetExport(_handle, name, out var address);
        return address;
    }

    public static PFN_vkGetInstanceProcAddr vkGetInstanceProcAddr;

    public delegate nint PFN_vkGetInstanceProcAddr(nint instance, byte* pName);
}

internal static unsafe class VulkanString
{
    public static byte[] ToUtf8Terminated(string s)
    {
        return System.Text.Encoding.UTF8.GetBytes(s + '\0');
    }

    public static byte* AllocUtf8(string s)
    {
        var bytes = ToUtf8Terminated(s);
        var ptr = (byte*)Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, (nint)ptr, bytes.Length);
        return ptr;
    }

    public static void FreeUtf8(byte* ptr)
    {
        if (ptr != null)
            Marshal.FreeHGlobal((nint)ptr);
    }
}
