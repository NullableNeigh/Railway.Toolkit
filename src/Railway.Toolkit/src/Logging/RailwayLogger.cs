using Microsoft.Extensions.Logging;

namespace Railway.Toolkit;

/// <summary>
/// Implementation of <see cref="IRailwayLogger"/> that wraps Microsoft.Extensions.Logging.ILogger.
/// Handles filtering, sampling, and message formatting.
/// </summary>
internal sealed class RailwayLogger : IRailwayLogger
{
    private readonly ILogger _logger;
    private readonly RailwayLoggingOptions _options;
    private static int _operationCount = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="RailwayLogger"/> class.
    /// </summary>
    /// <param name="logger">The underlying Microsoft.Extensions.Logging.ILogger.</param>
    /// <param name="options">The logging configuration options.</param>
    public RailwayLogger(ILogger logger, RailwayLoggingOptions options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Logs a railway operation with its status and duration.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="status">The status of the operation.</param>
    /// <param name="duration">The duration of the operation.</param>
    /// <param name="error">Optional error details.</param>
    public void LogOperation(string operationName, string status, TimeSpan duration, Error? error = null)
    {
        // Check if logging is enabled
        if (!_options.Enabled)
        {
            return;
        }

        // Apply slow operation threshold filter
        if (_options.SlowOperationThreshold.HasValue && duration < _options.SlowOperationThreshold.Value)
        {
            return;
        }

        // Apply sampling
        if (!ShouldLog())
        {
            return;
        }

        // Format the log message
        string message = FormatMessage(operationName, status, duration, error);

        // Determine log level based on status
        LogLevel logLevel = DetermineLogLevel(status, error);

        // Log the message
        _logger.Log(logLevel, message);
    }

    /// <summary>
    /// Determines whether this operation should be logged based on sampling rate.
    /// </summary>
    /// <returns>True if the operation should be logged; otherwise, false.</returns>
    private bool ShouldLog()
    {
        if (_options.SamplingRate <= 0)
        {
            return false;
        }

        if (_options.SamplingRate == 1)
        {
            return true; // Log every operation
        }

        int count = Interlocked.Increment(ref _operationCount);
        return (count % _options.SamplingRate) == 0;
    }

    /// <summary>
    /// Formats the log message with the railway emoji and operation details.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="status">The operation status.</param>
    /// <param name="duration">The operation duration.</param>
    /// <param name="error">Optional error details.</param>
    /// <returns>The formatted log message.</returns>
    private static string FormatMessage(string operationName, string status, TimeSpan duration, Error? error)
    {
        string durationText = FormatDuration(duration);

        if (error != null)
        {
            return $"🚂 \"{operationName}\" {status} ({error.Message}), {durationText}";
        }

        return $"🚂 \"{operationName}\" {status}, {durationText}";
    }

    /// <summary>
    /// Formats the duration in a human-readable format.
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>The formatted duration string.</returns>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 1)
        {
            return $"{duration.TotalMicroseconds:F0}µs";
        }

        if (duration.TotalSeconds < 1)
        {
            return $"{duration.TotalMilliseconds:F0}ms";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{duration.TotalSeconds:F2}s";
        }

        return $"{duration.TotalMinutes:F2}m";
    }

    /// <summary>
    /// Determines the appropriate log level based on operation status.
    /// </summary>
    /// <param name="status">The operation status.</param>
    /// <param name="error">Optional error details.</param>
    /// <returns>The appropriate log level.</returns>
    private static LogLevel DetermineLogLevel(string status, Error? error)
    {
        // Operations with errors are warnings
        if (error != null || status.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            return LogLevel.Warning;
        }

        // Track switches (success↔failure) are informational
        if (status.Contains("switched", StringComparison.OrdinalIgnoreCase))
        {
            return LogLevel.Information;
        }

        // Everything else is debug level
        return LogLevel.Debug;
    }
}
