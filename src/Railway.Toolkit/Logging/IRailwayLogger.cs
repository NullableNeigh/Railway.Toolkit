namespace Railway.Toolkit;

/// <summary>
/// Internal abstraction for Railway.Toolkit logging.
/// Receives full Result context so the implementation can make
/// its own decisions about what and how to log.
/// </summary>
internal interface IRailwayLogger
{
    /// <summary>
    /// Logs a railway operation with its full input/output context.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="operation">The name of the operation (e.g., "Map", "Bind").</param>
    /// <param name="input">The result passed into the operation.</param>
    /// <param name="output">The result produced by the operation.</param>
    /// <param name="elapsed">How long the operation took.</param>
    void LogOperation<TIn, TOut>(
        string operation,
        Result<TIn> input,
        Result<TOut> output,
        TimeSpan elapsed);
}
