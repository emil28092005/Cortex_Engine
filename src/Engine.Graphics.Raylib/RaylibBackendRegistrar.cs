using Engine.Graphics;

namespace Engine.Graphics.RaylibBackend;

/// <summary>
/// Triggers registration of the Raylib backend with the HAL factory.
/// </summary>
public static class RaylibBackendRegistrar
{
    static RaylibBackendRegistrar()
    {
        RenderBackendFactory.Register("raylib", (width, height, _) => new RaylibRenderContext(width, height));
    }

    /// <summary>
    /// No-op method that forces the static constructor to run.
    /// Call this before using <see cref="RenderBackendFactory.Create"/>.
    /// </summary>
    public static void EnsureRegistered() { }
}
