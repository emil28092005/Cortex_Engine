namespace Engine.Core;

/// <summary>
/// Common interface for camera controllers (free-fly, etc.).
/// </summary>
public interface ICameraController
{
    string Name { get; }
    void Update(IInputState input, float deltaTime);
}
