using System.IO;
using Engine.Graphics.Vulkan;

namespace Engine.Tests;

public class ShadowShaderTests
{
    [Fact]
    public void Shadow_Vertex_Shader_Exists()
    {
        Assert.True(File.Exists("Shaders/shadow.vert.spv") ||
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Shaders/shadow.vert.spv")));
    }

    [Fact]
    public void Shadow_Fragment_Shader_Exists()
    {
        Assert.True(File.Exists("Shaders/shadow.frag.spv") ||
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Shaders/shadow.frag.spv")));
    }

    [Fact]
    public void Main_Vertex_Shader_Exists()
    {
        Assert.True(File.Exists("Shaders/triangle.vert.spv") ||
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Shaders/triangle.vert.spv")));
    }

    [Fact]
    public void Main_Fragment_Shader_Exists()
    {
        Assert.True(File.Exists("Shaders/triangle.frag.spv") ||
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Shaders/triangle.frag.spv")));
    }

    [Fact]
    public void ImGui_Shaders_Exist()
    {
        Assert.True(File.Exists("Shaders/imgui.vert.spv") ||
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Shaders/imgui.vert.spv")));
        Assert.True(File.Exists("Shaders/imgui.frag.spv") ||
            File.Exists(Path.Combine(AppContext.BaseDirectory, "Shaders/imgui.frag.spv")));
    }
}
