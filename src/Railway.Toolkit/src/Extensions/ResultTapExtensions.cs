namespace Railway.Toolkit;

/// <summary>
/// Extension methods for performing side effects on Results without changing them.
/// Tap operations are useful for logging, debugging, or validation without breaking the chain.
/// </summary>
public static class ResultTapExtensions
{
    /// <summary>
    /// Executes an action on the success value without modifying the result.
    /// Useful for side effects like logging or validation.
    /// If the result is Fail, the action is not executed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The action to execute on the success value.</param>
    /// <returns>The original result unchanged.</returns>
    public static Result<T> Tap<T>(
        this Result<T> result,
        Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            if (result is Result<T>.Ok ok)
            {
                action(ok.Value);
                RailwayLogging.Logger?.LogOperation("Tap", "executed", timer.Elapsed);
            }
            else
            {
                RailwayLogging.Logger?.LogOperation("Tap", "skipped (on failure track)", timer.Elapsed);
            }

            return result;
        }
    }

    /// <summary>
    /// Executes an action on the error without modifying the result.
    /// Useful for error logging or handling without breaking the chain.
    /// If the result is Ok, the action is not executed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The action to execute on the error.</param>
    /// <returns>The original result unchanged.</returns>
    public static Result<T> TapError<T>(
        this Result<T> result,
        Action<Error> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            if (result is Result<T>.Fail fail)
            {
                action(fail.Error);
                RailwayLogging.Logger?.LogOperation("TapError", "executed", timer.Elapsed, fail.Error);
            }
            else
            {
                RailwayLogging.Logger?.LogOperation("TapError", "skipped (on success track)", timer.Elapsed);
            }

            return result;
        }
    }

    /// <summary>
    /// Asynchronously executes an action on the success value without modifying the result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to tap into.</param>
    /// <param name="action">The action to execute on the success value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        return result.Tap(action);
    }

    /// <summary>
    /// Executes an asynchronous action on the success value without modifying the result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The async action to execute on the success value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is Result<T>.Ok ok)
        {
            await action(ok.Value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Executes an asynchronous action on the success value of an async result without modifying it.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to tap into.</param>
    /// <param name="action">The async action to execute on the success value.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        return await result.TapAsync(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously executes an action on the error without modifying the result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to tap into.</param>
    /// <param name="action">The action to execute on the error.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T>> TapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Action<Error> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        return result.TapError(action);
    }

    /// <summary>
    /// Executes an asynchronous action on the error without modifying the result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The async action to execute on the error.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T>> TapErrorAsync<T>(
        this Result<T> result,
        Func<Error, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is Result<T>.Fail fail)
        {
            await action(fail.Error).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Executes an asynchronous action on the error of an async result without modifying it.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to tap into.</param>
    /// <param name="action">The async action to execute on the error.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T>> TapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        return await result.TapErrorAsync(action).ConfigureAwait(false);
    }
}
