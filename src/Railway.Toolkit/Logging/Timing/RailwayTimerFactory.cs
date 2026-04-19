namespace Railway.Toolkit;

/// <summary>
/// Creates <see cref="IRailwayTimer"/> instances based on the configured timing strategy.
/// </summary>
internal static class RailwayTimerFactory
{
    /// <summary>
    /// Creates a timer appropriate for the given options.
    /// </summary>
    /// <param name="options">The logging options containing the desired timing strategy.</param>
    /// <returns>An <see cref="IRailwayTimer"/> for the requested strategy.</returns>
    public static IRailwayTimer Create(RailwayLoggingOptions options)
    {
        return options.TimingStrategy switch
        {
            TimingStrategy.None => NullRailwayTimer.Instance,
            TimingStrategy.Stopwatch => new StopwatchRailwayTimer(),
            TimingStrategy.Timestamp => new TimestampRailwayTimer(),
            _ => NullRailwayTimer.Instance
        };
    }
}
