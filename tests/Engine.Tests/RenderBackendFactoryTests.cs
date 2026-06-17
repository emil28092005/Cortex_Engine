using Engine.Core;
using Engine.Graphics;

namespace Engine.Tests;

public class RenderBackendFactoryTests
{
    private sealed class FakeRenderContext : IRenderContext
    {
        public IWindow Window => null!;
        public IRenderer CreateRenderer() => null!;
        public void Resize(int width, int height) { }
        public void Dispose() { }
    }

    [Fact]
    public void Create_Returns_Registered_Backend()
    {
        RenderBackendFactory.Register("fake-test", (_, _, _) => new FakeRenderContext());

        var ctx = RenderBackendFactory.Create("fake-test", 800, 600, false);

        Assert.IsType<FakeRenderContext>(ctx);
    }

    [Fact]
    public void Create_Throws_For_Unknown_Backend()
    {
        Assert.Throws<NotSupportedException>(() =>
            RenderBackendFactory.Create("nonexistent", 800, 600, false));
    }

    [Fact]
    public void Register_Is_Case_Insensitive()
    {
        RenderBackendFactory.Register("CaseTest", (_, _, _) => new FakeRenderContext());

        var ctx = RenderBackendFactory.Create("casetest", 1, 1, false);
        Assert.IsType<FakeRenderContext>(ctx);
    }
}
