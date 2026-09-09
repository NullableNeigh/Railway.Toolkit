using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Railway.Toolkit;

/// <summary>
/// Implementation of <see cref="IRailwayLogger"/> that writes to Microsoft.Extensions.Logging.
/// Inspects the full input/output Result context to determine log level, message, and structured data.
/// </summary>
internal sealed class RailwayLogger : IRailwayLogger
{
    private readonly ILogger _logger;
    private readonly RailwayLoggingOptions _options;
    private int _operationCount;

    public RailwayLogger(ILogger logger, RailwayLoggingOptions options)
    {
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc />
    public IRailwayTimer StartOperation()
    {
        if (!_options.Enabled ||
            (!_logger.IsEnabled(LogLevel.Debug) && !_logger.IsEnabled(LogLevel.Warning)))
        {
            return NullRailwayTimer.Instance;
        }

        return RailwayTimerFactory.Create(_options);
    }

    /// <inheritdoc />
    public void LogOperation<TIn, TOut>(
        string operation,
        Result<TIn> input,
        Result<TOut> output,
        TimeSpan elapsed)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (_options.LogSlowOperationsOnly && elapsed < _options.SlowOperationThreshold)
        {
            return;
        }

        bool inputWasOk = input is Result<TIn>.Ok;
        bool outputIsOk = output is Result<TOut>.Ok;
        bool isSlow = elapsed > _options.SlowOperationThreshold;

        LogLevel logLevel = DetermineLogLevel(inputWasOk, outputIsOk, isSlow);
        if (!_logger.IsEnabled(logLevel) || !ShouldLog())
        {
            return;
        }

        string emoji = isSlow ? "🐌" : "🚂";

        Dictionary<string, object> scopeData = BuildScopeData<TIn, TOut>(operation, outputIsOk, elapsed);

        if (_options.LogSuccessValues && output is Result<TOut>.Ok okOutput)
        {
            scopeData["OutputValue"] = okOutput.Value?.ToString() ?? "null";
        }

        if (inputWasOk && output is Result<TOut>.Fail failedOutput)
        {
            scopeData["ErrorCode"] = failedOutput.Error.Code;
            scopeData["ErrorMessage"] = failedOutput.Error.Message;
        }

        using IDisposable? scope = _logger.BeginScope(scopeData);

        if (!inputWasOk)
        {
            // Already on failure track — operation was skipped
            _logger.Log(logLevel, "{Emoji} \"{Operation}\" skipped, {Duration}",
                emoji, operation, FormatDuration(elapsed));
        }
        else if (outputIsOk)
        {
            // Success track — operation executed and stayed on success track
            _logger.Log(logLevel, "{Emoji} \"{Operation}\" executed ✓, {Duration}",
                emoji, operation, FormatDuration(elapsed));
        }
        else
        {
            // Success -> Failure — operation switched to failure track
            Result<TOut>.Fail fail = (Result<TOut>.Fail)output;
            _logger.Log(logLevel, "{Emoji} \"{Operation}\" failed ({ErrorMessage}), {Duration}",
                emoji, operation, fail.Error.Message, FormatDuration(elapsed));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldLog()
    {
        if (_options.SamplingRate <= 0)
        {
            return false;
        }

        if (_options.SamplingRate == 1)
        {
            return true;
        }

        int count = Interlocked.Increment(ref _operationCount);
        return (count % _options.SamplingRate) == 0;
    }

    private Dictionary<string, object> BuildScopeData<TIn, TOut>(
        string operation,
        bool success,
        TimeSpan elapsed)
    {
        Dictionary<string, object> scopeData = new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["InputType"] = typeof(TIn).Name,
            ["OutputType"] = typeof(TOut).Name,
            ["Success"] = success
        };

        if (_options.TimingStrategy != TimingStrategy.None)
        {
            scopeData["DurationMs"] = elapsed.TotalMilliseconds;
        }

        return scopeData;
    }

    private static LogLevel DetermineLogLevel(bool inputWasOk, bool outputIsOk, bool isSlow)
    {
        if (isSlow)
        {
            return LogLevel.Warning;
        }

        if (inputWasOk && !outputIsOk)
        {
            // Switched to failure track
            return LogLevel.Warning;
        }

        if (!inputWasOk)
        {
            // Skipped — already on failure track, low signal
            return LogLevel.Debug;
        }

        return LogLevel.Debug;
    }

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
}
