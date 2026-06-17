using Engine.Core;

namespace Engine.Graphics;

public static class RenderBackendFactory
{
    private static readonly Dictionary<string, Func<int, int, bool, IRenderContext>> _backends =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string name, Func<int, int, bool, IRenderContext> factory)
    {
        _backends[name] = factory;
    }

    public static IRenderContext Create(string name, int width, int height, bool enableValidation)
    {
        if (_backends.TryGetValue(name, out var factory))
            return factory(width, height, enableValidation);

        throw new NotSupportedException(
            $"Unknown render backend '{name}'. Available: {string.Join(", ", _backends.Keys)}");
    }

    public static bool IsRegistered(string name) => _backends.ContainsKey(name);
}
