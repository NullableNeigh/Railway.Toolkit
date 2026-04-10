namespace Railway.Toolkit;

/// <summary>
/// Timing strategy for measuring operation duration.
/// </summary>
public enum TimingStrategy
{
    /// <summary>
    /// High precision timing using Stopwatch (QueryPerformanceCounter).
    /// Best for development - most accurate but higher overhead.
    /// </summary>
    HighPrecision,

    /// <summary>
    /// Standard timing using DateTime.
    /// Good for staging - balanced accuracy and performance.
    /// </summary>
    Standard,

    /// <summary>
    /// No timing measurement.
    /// Best for production - minimal overhead.
    /// </summary>
    None
}

/// <summary>
/// Configuration options for Railway.Toolkit logging.
/// </summary>
public sealed class RailwayLoggingOptions
{
    /// <summary>
    /// Gets or sets whether logging is enabled.
    /// Default: true (development-oriented).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timing strategy for measuring operation duration.
    /// Default: HighPrecision (development-oriented).
    /// </summary>
    public TimingStrategy TimingStrategy { get; set; } = TimingStrategy.HighPrecision;

    /// <summary>
    /// Gets or sets the threshold for logging slow operations.
    /// Only operations exceeding this duration will be logged.
    /// Null means log all operations.
    /// Default: 100ms (development-oriented).
    /// </summary>
    public TimeSpan? SlowOperationThreshold { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the sampling rate for logging operations.
    /// Value of 1 logs every operation, 10 logs every 10th operation, etc.
    /// Default: 1 (every operation, development-oriented).
    /// </summary>
    public int SamplingRate { get; set; } = 1;
}
