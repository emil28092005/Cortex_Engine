using Engine.Graphics;

namespace Engine.Graphics.OpenTK;

/// <summary>
/// Triggers registration of the OpenTK backend with the HAL factory.
/// </summary>
public static class OpenTKBackendRegistrar
{
    static OpenTKBackendRegistrar()
    {
        RenderBackendFactory.Register("opentk", (width, height, _) => new OpenTKRenderContext(width, height));
    }

    public static void EnsureRegistered() { }
}
