namespace Railway.Toolkit;

/// <summary>
/// Static factory methods for creating Result instances.
/// Provides a discoverable API for creating success and failure results.
/// </summary>
public static class Result
{
    /// <summary>
    /// Creates a successful Result with no value (Unit).
    /// Use this when an operation succeeds but has nothing to return.
    /// </summary>
    /// <returns>A Result in the Ok state with Unit value.</returns>
    public static Result<Unit> Ok()
    {
        return new Result<Unit>.Ok(Unit.Value);
    }

    /// <summary>
    /// Creates a successful Result containing the specified value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A Result in the Ok state with the given value.</returns>
    public static Result<T> Ok<T>(T value)
    {
        return new Result<T>.Ok(value);
    }

    /// <summary>
    /// Creates a failed Result containing the specified error.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A Result in the Fail state with the given error.</returns>
    public static Result<T> Fail<T>(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>.Fail(error);
    }

    /// <summary>
    /// Creates a failed Result with a message and error code.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="code">The error code.</param>
    /// <returns>A Result in the Fail state with the specified error.</returns>
    public static Result<T> Fail<T>(string message, string code)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(code);

        Error error = Error.Create(message, code);
        return new Result<T>.Fail(error);
    }

    /// <summary>
    /// Creates a Result from a nullable value.
    /// If the value is not null, returns Ok with the value.
    /// If the value is null, returns Fail with the specified error.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="error">The error to use if the value is null.</param>
    /// <returns>Ok if the value is not null, otherwise Fail with the error.</returns>
    public static Result<T> FromNullable<T>(T? value, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return value is not null
            ? new Result<T>.Ok(value)
            : new Result<T>.Fail(error);
    }

    /// <summary>
    /// Creates a Result from a nullable value.
    /// If the value is not null, returns Ok with the value.
    /// If the value is null, returns Fail with the specified message and code.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <param name="message">The error message if the value is null.</param>
    /// <param name="code">The error code if the value is null.</param>
    /// <returns>Ok if the value is not null, otherwise Fail with the error.</returns>
    public static Result<T> FromNullable<T>(T? value, string message, string code)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(code);

        return value is not null
            ? new Result<T>.Ok(value)
            : new Result<T>.Fail(Error.Create(message, code));
    }
}
