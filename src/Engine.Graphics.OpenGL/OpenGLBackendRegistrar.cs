using Engine.Graphics;

namespace Engine.Graphics.OpenGL;

/// <summary>
/// Triggers registration of the OpenGL backend with the HAL factory.
/// </summary>
public static class OpenGLBackendRegistrar
{
    static OpenGLBackendRegistrar()
    {
        RenderBackendFactory.Register("opengl", (width, height, _) => new OpenGLRenderContext(width, height));
    }

    public static void EnsureRegistered() { }
}
