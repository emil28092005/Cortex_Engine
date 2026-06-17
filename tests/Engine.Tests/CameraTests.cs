using System.Numerics;
using Engine.Core.Components;

namespace Engine.Tests;

public class CameraTests
{
    [Fact]
    public void View_Matrix_Transforms_Position_To_Origin()
    {
        var cam = new Camera(
            new Vector3(0, 0, 10),
            new Vector3(0, 0, 0),
            Vector3.UnitY,
            MathF.PI / 4f,
            16f / 9f,
            0.1f,
            100f);

        var view = cam.GetViewMatrix();
        var originInCameraSpace = Vector3.Transform(new Vector3(0, 0, 0), view);

        Assert.Equal(0f, originInCameraSpace.X, 0.001f);
        Assert.Equal(0f, originInCameraSpace.Y, 0.001f);
        Assert.Equal(-10f, originInCameraSpace.Z, 0.001f);
    }

    [Fact]
    public void Projection_Matrix_Has_Correct_Aspect_Ratio()
    {
        var cam = new Camera(
            Vector3.Zero,
            Vector3.UnitZ,
            Vector3.UnitY,
            MathF.PI / 4f,
            16f / 9f,
            0.1f,
            100f);

        var proj = cam.GetProjectionMatrix();

        Assert.True(proj.M11 > 0);
        Assert.True(proj.M22 > 0);
        Assert.Equal(0f, proj.M41, 0.001f);
    }

    [Fact]
    public void Default_Material_Has_Expected_Values()
    {
        var mat = Material.Default;

        Assert.Equal(0.5f, mat.Roughness);
        Assert.Equal(0.0f, mat.Metallic);
        Assert.False(mat.HasTexture);
    }

    [Fact]
    public void Material_With_Texture_Path_Has_Texture_Flag()
    {
        var mat = new Material(texturePath: "Content/test.png");

        Assert.True(mat.HasTexture);
        Assert.Equal("Content/test.png", mat.TexturePath);
    }
}
