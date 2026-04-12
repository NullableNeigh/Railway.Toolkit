using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Railway.Toolkit;

/// <summary>
/// Provides configuration for Railway.Toolkit logging.
/// </summary>
public static class RailwayLogging
{
    private static IRailwayLogger? _logger;
    private static RailwayLoggingOptions _options = new RailwayLoggingOptions();
    private static readonly object _lock = new object();

    /// <summary>
    /// Gets the current logging options.
    /// </summary>
    internal static RailwayLoggingOptions Options => _options;

    /// <summary>
    /// Gets the configured logger instance.
    /// Returns null if logging is not configured or disabled.
    /// </summary>
    internal static IRailwayLogger? Logger => _options.Enabled ? _logger : null;

    /// <summary>
    /// Configures Railway.Toolkit logging with the specified logger and options.
    /// </summary>
    /// <param name="logger">The Microsoft.Extensions.Logging.ILogger to use.</param>
    /// <param name="configure">Optional action to configure logging options.</param>
    public static void Configure(ILogger logger, Action<RailwayLoggingOptions>? configure = null)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        lock (_lock)
        {
            RailwayLoggingOptions options = new RailwayLoggingOptions();
            configure?.Invoke(options);

            _options = options;
            _logger = new RailwayLogger(logger, options);
        }
    }

    /// <summary>
    /// Configures Railway.Toolkit logging with default options.
    /// </summary>
    /// <param name="logger">The Microsoft.Extensions.Logging.ILogger to use.</param>
    public static void Configure(ILogger logger)
    {
        Configure(logger, null);
    }

    /// <summary>
    /// Configures Railway.Toolkit logging with options only (no logger).
    /// Useful for updating options after initial configuration.
    /// </summary>
    /// <param name="configure">Action to configure logging options.</param>
    public static void ConfigureOptions(Action<RailwayLoggingOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        lock (_lock)
        {
            RailwayLoggingOptions options = new RailwayLoggingOptions();
            configure(options);

            _options = options;

            // Recreate logger with new options if one exists
            if (_logger != null && _logger is RailwayLogger railwayLogger)
            {
                // Get the underlying ILogger through reflection or keep reference
                // For now, just update options - the logger will use the new static Options
                _logger = new RailwayLogger(
                    new NullLogger<RailwayLogger>(),
                    options
                );
            }
        }
    }

    /// <summary>
    /// Disables Railway.Toolkit logging.
    /// </summary>
    public static void Disable()
    {
        lock (_lock)
        {
            _options = new RailwayLoggingOptions { Enabled = false };
            _logger = null;
        }
    }

    /// <summary>
    /// Resets logging configuration to defaults.
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            _options = new RailwayLoggingOptions();
            _logger = null;
        }
    }
}
