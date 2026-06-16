using Engine.Core;

namespace Engine.Core.Components;

/// <summary>
/// A mesh component for the ECS.
/// Keeps CPU-side vertex/index data that the renderer uploads to GPU buffers.
/// </summary>
public record struct Mesh
{
    public Vertex[] Vertices;
    public uint[] Indices;

    public Mesh(Vertex[] vertices, uint[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}
