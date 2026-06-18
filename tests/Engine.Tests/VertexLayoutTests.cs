using System.Runtime.InteropServices;
using Engine.Core;

namespace Engine.Tests;

/// <summary>
/// Verifies the Vertex struct layout matches what the Vulkan vertex input description expects.
/// The struct is { Vector3 Position, Vector3 Color, Vector3 Normal } = 9 floats = 36 bytes.
/// </summary>
public unsafe class VertexLayoutTests
{
    [Fact]
    public void Vertex_Is_36_Bytes()
    {
        Assert.Equal(36, sizeof(Vertex));
    }

    [Fact]
    public void Vertex_Has_Three_Vector3_Fields()
    {
        var v = new Vertex(
            new System.Numerics.Vector3(1, 2, 3),
            new System.Numerics.Vector3(4, 5, 6),
            new System.Numerics.Vector3(7, 8, 9));

        Assert.Equal(1f, v.Position.X);
        Assert.Equal(2f, v.Position.Y);
        Assert.Equal(3f, v.Position.Z);
        Assert.Equal(4f, v.Color.X);
        Assert.Equal(5f, v.Color.Y);
        Assert.Equal(6f, v.Color.Z);
        Assert.Equal(7f, v.Normal.X);
        Assert.Equal(8f, v.Normal.Y);
        Assert.Equal(9f, v.Normal.Z);
    }

    [Fact]
    public void Vertex_Position_At_Offset_0()
    {
        Assert.Equal(0, Marshal.OffsetOf<Vertex>("Position"));
    }

    [Fact]
    public void Vertex_Color_At_Offset_12()
    {
        Assert.Equal(12, Marshal.OffsetOf<Vertex>("Color"));
    }

    [Fact]
    public void Vertex_Normal_At_Offset_24()
    {
        Assert.Equal(24, Marshal.OffsetOf<Vertex>("Normal"));
    }
}
