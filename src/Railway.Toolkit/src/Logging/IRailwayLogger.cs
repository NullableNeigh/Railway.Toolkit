namespace Railway.Toolkit;

/// <summary>
/// Internal abstraction for Railway.Toolkit logging.
/// </summary>
internal interface IRailwayLogger
{
    /// <summary>
    /// Logs a railway operation with its status and duration.
    /// </summary>
    /// <param name="operationName">The name of the operation (e.g., "Map", "Bind").</param>
    /// <param name="status">The status of the operation (e.g., "executed", "skipped", "failed").</param>
    /// <param name="duration">The duration of the operation.</param>
    /// <param name="error">Optional error details if the operation failed.</param>
    void LogOperation(string operationName, string status, TimeSpan duration, Error? error = null);
}
