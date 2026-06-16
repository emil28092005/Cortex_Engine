using System;
using System.IO;
using System.Reflection;

namespace Engine.Graphics;

/// <summary>
/// Loads SPIR-V shader bytecode embedded in the assembly.
/// </summary>
public static class ShaderLoader
{
    public static byte[] Load(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Engine.Graphics.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded shader resource not found: {resourceName}");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
