using System.Numerics;

namespace Engine.Core;

/// <summary>
/// A 3D vertex with position, color, and normal.
/// Layout matches the Vulkan vertex input description.
/// </summary>
public struct Vertex
{
    public Vector3 Position;
    public Vector3 Color;
    public Vector3 Normal;

    public Vertex(Vector3 position, Vector3 color, Vector3 normal)
    {
        Position = position;
        Color = color;
        Normal = normal;
    }
}
