using System.Diagnostics;

namespace Railway.Toolkit;

/// <summary>
/// A timer backed by <see cref="Stopwatch"/>.
/// Good general-purpose choice - familiar, cross-platform, and accurate enough for most scenarios.
/// </summary>
internal sealed class StopwatchRailwayTimer : IRailwayTimer
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <inheritdoc />
    public TimeSpan GetElapsed() => _stopwatch.Elapsed;

    /// <inheritdoc />
    public void Dispose() => _stopwatch.Stop();
}
