using System.Diagnostics;

namespace Railway.Toolkit;

/// <summary>
/// A timer that reads the CPU performance counter directly via <see cref="Stopwatch.GetTimestamp"/>.
/// Higher precision than <see cref="StopwatchRailwayTimer"/> - avoids the Stopwatch object allocation
/// and reads the underlying counter with minimal overhead.
/// Best suited for development, testing, or profiling scenarios where sub-microsecond accuracy matters.
/// </summary>
internal sealed class TimestampRailwayTimer : IRailwayTimer
{
    private readonly long _startTimestamp = Stopwatch.GetTimestamp();

    /// <inheritdoc />
    public TimeSpan GetElapsed() => Stopwatch.GetElapsedTime(_startTimestamp);

    /// <inheritdoc />
    public void Dispose() { }
}
