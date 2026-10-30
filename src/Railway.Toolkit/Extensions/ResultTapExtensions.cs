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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Ok ok)
        {
            action(ok.Value);
        }

        RailwayLogging.Logger?.LogOperation("Tap", result, result, timer.GetElapsed());
        return result;
    }

    /// <summary>
    /// Executes an action if the result is Ok, without receiving the value.
    /// Useful when the side effect does not need the success value.
    /// If the result is Fail, the action is not executed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result unchanged.</returns>
    public static Result<T> Tap<T>(
        this Result<T> result,
        Action action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Ok)
        {
            action();
        }

        RailwayLogging.Logger?.LogOperation("Tap", result, result, timer.GetElapsed());
        return result;
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Fail fail)
        {
            action(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("TapError", result, result, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return result;
    }

    /// <summary>
    /// Executes an action if the result is Fail, without receiving the error.
    /// Useful when the side effect does not need the error details.
    /// If the result is Ok, the action is not executed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result unchanged.</returns>
    public static Result<T> TapError<T>(
        this Result<T> result,
        Action action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Fail)
        {
            action();
        }

        RailwayLogging.Logger?.LogOperation("TapError", result, result, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return result;
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
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Ok ok)
        {
            action(ok.Value);
        }

        RailwayLogging.Logger?.LogOperation("TapAsync", result, result, timer.GetElapsed());
        return result;
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Ok ok)
        {
            await RailwayContracts.Output(action(ok.Value)).ConfigureAwait(false);
        }

        RailwayLogging.Logger?.LogOperation("TapAsync", result, result, timer.GetElapsed());
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
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        result = RailwayContracts.Output(result);
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
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Fail fail)
        {
            action(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("TapErrorAsync", result, result, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return result;
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Fail fail)
        {
            await RailwayContracts.Output(action(fail.Error)).ConfigureAwait(false);
        }

        RailwayLogging.Logger?.LogOperation("TapErrorAsync", result, result, timer.GetElapsed(), executed: result is Result<T>.Fail);
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
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        result = RailwayContracts.Output(result);
        return await result.TapErrorAsync(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an action regardless of whether the result is Ok or Fail.
    /// The result passes through unchanged.
    /// Useful for cleanup, audit logging, or metrics that must always run.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>The original result unchanged.</returns>
    public static Result<T> Finally<T>(
        this Result<T> result,
        Action action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        action();

        RailwayLogging.Logger?.LogOperation("Finally", result, result, timer.GetElapsed(), executed: true);
        return result;
    }

    /// <summary>
    /// Executes an action with access to the full result regardless of track.
    /// The result passes through unchanged.
    /// Useful when the side effect needs to inspect whether the operation succeeded or failed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The action to execute, receiving the full result.</param>
    /// <returns>The original result unchanged.</returns>
    public static Result<T> Finally<T>(
        this Result<T> result,
        Action<Result<T>> action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        action(result);

        RailwayLogging.Logger?.LogOperation("Finally", result, result, timer.GetElapsed(), executed: true);
        return result;
    }

    /// <summary>
    /// Executes an async action regardless of track on a sync result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The async action to execute.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    public static async Task<Result<T>> FinallyAsync<T>(
        this Result<T> result,
        Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        await RailwayContracts.Output(action()).ConfigureAwait(false);

        RailwayLogging.Logger?.LogOperation("FinallyAsync", result, result, timer.GetElapsed(), executed: true);
        return result;
    }

    /// <summary>
    /// Always executes a sync action after an incoming task, including faults and cancellation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to tap into.</param>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <remarks>Cleanup runs once even when the incoming task throws or cancels. Request cancellation cannot skip cleanup.
    /// Cleanup is tokenless and awaited normally. Its exception takes precedence over an incoming exception or cancellation, as in C# finally.
    /// Otherwise the original result, exception, or cancellation is preserved. No output-result log is emitted for faults or cancellation.</remarks>
    public static async Task<Result<T>> FinallyAsync<T>(
        this Task<Result<T>> resultTask,
        Action action)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T>? result = null;
        bool completed = false;
        try
        {
            result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
            completed = true;
        }
        finally
        {
            using IRailwayTimer timer = RailwayLogging.StartOperation();
            action();
            if (completed)
                RailwayLogging.Logger?.LogOperation("FinallyAsync", result!, result!, timer.GetElapsed(), executed: true);
        }
        return result!;
    }

    /// <summary>
    /// Always executes and awaits cleanup after an incoming task, including faults and cancellation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to tap into.</param>
    /// <param name="action">The async action to execute.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <remarks>Cleanup runs once even when the incoming task throws or cancels. Request cancellation cannot skip cleanup.
    /// Cleanup is tokenless and awaited normally. Its exception takes precedence over an incoming exception or cancellation, as in C# finally.
    /// Otherwise the original result, exception, or cancellation is preserved. No output-result log is emitted for faults or cancellation.</remarks>
    public static async Task<Result<T>> FinallyAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T>? result = null;
        bool completed = false;
        try
        {
            result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
            completed = true;
        }
        finally
        {
            using IRailwayTimer timer = RailwayLogging.StartOperation();
            await RailwayContracts.Output(action()).ConfigureAwait(false);
            if (completed)
                RailwayLogging.Logger?.LogOperation("FinallyAsync", result!, result!, timer.GetElapsed(), executed: true);
        }
        return result!;
    }

    /// <summary>
    /// Executes an asynchronous action on the success value without modifying the result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to tap into.</param>
    /// <param name="action">The async action to execute on the success value.</param>
    /// <param name="cancellationToken">The required governing token passed unchanged to the delegate.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <exception cref="ArgumentNullException">A required delegate or operation argument is null.</exception>
    /// <exception cref="OperationCanceledException">The governing token is requested at an observation point, or consumer work raises cancellation.</exception>
    /// <remarks>Argument validation precedes cancellation.
    /// Direct Result failures skip the delegate without observing cancellation. The token is checked before delegate invocation and after normal awaits.
    /// Every OperationCanceledException propagates.
    /// Propagated cancellation emits no Railway operation log. Delegates that ignore cancellation are not interrupted.</remarks>
    public static async Task<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        if (result is Result<T>.Ok ok)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RailwayContracts.Output(action(ok.Value, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        RailwayLogging.Logger?.LogOperation("TapAsync", result, result, timer.GetElapsed());
        return result;
    }

    /// <summary>
    /// Executes an asynchronous action on the success value of an async result without modifying it.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to tap into.</param>
    /// <param name="action">The async action to execute on the success value.</param>
    /// <param name="cancellationToken">The required governing token passed unchanged to the delegate.</param>
    /// <returns>A task containing the original result unchanged.</returns>
    /// <exception cref="ArgumentNullException">A required delegate or operation argument is null.</exception>
    /// <exception cref="OperationCanceledException">The governing token is requested at an observation point, or consumer work raises cancellation.</exception>
    /// <remarks>Argument validation precedes cancellation. Incoming result tasks are awaited normally, then cancellation is checked before either track is processed.
    /// Direct Result failures skip the delegate without observing cancellation. The token is checked before delegate invocation and after normal awaits.
    /// Every OperationCanceledException propagates.
    /// Propagated cancellation emits no Railway operation log. Delegates that ignore cancellation are not interrupted.</remarks>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        return await result.TapAsync(action, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs cancellable asynchronous failure-track work.</summary>
    /// <typeparam name="T">The T type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="action">The error callback.</param>
    /// <param name="cancellationToken">The required governing token forwarded unchanged.</param>
    /// <returns>The operation completion and output.</returns>
    /// <remarks>Arguments are validated first. Incoming tasks are awaited normally, then cancellation is checked before
    /// either track. The same token is forwarded to the selected callback. Cancellation is checked before invocation
    /// and after normal completion, before output processing or logging. Exceptions propagate; completed effects are
    /// not rolled back. Cancellation emits no operation log. Direct Ok inputs bypass cancellation and the
    /// callback.</remarks>
    public static async Task<Result<T>> TapErrorAsync<T>(
        this Result<T> result,
        Func<Error, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RailwayContracts.Output(action(fail.Error, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        RailwayLogging.Logger?.LogOperation("TapErrorAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Runs cancellable asynchronous failure-track work.</summary>
    /// <typeparam name="T">The T type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="action">The error callback.</param>
    /// <param name="cancellationToken">The required governing token forwarded unchanged.</param>
    /// <returns>The operation completion and output.</returns>
    /// <remarks>Arguments are validated first. Incoming tasks are awaited normally, then cancellation is checked before
    /// either track. The same token is forwarded to the selected callback. Cancellation is checked before invocation
    /// and after normal completion, before output processing or logging. Exceptions propagate; completed effects are
    /// not rolled back. Cancellation emits no operation log. Direct Ok inputs bypass cancellation and the
    /// callback.</remarks>
    public static async Task<Result<T>> TapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        return await result.TapErrorAsync(action, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs a synchronous callback after awaiting a result.</summary>
    /// <typeparam name="T">The T type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="action">The token-aware synchronous callback.</param>
    /// <param name="cancellationToken">The required governing token forwarded unchanged.</param>
    /// <returns>The operation completion and output.</returns>
    /// <remarks>Arguments are validated first. Incoming tasks are awaited normally, then cancellation is checked before
    /// either track. The same token is forwarded to the selected callback. Cancellation is checked before invocation
    /// and after normal completion, before output processing or logging. Exceptions propagate; completed effects are
    /// not rolled back. Cancellation emits no operation log. Synchronous work is not interrupted.</remarks>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Action<T, CancellationToken> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Ok ok)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action(ok.Value, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        RailwayLogging.Logger?.LogOperation("TapAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs a synchronous callback after awaiting a result.</summary>
    /// <typeparam name="T">The T type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="action">The token-aware synchronous callback.</param>
    /// <param name="cancellationToken">The required governing token forwarded unchanged.</param>
    /// <returns>The operation completion and output.</returns>
    /// <remarks>Arguments are validated first. Incoming tasks are awaited normally, then cancellation is checked before
    /// either track. The same token is forwarded to the selected callback. Cancellation is checked before invocation
    /// and after normal completion, before output processing or logging. Exceptions propagate; completed effects are
    /// not rolled back. Cancellation emits no operation log. Synchronous work is not interrupted.</remarks>
    public static async Task<Result<T>> TapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Action<Error, CancellationToken> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action(fail.Error, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        RailwayLogging.Logger?.LogOperation("TapErrorAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }
}
