using Microsoft.Extensions.Logging;

namespace Railway.Toolkit;

/// <summary>
/// Provides scoped logging configuration for Railway.Toolkit operations.
/// Uses AsyncLocal to isolate logging context per async execution flow,
/// allowing concurrent operations to have independent logging configurations.
/// </summary>
public static class RailwayLogging
{
    private static readonly AsyncLocal<IRailwayLogger?> _logger = new AsyncLocal<IRailwayLogger?>();
    private static readonly AsyncLocal<RailwayLoggingOptions?> _options = new AsyncLocal<RailwayLoggingOptions?>();

    /// <summary>
    /// Gets the configured logger for the current async context.
    /// Returns null if logging has not been enabled in this context.
    /// </summary>
    internal static IRailwayLogger? Logger => _logger.Value;

    /// <summary>
    /// Gets the logging options for the current async context.
    /// Falls back to default options if none have been configured.
    /// </summary>
    internal static RailwayLoggingOptions Options => _options.Value ?? new RailwayLoggingOptions();

    /// <summary>
    /// Enables logging for the current async execution context.
    /// Dispose the returned scope to restore the previous logging configuration.
    /// </summary>
    /// <param name="logger">The Microsoft.Extensions.Logging.ILogger to write to.</param>
    /// <param name="options">
    /// Logging options. If null, uses <see cref="RailwayLoggingOptions.Default"/>
    /// which selects sensible defaults based on ASPNETCORE_ENVIRONMENT.
    /// </param>
    /// <returns>
    /// An <see cref="IDisposable"/> scope. Disposing it restores whatever logging
    /// configuration was active before this call — useful for nested scopes in tests
    /// or request pipelines.
    /// </returns>
    /// <example>
    /// using (RailwayLogging.EnableLogging(logger))
    /// {
    ///     Result&lt;int&gt; result = Result.Ok(42).Map(x => x * 2);
    /// }
    /// </example>
    public static IDisposable EnableLogging(ILogger logger, RailwayLoggingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        IRailwayLogger? previousLogger = _logger.Value;
        RailwayLoggingOptions? previousOptions = _options.Value;

        RailwayLoggingOptions resolvedOptions = options ?? RailwayLoggingOptions.Default;

        _options.Value = resolvedOptions;
        _logger.Value = resolvedOptions.Enabled ? new RailwayLogger(logger, resolvedOptions) : null;

        return new LoggingScope(() =>
        {
            _logger.Value = previousLogger;
            _options.Value = previousOptions;
        });
    }

    /// <summary>
    /// Creates a timer for the current async context using the configured timing strategy.
    /// Returns a <see cref="NullRailwayTimer"/> if no options are configured.
    /// </summary>
    internal static IRailwayTimer StartOperation()
    {
        return RailwayTimerFactory.Create(Options);
    }

    /// <summary>
    /// Manages the lifetime of a logging scope, restoring the previous context on dispose.
    /// </summary>
    private sealed class LoggingScope : IDisposable
    {
        private readonly Action _restore;
        private bool _disposed;

        public LoggingScope(Action restore)
        {
            _restore = restore;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _restore();
            }
        }
    }
}
