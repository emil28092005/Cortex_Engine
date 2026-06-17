using System.Numerics;
using Engine.Core.Components;

namespace Engine.Tests;

public class TransformTests
{
    [Fact]
    public void Identity_Transform_Produces_Identity_Matrix()
    {
        var t = new Transform(Vector3.Zero, Quaternion.Identity, Vector3.One);
        var m = t.GetMatrix();

        Assert.Equal(Matrix4x4.Identity, m);
    }

    [Fact]
    public void Translation_Appears_In_Matrix()
    {
        var t = new Transform(new Vector3(1, 2, 3), Quaternion.Identity, Vector3.One);
        var m = t.GetMatrix();

        Assert.Equal(1f, m.M41);
        Assert.Equal(2f, m.M42);
        Assert.Equal(3f, m.M43);
    }

    [Fact]
    public void Scale_Affects_Matrix_Diagonal()
    {
        var t = new Transform(Vector3.Zero, Quaternion.Identity, new Vector3(2, 3, 4));
        var m = t.GetMatrix();

        Assert.Equal(2f, m.M11);
        Assert.Equal(3f, m.M22);
        Assert.Equal(4f, m.M33);
    }

    [Fact]
    public void Rotation_Around_Y_Rotates_X_Axis()
    {
        var angle = MathF.PI / 2f;
        var rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
        var t = new Transform(Vector3.Zero, rot, Vector3.One);
        var m = t.GetMatrix();

        var xAxis = new Vector3(m.M11, m.M21, m.M31);
        Assert.Equal(0f, xAxis.X, 0.001f);
        Assert.Equal(0f, xAxis.Y, 0.001f);
        Assert.Equal(1f, xAxis.Z, 0.001f);
    }
}
