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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            output = binder(ok.Value);
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("Bind", result, output, timer.GetElapsed());
        return output;
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
        return result.Bind(binder);
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            output = await binder(ok.Value).ConfigureAwait(false);
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("BindAsync", result, output, timer.GetElapsed());
        return output;
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
        return await result.BindAsync(binder).ConfigureAwait(false);
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a Result.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The async function that returns a new Result.</param>
    /// <param name="cancellationToken">The required governing token passed unchanged to the delegate.</param>
    /// <returns>A task containing the bound result.</returns>
    /// <exception cref="ArgumentNullException">A required delegate or operation argument is null.</exception>
    /// <exception cref="OperationCanceledException">The governing token is requested at an observation point, or consumer work raises cancellation.</exception>
    /// <remarks>Argument validation precedes cancellation.
    /// Existing failures skip the delegate without observing cancellation. The token is checked before delegate invocation.
    /// Every OperationCanceledException propagates.
    /// Propagated cancellation emits no Railway operation log. Delegates that ignore cancellation are not interrupted.</remarks>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, CancellationToken, Task<Result<TOut>>> binder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binder);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = await binder(ok.Value, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("BindAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Chains an asynchronous operation that returns a Result on an async result.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="resultTask">The task containing the result to bind.</param>
    /// <param name="binder">The async function that returns a new Result.</param>
    /// <param name="cancellationToken">The required governing token passed unchanged to the delegate.</param>
    /// <returns>A task containing the bound result.</returns>
    /// <exception cref="ArgumentNullException">A required delegate or operation argument is null.</exception>
    /// <exception cref="OperationCanceledException">The governing token is requested at an observation point, or consumer work raises cancellation.</exception>
    /// <remarks>Argument validation precedes cancellation. Incoming result tasks are awaited normally.
    /// Existing failures skip the delegate without observing cancellation. The token is checked before delegate invocation.
    /// Every OperationCanceledException propagates.
    /// Propagated cancellation emits no Railway operation log. Delegates that ignore cancellation are not interrupted.</remarks>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, CancellationToken, Task<Result<TOut>>> binder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binder);

        Result<TIn> result = await resultTask.ConfigureAwait(false);
        return await result.BindAsync(binder, cancellationToken).ConfigureAwait(false);
    }
}
