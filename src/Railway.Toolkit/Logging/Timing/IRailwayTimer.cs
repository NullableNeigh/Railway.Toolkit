namespace Railway.Toolkit;

/// <summary>
/// Abstraction for measuring operation duration.
/// Implementations provide different precision/overhead trade-offs.
/// </summary>
internal interface IRailwayTimer : IDisposable
{
    /// <summary>
    /// Returns the elapsed time since this timer was created.
    /// </summary>
    TimeSpan GetElapsed();
}
