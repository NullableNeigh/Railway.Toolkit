namespace Railway.Toolkit;

/// <summary>
/// Extension methods for mapping (transforming) success values in Result types.
/// Map operations transform the value on the success track while leaving errors unchanged.
/// </summary>
public static class ResultMapExtensions
{
    /// <summary>
    /// Transforms the success value using the provided function.
    /// If the result is Fail, the error passes through unchanged.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">The function to apply to the success value.</param>
    /// <returns>A new Result with the transformed value, or the original error.</returns>
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return result.Match<Result<TOut>>(
            ok => mapper(ok.Value),
            fail => fail.Error
        );
    }

    /// <summary>
    /// Asynchronously transforms the success value using the provided function.
    /// If the result is Fail, the error passes through unchanged.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="resultTask">The task containing the result to transform.</param>
    /// <param name="mapper">The function to apply to the success value.</param>
    /// <returns>A task containing the transformed result.</returns>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, TOut> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var result = await resultTask.ConfigureAwait(false);
        return result.Map(mapper);
    }

    /// <summary>
    /// Transforms the success value using an asynchronous function.
    /// If the result is Fail, the error passes through unchanged.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">The async function to apply to the success value.</param>
    /// <returns>A task containing the transformed result.</returns>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return await result.Match<Task<Result<TOut>>>(
            async ok => await mapper(ok.Value).ConfigureAwait(false),
            fail => Task.FromResult<Result<TOut>>(fail.Error)
        ).ConfigureAwait(false);
    }

    /// <summary>
    /// Transforms the success value using an asynchronous function on an async result.
    /// If the result is Fail, the error passes through unchanged.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="resultTask">The task containing the result to transform.</param>
    /// <param name="mapper">The async function to apply to the success value.</param>
    /// <returns>A task containing the transformed result.</returns>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<TOut>> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var result = await resultTask.ConfigureAwait(false);
        return await result.MapAsync(mapper).ConfigureAwait(false);
    }
}
