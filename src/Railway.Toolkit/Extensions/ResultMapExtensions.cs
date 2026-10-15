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

    /// <summary>
    /// Transforms the success value using an asynchronous function.
    /// If the result is Fail, the error passes through unchanged.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="mapper">The async function to apply to the success value.</param>
    /// <param name="cancellationToken">The required governing token passed unchanged to the delegate.</param>
    /// <returns>A task containing the transformed result.</returns>
    /// <exception cref="ArgumentNullException">A required delegate or operation argument is null.</exception>
    /// <exception cref="OperationCanceledException">The governing token is requested at an observation point, or consumer work raises cancellation.</exception>
    /// <remarks>Argument validation precedes cancellation.
    /// Direct Result failures skip the delegate without observing cancellation. The token is checked before delegate invocation and after normal awaits.
    /// Every OperationCanceledException propagates.
    /// Propagated cancellation emits no Railway operation log. Delegates that ignore cancellation are not interrupted.</remarks>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, CancellationToken, Task<TOut>> mapper,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TOut mapped = await mapper(ok.Value, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
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
    /// <param name="cancellationToken">The required governing token passed unchanged to the delegate.</param>
    /// <returns>A task containing the transformed result.</returns>
    /// <exception cref="ArgumentNullException">A required delegate or operation argument is null.</exception>
    /// <exception cref="OperationCanceledException">The governing token is requested at an observation point, or consumer work raises cancellation.</exception>
    /// <remarks>Argument validation precedes cancellation. Incoming result tasks are awaited normally, then cancellation is checked before either track is processed.
    /// Direct Result failures skip the delegate without observing cancellation. The token is checked before delegate invocation and after normal awaits.
    /// Every OperationCanceledException propagates.
    /// Propagated cancellation emits no Railway operation log. Delegates that ignore cancellation are not interrupted.</remarks>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, CancellationToken, Task<TOut>> mapper,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        Result<TIn> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await result.MapAsync(mapper, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs a synchronous callback after awaiting a result.</summary>
    /// <typeparam name="TIn">The TIn type.</typeparam>
    /// <typeparam name="TOut">The TOut type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="mapper">The token-aware synchronous callback.</param>
    /// <param name="cancellationToken">The required governing token forwarded unchanged.</param>
    /// <returns>The operation completion and output.</returns>
    /// <remarks>Arguments are validated first. Incoming tasks are awaited normally, then cancellation is checked before
    /// either track. The same token is forwarded to the selected callback. Cancellation is checked before invocation
    /// and after normal completion, before output processing or logging. Exceptions propagate; completed effects are
    /// not rolled back. Cancellation emits no operation log. Synchronous work is not interrupted.</remarks>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, CancellationToken, TOut> mapper,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        Result<TIn> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TOut value = mapper(ok.Value, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            output = new Result<TOut>.Ok(value);
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }

        RailwayLogging.Logger?.LogOperation("Map", result, output, timer.GetElapsed());
        return output;
    }
}
