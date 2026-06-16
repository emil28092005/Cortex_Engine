using System.Numerics;

namespace Engine.Core;

/// <summary>
/// A simple 3D vertex with position and color.
/// Layout matches the Vulkan vertex input description.
/// </summary>
public struct Vertex
{
    public Vector3 Position;
    public Vector3 Color;

    public Vertex(Vector3 position, Vector3 color)
    {
        Position = position;
        Color = color;
    }
}
