using System;
using System.Diagnostics;

namespace Engine.Core;

/// <summary>
/// Frame timing and fixed-timestep helper.
/// </summary>
public sealed class Timing
{
    private readonly Stopwatch _stopwatch;
    private double _lastTime;
    private double _accumulator;

    public double DeltaTime { get; private set; }
    public double TotalTime { get; private set; }
    public double FixedTimeStep { get; set; } = 1.0 / 60.0;
    public double FixedTimeAccumulator => _accumulator;

    public Timing()
    {
        _stopwatch = Stopwatch.StartNew();
        _lastTime = 0.0;
    }

    public void Tick()
    {
        var current = _stopwatch.Elapsed.TotalSeconds;
        DeltaTime = current - _lastTime;
        _lastTime = current;
        TotalTime = current;
        _accumulator += DeltaTime;
    }

    public bool ConsumeFixedStep()
    {
        if (_accumulator < FixedTimeStep)
            return false;

        _accumulator -= FixedTimeStep;
        return true;
    }

    public void ResetAccumulator()
    {
        if (_accumulator > FixedTimeStep * 5)
            _accumulator = FixedTimeStep * 5;
    }
}
