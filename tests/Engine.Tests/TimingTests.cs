using Engine.Core;

namespace Engine.Tests;

public class TimingTests
{
    [Fact]
    public void Tick_Updates_DeltaTime()
    {
        var timing = new Timing();

        timing.Tick();

        Assert.True(timing.DeltaTime > 0);
    }

    [Fact]
    public void Tick_Updates_TotalTime()
    {
        var timing = new Timing();

        timing.Tick();
        var time1 = timing.TotalTime;
        timing.Tick();
        var time2 = timing.TotalTime;

        Assert.True(time2 > time1);
    }

    [Fact]
    public void ConsumeFixedStep_Returns_True_When_Accumulator_Exceeds_Step()
    {
        var timing = new Timing { FixedTimeStep = 0.001 };

        timing.Tick();
        Thread.Sleep(5);
        timing.Tick();

        Assert.True(timing.ConsumeFixedStep());
    }

    [Fact]
    public void ConsumeFixedStep_Returns_False_When_Below_Step()
    {
        var timing = new Timing { FixedTimeStep = 100.0 };

        timing.Tick();

        Assert.False(timing.ConsumeFixedStep());
    }

    [Fact]
    public void ResetAccumulator_Clamps_To_Max()
    {
        var timing = new Timing { FixedTimeStep = 0.1 };

        // Simulate a huge delta by ticking many times without consuming
        for (var i = 0; i < 1000; i++)
            timing.Tick();

        timing.ResetAccumulator();

        Assert.True(timing.FixedTimeAccumulator <= timing.FixedTimeStep * 5 + 0.001f);
    }
}
