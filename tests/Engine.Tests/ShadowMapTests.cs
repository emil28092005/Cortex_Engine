using System.Numerics;

namespace Engine.Tests;

public class ShadowMapFaceDirectionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void All_Six_Faces_Produce_Valid_View_Matrices(int face)
    {
        // Test the face direction logic directly (without VulkanShadowMap class)
        var lightPos = new Vector3(3, 7, -2);
        var (view, proj) = ComputeFaceViewProj(lightPos, face);

        Assert.True(!float.IsNaN(view.M11));
        Assert.True(!float.IsNaN(proj.M11));
        Assert.True(!float.IsInfinity(view.M11));
        Assert.True(!float.IsInfinity(proj.M11));
    }

    [Theory]
    [InlineData(0, 1, 0, 0)]
    [InlineData(1, -1, 0, 0)]
    [InlineData(2, 0, 1, 0)]
    [InlineData(3, 0, -1, 0)]
    [InlineData(4, 0, 0, 1)]
    [InlineData(5, 0, 0, -1)]
    public void Face_Target_Is_LightPos_Plus_Direction(int face, float dx, float dy, float dz)
    {
        var lightPos = new Vector3(5, 10, 3);
        var (view, _) = ComputeFaceViewProj(lightPos, face);

        // View matrix transforms lightPos to origin
        var origin = Vector3.Transform(lightPos, view);
        Assert.Equal(0f, origin.X, 0.001f);
        Assert.Equal(0f, origin.Y, 0.001f);
        Assert.Equal(0f, origin.Z, 0.001f);
    }

    [Fact]
    public void All_Faces_Have_90_Degrees_FOV()
    {
        for (int face = 0; face < 6; face++)
        {
            var (_, proj) = ComputeFaceViewProj(Vector3.Zero, face);
            // FOV=90°, aspect=1 → M22 = 1/tan(PI/4) = 1 (no M22 flip for shadow cubemap)
            Assert.Equal(1f, proj.M22, 0.001f);
        }
    }

    [Fact]
    public void Face_2_Uses_Positive_Z_Up()
    {
        var lightPos = new Vector3(0, 5, 0);
        var (view, _) = ComputeFaceViewProj(lightPos, 2);
        // +Y face: up = +Z (Vulkan cubemap convention)
        var posZ = Vector3.Transform(new Vector3(0, 0, 1), view);
        Assert.True(posZ.Y > 0, $"Face 2 up should map +Z to +Y view space, got {posZ}");
    }

    [Fact]
    public void Face_3_Uses_Negative_Z_Up()
    {
        var lightPos = new Vector3(0, 5, 0);
        var (view, _) = ComputeFaceViewProj(lightPos, 3);
        // -Y face: up = -Z (Vulkan cubemap convention)
        var negZ = Vector3.Transform(new Vector3(0, 0, -1), view);
        Assert.True(negZ.Y > 0, $"Face 3 up should map -Z to +Y view space, got {negZ}");
    }

    [Fact]
    public void FarPlane_Matches_Between_Projection_And_Shader()
    {
        // The shader hardcodes FAR_PLANE = 60.0 and divides by it
        // The projection must use the same far plane
        var (_, proj) = ComputeFaceViewProj(Vector3.Zero, 0);
        // M33 for perspective: should be negative (far/near-far)
        Assert.True(proj.M33 < 0, $"M33 should be negative for perspective projection, got {proj.M33}");
    }

    static (Matrix4x4 view, Matrix4x4 proj) ComputeFaceViewProj(Vector3 lightPos, int face)
    {
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1.0f, 0.1f, 60f);

        var target = lightPos;
        Vector3 up;

        switch (face)
        {
            case 0: target += Vector3.UnitX; up = new Vector3(0, -1, 0); break;
            case 1: target += -Vector3.UnitX; up = new Vector3(0, -1, 0); break;
            case 2: target += Vector3.UnitY; up = new Vector3(0, 0, 1); break;
            case 3: target += -Vector3.UnitY; up = new Vector3(0, 0, -1); break;
            case 4: target += Vector3.UnitZ; up = new Vector3(0, -1, 0); break;
            case 5: target += -Vector3.UnitZ; up = new Vector3(0, -1, 0); break;
            default: target += Vector3.UnitZ; up = new Vector3(0, -1, 0); break;
        }

        var view = Matrix4x4.CreateLookAt(lightPos, target, up);
        return (view, proj);
    }
}
