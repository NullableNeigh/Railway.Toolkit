namespace Railway.Toolkit;

/// <summary>
/// Provides entry points for railway-oriented programming pipelines.
/// </summary>
public static class Railway
{
    /// <summary>
    /// Starts a railway pipeline with a value.
    /// Creates a successful Result containing the specified value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to start the pipeline with.</param>
    /// <returns>A Result in the Ok state with the given value.</returns>
    public static Result<T> Start<T>(T value)
    {
        return Result.Ok(value);
    }
}
