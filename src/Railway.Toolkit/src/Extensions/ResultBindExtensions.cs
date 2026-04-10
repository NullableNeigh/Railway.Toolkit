namespace Railway.Toolkit;

/// <summary>
/// Extension methods for binding (chaining) operations that return Results.
/// Bind is the core of railway-oriented programming, enabling composition of fallible operations.
/// </summary>
public static class ResultBindExtensions
{
    /// <summary>
    /// Chains an operation that returns a Result.
    /// If the input is Ok, executes the binder function. If Fail, passes the error through.
    /// This is the fundamental operation for composing railway-oriented functions.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The function that returns a new Result.</param>
    /// <returns>The result of the binder function, or the original error.</returns>
    public static Result<TOut> Bind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            Result<TOut> output = result.Match<Result<TOut>>(
                ok =>
                {
                    Result<TOut> bound = binder(ok.Value);

                    bound.Match(
                        okBound => RailwayLogging.Logger?.LogOperation("Bind", "executed (stayed on success track)", timer.Elapsed),
                        failBound => RailwayLogging.Logger?.LogOperation("Bind", "failed (switched to failure track)", timer.Elapsed, failBound.Error)
                    );

                    return bound;
                },
                fail =>
                {
                    RailwayLogging.Logger?.LogOperation("Bind", "skipped (on failure track)", timer.Elapsed);
                    return fail.Error;
                }
            );

            return output;
        }
    }

    /// <summary>
    /// Asynchronously chains an operation that returns a Result.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="resultTask">The task containing the result to bind.</param>
    /// <param name="binder">The function that returns a new Result.</param>
    /// <returns>A task containing the bound result.</returns>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        Result<TIn> result = await resultTask.ConfigureAwait(false);
        return result.Bind(binder); // Logging happens in Bind
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a Result.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The async function that returns a new Result.</param>
    /// <returns>A task containing the bound result.</returns>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            Result<TOut> output = await result.Match<Task<Result<TOut>>>(
                async ok =>
                {
                    Result<TOut> bound = await binder(ok.Value).ConfigureAwait(false);

                    bound.Match(
                        okBound => RailwayLogging.Logger?.LogOperation("BindAsync", "executed (stayed on success track)", timer.Elapsed),
                        failBound => RailwayLogging.Logger?.LogOperation("BindAsync", "failed (switched to failure track)", timer.Elapsed, failBound.Error)
                    );

                    return bound;
                },
                fail =>
                {
                    RailwayLogging.Logger?.LogOperation("BindAsync", "skipped (on failure track)", timer.Elapsed);
                    return Task.FromResult<Result<TOut>>(fail.Error);
                }
            ).ConfigureAwait(false);

            return output;
        }
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a Result on an async result.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="resultTask">The task containing the result to bind.</param>
    /// <param name="binder">The async function that returns a new Result.</param>
    /// <returns>A task containing the bound result.</returns>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        Result<TIn> result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder).ConfigureAwait(false); // Logging happens in BindAsync
    }
}
