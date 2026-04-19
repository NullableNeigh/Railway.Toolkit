namespace Railway.Toolkit;

/// <summary>
/// Timing strategy for measuring operation duration.
/// </summary>
public enum TimingStrategy
{
    /// <summary>
    /// No timing measurement. Zero overhead - uses <see cref="NullRailwayTimer"/>.
    /// Best for production when timing data is not required.
    /// </summary>
    None,

    /// <summary>
    /// Standard timing using <see cref="System.Diagnostics.Stopwatch"/>.
    /// Good general-purpose choice - cross-platform and accurate for most scenarios.
    /// Uses <see cref="StopwatchRailwayTimer"/>.
    /// </summary>
    Stopwatch,

    /// <summary>
    /// High precision timing via <see cref="System.Diagnostics.Stopwatch.GetTimestamp"/>.
    /// Reads the CPU performance counter directly with minimal allocation overhead.
    /// Best for development, testing, or profiling where sub-microsecond accuracy matters.
    /// Uses <see cref="TimestampRailwayTimer"/>.
    /// </summary>
    Timestamp
}

/// <summary>
/// Configuration options for Railway.Toolkit logging.
/// </summary>
public sealed class RailwayLoggingOptions
{
    /// <summary>
    /// Gets or sets whether logging is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timing strategy for measuring operation duration.
    /// Default: <see cref="TimingStrategy.Stopwatch"/>.
    /// </summary>
    public TimingStrategy TimingStrategy { get; set; } = TimingStrategy.Stopwatch;

    /// <summary>
    /// Gets or sets whether to include success values in log output.
    /// Useful in development but should be disabled in production to avoid leaking sensitive data.
    /// Default: false.
    /// </summary>
    public bool LogSuccessValues { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to skip logging operations that complete faster than <see cref="SlowOperationThreshold"/>.
    /// Reduces log noise in high-throughput scenarios.
    /// Default: false (log all operations).
    /// </summary>
    public bool LogSlowOperationsOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets the duration above which an operation is considered slow.
    /// Used for warning-level logging and, when <see cref="LogSlowOperationsOnly"/> is true, for filtering.
    /// Default: 100ms.
    /// </summary>
    public TimeSpan SlowOperationThreshold { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the sampling rate for logging operations.
    /// 1 logs every operation, 10 logs every 10th operation, etc.
    /// Default: 1 (every operation).
    /// </summary>
    public int SamplingRate { get; set; } = 1;

    /// <summary>
    /// Returns environment-aware default options based on the ASPNETCORE_ENVIRONMENT variable.
    /// Development: verbose, high precision, success values enabled.
    /// Testing: high precision, all operations logged.
    /// Staging: slow operations only, standard timing.
    /// Production (default): disabled, no timing overhead.
    /// </summary>
    public static RailwayLoggingOptions Default
    {
        get
        {
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            return env switch
            {
                "Development" => new RailwayLoggingOptions
                {
                    TimingStrategy = TimingStrategy.Timestamp,
                    LogSuccessValues = true,
                    LogSlowOperationsOnly = false
                },
                "Testing" => new RailwayLoggingOptions
                {
                    TimingStrategy = TimingStrategy.Timestamp,
                    LogSuccessValues = false,
                    LogSlowOperationsOnly = false
                },
                "Staging" => new RailwayLoggingOptions
                {
                    TimingStrategy = TimingStrategy.Stopwatch,
                    LogSlowOperationsOnly = true,
                    SlowOperationThreshold = TimeSpan.FromMilliseconds(50)
                },
                _ => new RailwayLoggingOptions
                {
                    Enabled = false,
                    TimingStrategy = TimingStrategy.None
                }
            };
        }
    }
}
