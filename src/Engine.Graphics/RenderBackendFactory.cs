namespace Engine.Graphics;

/// <summary>
/// Factory for creating render backends by name.
/// </summary>
public static class RenderBackendFactory
{
    private static readonly Dictionary<string, Func<int, int, bool, IRenderContext>> _backends = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Register a backend factory. Case-insensitive lookup.
    /// </summary>
    public static void Register(string name, Func<int, int, bool, IRenderContext> factory)
    {
        _backends[name] = factory;
    }

    /// <summary>
    /// Create a render context for the given backend.
    /// </summary>
    public static IRenderContext Create(string name, int width, int height, bool enableValidation)
    {
        if (!_backends.TryGetValue(name, out var factory))
            throw new NotSupportedException($"Render backend '{name}' is not registered.");

        return factory(width, height, enableValidation);
    }
}
