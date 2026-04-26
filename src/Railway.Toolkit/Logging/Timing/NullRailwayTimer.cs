using System.Runtime.CompilerServices;

namespace Railway.Toolkit;

/// <summary>
/// A no-op timer used when timing is disabled.
/// Singleton - allocates nothing and has zero overhead.
/// </summary>
internal sealed class NullRailwayTimer : IRailwayTimer
{
    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static readonly NullRailwayTimer Instance = new NullRailwayTimer();

    private NullRailwayTimer() { }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimeSpan GetElapsed() => TimeSpan.Zero;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() { }
}
