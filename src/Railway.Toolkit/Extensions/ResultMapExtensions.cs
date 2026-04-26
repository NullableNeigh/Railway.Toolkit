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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            output = new Result<TOut>.Ok(mapper(ok.Value));
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("Map", result, output, timer.GetElapsed());
        return output;
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

        Result<TIn> result = await resultTask.ConfigureAwait(false);
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            TOut mapped = await mapper(ok.Value).ConfigureAwait(false);
            output = new Result<TOut>.Ok(mapped);
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("MapAsync", result, output, timer.GetElapsed());
        return output;
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

        Result<TIn> result = await resultTask.ConfigureAwait(false);
        return await result.MapAsync(mapper).ConfigureAwait(false);
    }
}
