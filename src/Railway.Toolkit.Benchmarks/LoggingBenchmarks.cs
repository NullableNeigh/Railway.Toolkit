using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

namespace Railway.Toolkit;

public enum BenchmarkLoggingMode
{
    Absent,
    Disabled,
    ProviderDisabled,
    EnabledUntimed,
    EnabledTimestamp,
    EnabledStopwatch
}

[MemoryDiagnoser]
public class LoggingBenchmarks
{
    private readonly Result<int> _input = Result.Ok(10);
    private readonly Error _error = Error.Create("Invalid", "Benchmark.Invalid");
    private IDisposable? _scope;
    private CountingLogger _logger = null!;

    [ParamsAllValues]
    public BenchmarkLoggingMode Mode { get; set; }

    public long EventCount => _logger.EventCount;

    [GlobalSetup]
    public void Setup()
    {
        _logger = new CountingLogger(Mode != BenchmarkLoggingMode.ProviderDisabled);
        if (Mode == BenchmarkLoggingMode.Absent)
        {
            return;
        }
        RailwayLoggingOptions options = new RailwayLoggingOptions
        {
            Enabled = Mode != BenchmarkLoggingMode.Disabled,
            TimingStrategy = Mode switch
            {
                BenchmarkLoggingMode.EnabledTimestamp => TimingStrategy.Timestamp,
                BenchmarkLoggingMode.EnabledStopwatch => TimingStrategy.Stopwatch,
                _ => TimingStrategy.None
            },
            SamplingRate = 1,
            LogSuccessValues = false,
            LogSlowOperationsOnly = false
        };
        _scope = RailwayLogging.EnableLogging(_logger, options);
    }

    [Benchmark]
    public Result<int> Pipeline() => _input
        .Map(static value => value + 1)
        .Bind(static value => Result.Ok(value * 2))
        .Ensure(static value => value > 0, _error);

    [GlobalCleanup]
    public void Cleanup() => _scope?.Dispose();

    // A minimal non-I/O sink isolates library logging costs from a chosen provider.
    private sealed class CountingLogger(bool enabled) : ILogger
    {
        public long EventCount { get; private set; }
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => EmptyScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => enabled;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            EventCount++;
        }
    }

    private sealed class EmptyScope : IDisposable
    {
        internal static readonly EmptyScope Instance = new EmptyScope();
        public void Dispose() { }
    }
}
