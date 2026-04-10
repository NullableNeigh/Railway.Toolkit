namespace Railway.Toolkit;

/// <summary>
/// Extension methods for transforming and handling errors in Results.
/// These operations work on the error track while leaving success values unchanged.
/// </summary>
public static class ResultErrorExtensions
{
    /// <summary>
    /// Transforms the error using the provided function.
    /// If the result is Ok, the success value passes through unchanged.
    /// Useful for adding context or converting error types.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">The function to apply to the error.</param>
    /// <returns>A new Result with the transformed error, or the original success value.</returns>
    public static Result<T> MapError<T>(
        this Result<T> result,
        Func<Error, Error> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return result.Match<Result<T>>(
            ok => ok.Value,
            fail => mapper(fail.Error)
        );
    }

    /// <summary>
    /// Asynchronously transforms the error using the provided function.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to transform.</param>
    /// <param name="mapper">The function to apply to the error.</param>
    /// <returns>A task containing the result with the transformed error.</returns>
    public static async Task<Result<T>> MapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Error> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        Result<T> result = await resultTask.ConfigureAwait(false);
        return result.MapError(mapper);
    }

    /// <summary>
    /// Transforms the error using an asynchronous function.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">The async function to apply to the error.</param>
    /// <returns>A task containing the result with the transformed error.</returns>
    public static async Task<Result<T>> MapErrorAsync<T>(
        this Result<T> result,
        Func<Error, Task<Error>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return await result.Match<Task<Result<T>>>(
            ok => Task.FromResult<Result<T>>(ok.Value),
            async fail => await mapper(fail.Error).ConfigureAwait(false)
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Transforms the error using an asynchronous function on an async result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to transform.</param>
    /// <param name="mapper">The async function to apply to the error.</param>
    /// <returns>A task containing the result with the transformed error.</returns>
    public static async Task<Result<T>> MapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task<Error>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        Result<T> result = await resultTask.ConfigureAwait(false);
        return await result.MapErrorAsync(mapper).ConfigureAwait(false);
    }

    /// <summary>
    /// Provides a fallback value if the result is a failure.
    /// Converts any failure into a success with the provided default value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to provide a default for.</param>
    /// <param name="defaultValue">The value to use if the result is a failure.</param>
    /// <returns>The original result if Ok, or a new Ok result with the default value if Fail.</returns>
    public static Result<T> OrElse<T>(
        this Result<T> result,
        T defaultValue)
    {
        return result.Match<Result<T>>(
            ok => ok.Value,
            fail => defaultValue
        );
    }

    /// <summary>
    /// Provides a fallback value using a function if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to provide a default for.</param>
    /// <param name="defaultValueFactory">The function to create the default value.</param>
    /// <returns>The original result if Ok, or a new Ok result with the computed default value if Fail.</returns>
    public static Result<T> OrElse<T>(
        this Result<T> result,
        Func<Error, T> defaultValueFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);

        return result.Match<Result<T>>(
            ok => ok.Value,
            fail => defaultValueFactory(fail.Error)
        );
    }

    /// <summary>
    /// Provides an alternative Result if the current result is a failure.
    /// Useful for fallback strategies or retry logic.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to provide an alternative for.</param>
    /// <param name="alternative">The alternative result to use if the current result is a failure.</param>
    /// <returns>The original result if Ok, or the alternative result if Fail.</returns>
    public static Result<T> OrElseWith<T>(
        this Result<T> result,
        Result<T> alternative)
    {
        return result.Match<Result<T>>(
            ok => ok.Value,
            fail => alternative
        );
    }

    /// <summary>
    /// Provides an alternative Result using a function if the current result is a failure.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to provide an alternative for.</param>
    /// <param name="alternativeFactory">The function to create the alternative result.</param>
    /// <returns>The original result if Ok, or the computed alternative result if Fail.</returns>
    public static Result<T> OrElseWith<T>(
        this Result<T> result,
        Func<Error, Result<T>> alternativeFactory)
    {
        ArgumentNullException.ThrowIfNull(alternativeFactory);

        return result.Match<Result<T>>(
            ok => ok.Value,
            fail => alternativeFactory(fail.Error)
        );
    }

    /// <summary>
    /// Recovers from a failure by providing a fallback value.
    /// Alias for OrElse.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to recover from.</param>
    /// <param name="fallbackValue">The value to use if the result is a failure.</param>
    /// <returns>The original result if Ok, or a new Ok result with the fallback value if Fail.</returns>
    public static Result<T> Recover<T>(
        this Result<T> result,
        T fallbackValue)
    {
        return result.OrElse(fallbackValue);
    }

    /// <summary>
    /// Recovers from a failure by providing a fallback value using a function.
    /// Alias for OrElse.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to recover from.</param>
    /// <param name="fallbackValueFactory">The function to create the fallback value.</param>
    /// <returns>The original result if Ok, or a new Ok result with the computed fallback value if Fail.</returns>
    public static Result<T> Recover<T>(
        this Result<T> result,
        Func<Error, T> fallbackValueFactory)
    {
        return result.OrElse(fallbackValueFactory);
    }

    /// <summary>
    /// Recovers from a failure by providing an alternative Result.
    /// Alias for OrElseWith.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to recover from.</param>
    /// <param name="alternative">The alternative result to use if the current result is a failure.</param>
    /// <returns>The original result if Ok, or the alternative result if Fail.</returns>
    public static Result<T> RecoverWith<T>(
        this Result<T> result,
        Result<T> alternative)
    {
        return result.OrElseWith(alternative);
    }

    /// <summary>
    /// Recovers from a failure by providing an alternative Result using a function.
    /// Alias for OrElseWith.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to recover from.</param>
    /// <param name="alternativeFactory">The function to create the alternative result.</param>
    /// <returns>The original result if Ok, or the computed alternative result if Fail.</returns>
    public static Result<T> RecoverWith<T>(
        this Result<T> result,
        Func<Error, Result<T>> alternativeFactory)
    {
        return result.OrElseWith(alternativeFactory);
    }
}
