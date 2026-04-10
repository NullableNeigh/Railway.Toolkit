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

        return result.Match<Result<TOut>>(
            ok => binder(ok.Value),
            fail => fail.Error
        );
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

        return await result.Match<Task<Result<TOut>>>(
            async ok => await binder(ok.Value).ConfigureAwait(false),
            fail => Task.FromResult<Result<TOut>>(fail.Error)
        ).ConfigureAwait(false);
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
}
