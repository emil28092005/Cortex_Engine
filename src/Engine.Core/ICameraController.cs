namespace Engine.Core;

/// <summary>
/// Common interface for camera controllers (orbit, free-fly, etc.).
/// </summary>
public interface ICameraController
{
    string Name { get; }
    void Update(InputMapping input, float deltaTime);
}
