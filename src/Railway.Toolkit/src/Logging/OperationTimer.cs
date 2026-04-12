using System.Diagnostics;

namespace Railway.Toolkit;

/// <summary>
/// Measures operation duration using configurable timing strategies.
/// Implements IDisposable for use in using statements.
/// </summary>
internal sealed class OperationTimer : IDisposable
{
    private readonly TimingStrategy _strategy;
    private readonly Stopwatch? _stopwatch;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationTimer"/> class.
    /// </summary>
    /// <param name="strategy">The timing strategy to use.</param>
    public OperationTimer(TimingStrategy strategy)
    {
        _strategy = strategy;

        switch (strategy)
        {
            case TimingStrategy.Timestamp:
            case TimingStrategy.Stopwatch:
                _stopwatch = Stopwatch.StartNew();
                break;

            case TimingStrategy.None:
                // No timing needed
                break;
        }
    }

    /// <summary>
    /// Gets the elapsed time since the timer started.
    /// </summary>
    public TimeSpan Elapsed
    {
        get
        {
            return _strategy switch
            {
                TimingStrategy.Timestamp => _stopwatch?.Elapsed ?? TimeSpan.Zero,
                TimingStrategy.Stopwatch => _stopwatch?.Elapsed ?? TimeSpan.Zero,
                TimingStrategy.None => TimeSpan.Zero,
                _ => TimeSpan.Zero
            };
        }
    }

    /// <summary>
    /// Stops the timer and releases resources.
    /// </summary>
    public void Dispose()
    {
        _stopwatch?.Stop();
    }
}
