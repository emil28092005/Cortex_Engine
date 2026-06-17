using Engine.Core;

namespace Engine.Graphics;

/// <summary>
/// Factory for creating concrete graphics backends by name.
/// Backends register themselves so the app only depends on the HAL interfaces.
/// Each backend creates and owns its own window.
/// </summary>
public static class RenderBackendFactory
{
    private static readonly Dictionary<string, Func<int, int, bool, IRenderContext>> _registry
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a backend implementation under the given name.
    /// The factory receives (width, height, enableValidation) and must create
    /// its own window and render context.
    /// </summary>
    public static void Register(string name, Func<int, int, bool, IRenderContext> factory)
    {
        _registry[name] = factory;
    }

    /// <summary>
    /// Create a backend instance for the given name.
    /// The backend assembly must have registered itself before this is called.
    /// </summary>
    public static IRenderContext Create(string name, int width, int height, bool enableValidation)
    {
        if (!_registry.TryGetValue(name, out var factory))
            throw new NotSupportedException($"No graphics backend named '{name}' is registered.");

        return factory(width, height, enableValidation);
    }
}
