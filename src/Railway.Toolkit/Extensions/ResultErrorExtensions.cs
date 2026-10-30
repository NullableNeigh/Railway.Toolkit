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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<T> output;

        if (result is Result<T>.Ok ok)
        {
            output = new Result<T>.Ok(ok.Value);
        }
        else
        {
            Result<T>.Fail fail = (Result<T>.Fail)result;
            output = new Result<T>.Fail(RailwayContracts.Output(mapper(fail.Error)));
        }

        RailwayLogging.Logger?.LogOperation("MapError", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
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
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);

        Result<T> result = await resultTask.ConfigureAwait(false);
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<T> output;

        if (result is Result<T>.Ok ok)
        {
            output = new Result<T>.Ok(ok.Value);
        }
        else
        {
            Result<T>.Fail fail = (Result<T>.Fail)result;
            output = new Result<T>.Fail(RailwayContracts.Output(mapper(fail.Error)));
        }

        RailwayLogging.Logger?.LogOperation("MapErrorAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<T> output;

        if (result is Result<T>.Ok ok)
        {
            output = new Result<T>.Ok(ok.Value);
        }
        else
        {
            Result<T>.Fail fail = (Result<T>.Fail)result;
            output = new Result<T>.Fail(RailwayContracts.Output(await RailwayContracts.Output(mapper(fail.Error)).ConfigureAwait(false)));
        }

        RailwayLogging.Logger?.LogOperation("MapErrorAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
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
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);

        Result<T> result = await resultTask.ConfigureAwait(false);
        result = RailwayContracts.Output(result);
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
        ArgumentNullException.ThrowIfNull(result);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<T> output = result is Result<T>.Ok ok
            ? new Result<T>.Ok(ok.Value)
            : new Result<T>.Ok(defaultValue);

        RailwayLogging.Logger?.LogOperation("OrElse", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(defaultValueFactory);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<T> output;

        if (result is Result<T>.Ok ok)
        {
            output = new Result<T>.Ok(ok.Value);
        }
        else
        {
            Result<T>.Fail fail = (Result<T>.Fail)result;
            output = new Result<T>.Ok(defaultValueFactory(fail.Error));
        }

        RailwayLogging.Logger?.LogOperation("OrElse", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(alternative);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<T> output = result is Result<T>.Ok ok
            ? new Result<T>.Ok(ok.Value)
            : alternative;

        RailwayLogging.Logger?.LogOperation("OrElseWith", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
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
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(alternativeFactory);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<T> output;

        if (result is Result<T>.Ok ok)
        {
            output = new Result<T>.Ok(ok.Value);
        }
        else
        {
            Result<T>.Fail fail = (Result<T>.Fail)result;
            output = RailwayContracts.Output(alternativeFactory(fail.Error));
        }

        RailwayLogging.Logger?.LogOperation("OrElseWith", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Runs cancellable asynchronous failure-track work.</summary>
    /// <typeparam name="T">The T type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="mapper">The error callback.</param>
    /// <param name="cancellationToken">The required governing token forwarded unchanged.</param>
    /// <returns>The operation completion and output.</returns>
    /// <remarks>Arguments are validated first. Incoming tasks are awaited normally, then cancellation is checked before
    /// either track. The same token is forwarded to the selected callback. Cancellation is checked before invocation
    /// and after normal completion, before output processing or logging. Exceptions propagate; completed effects are
    /// not rolled back. Cancellation emits no operation log. Direct Ok inputs bypass cancellation and the
    /// callback.</remarks>
    public static async Task<Result<T>> MapErrorAsync<T>(
        this Result<T> result,
        Func<Error, CancellationToken, Task<Error>> mapper,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Error error = await RailwayContracts.Output(mapper(fail.Error, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            error = RailwayContracts.Output(error);
            output = new Result<T>.Fail(error);
        }

        RailwayLogging.Logger?.LogOperation("MapErrorAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Runs cancellable asynchronous failure-track work.</summary>
    /// <typeparam name="T">The T type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="mapper">The error callback.</param>
    /// <param name="cancellationToken">The required governing token forwarded unchanged.</param>
    /// <returns>The operation completion and output.</returns>
    /// <remarks>Arguments are validated first. Incoming tasks are awaited normally, then cancellation is checked before
    /// either track. The same token is forwarded to the selected callback. Cancellation is checked before invocation
    /// and after normal completion, before output processing or logging. Exceptions propagate; completed effects are
    /// not rolled back. Cancellation emits no operation log. Direct Ok inputs bypass cancellation and the
    /// callback.</remarks>
    public static async Task<Result<T>> MapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, CancellationToken, Task<Error>> mapper,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);

        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        return await result.MapErrorAsync(mapper, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs a synchronous callback after awaiting a result.</summary>
    /// <typeparam name="T">The T type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="mapper">The token-aware synchronous callback.</param>
    /// <param name="cancellationToken">The required governing token forwarded unchanged.</param>
    /// <returns>The operation completion and output.</returns>
    /// <remarks>Arguments are validated first. Incoming tasks are awaited normally, then cancellation is checked before
    /// either track. The same token is forwarded to the selected callback. Cancellation is checked before invocation
    /// and after normal completion, before output processing or logging. Exceptions propagate; completed effects are
    /// not rolled back. Cancellation emits no operation log. Synchronous work is not interrupted.</remarks>
    public static async Task<Result<T>> MapErrorAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, CancellationToken, Error> mapper,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);

        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Error error = mapper(fail.Error, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            error = RailwayContracts.Output(error);
            output = new Result<T>.Fail(error);
        }

        RailwayLogging.Logger?.LogOperation("MapErrorAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides a successful fallback value on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="defaultValue">The fallback, used only on failure.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Successful inputs skip the callback.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseAsync<T>(
        this Task<Result<T>> resultTask,
        T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            T fallback = defaultValue;
            output = new Result<T>.Ok(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides a successful fallback value on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="defaultValueFactory">The fallback, used only on failure.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Successful inputs skip the callback.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, T> defaultValueFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            T fallback = defaultValueFactory(fail.Error);
            output = new Result<T>.Ok(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides a successful fallback value on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="defaultValueFactory">The fallback, used only on failure.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Successful inputs skip the callback.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseAsync<T>(
        this Result<T> result,
        Func<Error, Task<T>> defaultValueFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);
        ArgumentNullException.ThrowIfNull(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            T fallback = await RailwayContracts.Output(defaultValueFactory(fail.Error)).ConfigureAwait(false);
            output = new Result<T>.Ok(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides a successful fallback value on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="defaultValueFactory">The fallback, used only on failure.</param>
    /// <param name="cancellationToken">The governing token, forwarded unchanged.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Cancellation is checked after incoming awaits and before/after selected callbacks; direct successes bypass it.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseAsync<T>(
        this Result<T> result,
        Func<Error, CancellationToken, Task<T>> defaultValueFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);
        ArgumentNullException.ThrowIfNull(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            T fallback = await RailwayContracts.Output(defaultValueFactory(fail.Error, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            output = new Result<T>.Ok(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides a successful fallback value on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="defaultValueFactory">The fallback, used only on failure.</param>
    /// <param name="cancellationToken">The governing token, forwarded unchanged.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Cancellation is checked after incoming awaits and before/after selected callbacks; direct successes bypass it.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, CancellationToken, Task<T>> defaultValueFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            T fallback = await RailwayContracts.Output(defaultValueFactory(fail.Error, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            output = new Result<T>.Ok(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides a successful fallback value on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="defaultValueFactory">The fallback, used only on failure.</param>
    /// <param name="cancellationToken">The governing token, forwarded unchanged.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Cancellation is checked after incoming awaits and before/after selected callbacks; direct successes bypass it.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, CancellationToken, T> defaultValueFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            T fallback = defaultValueFactory(fail.Error, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            output = new Result<T>.Ok(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides an alternative result on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="alternative">The fallback, used only on failure.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Successful inputs skip the callback.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseWithAsync<T>(
        this Task<Result<T>> resultTask,
        Result<T> alternative)
    {
        ArgumentNullException.ThrowIfNull(alternative);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            Result<T> fallback = alternative;
            output = RailwayContracts.Output(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseWithAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides an alternative result on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="alternativeFactory">The fallback, used only on failure.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Successful inputs skip the callback.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseWithAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Result<T>> alternativeFactory)
    {
        ArgumentNullException.ThrowIfNull(alternativeFactory);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            Result<T> fallback = alternativeFactory(fail.Error);
            output = RailwayContracts.Output(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseWithAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides an alternative result on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="alternativeFactory">The fallback, used only on failure.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Successful inputs skip the callback.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseWithAsync<T>(
        this Result<T> result,
        Func<Error, Task<Result<T>>> alternativeFactory)
    {
        ArgumentNullException.ThrowIfNull(alternativeFactory);
        ArgumentNullException.ThrowIfNull(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            Result<T> fallback = await RailwayContracts.Output(alternativeFactory(fail.Error)).ConfigureAwait(false);

            output = RailwayContracts.Output(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseWithAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides an alternative result on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="alternativeFactory">The fallback, used only on failure.</param>
    /// <param name="cancellationToken">The governing token, forwarded unchanged.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Cancellation is checked after incoming awaits and before/after selected callbacks; direct successes bypass it.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseWithAsync<T>(
        this Result<T> result,
        Func<Error, CancellationToken, Task<Result<T>>> alternativeFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alternativeFactory);
        ArgumentNullException.ThrowIfNull(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Result<T> fallback = await RailwayContracts.Output(alternativeFactory(fail.Error, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            output = RailwayContracts.Output(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseWithAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides an alternative result on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="alternativeFactory">The fallback, used only on failure.</param>
    /// <param name="cancellationToken">The governing token, forwarded unchanged.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Cancellation is checked after incoming awaits and before/after selected callbacks; direct successes bypass it.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseWithAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, CancellationToken, Task<Result<T>>> alternativeFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alternativeFactory);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Result<T> fallback = await RailwayContracts.Output(alternativeFactory(fail.Error, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            output = RailwayContracts.Output(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseWithAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Provides an alternative result on the failure track.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="resultTask">The incoming result task.</param>
    /// <param name="alternativeFactory">The fallback, used only on failure.</param>
    /// <param name="cancellationToken">The governing token, forwarded unchanged.</param>
    /// <returns>The original success or fallback result.</returns>
    /// <remarks>Incoming task faults and callback exceptions propagate. Task inputs are awaited normally.
    /// Required arguments are validated before work. Cancellation is checked after incoming awaits and before/after selected callbacks; direct successes bypass it.
    /// Null result outputs are programming errors. Nullable success values are permitted.</remarks>
    public static async Task<Result<T>> OrElseWithAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, CancellationToken, Result<T>> alternativeFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alternativeFactory);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Result<T> fallback = alternativeFactory(fail.Error, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            output = RailwayContracts.Output(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseWithAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Awaits the incoming task, then invokes the asynchronous fallback only on failure.</summary>
    /// <remarks>Incoming faults propagate. Null callback tasks/results are programming errors; nullable values are allowed.</remarks>
    public static async Task<Result<T>> OrElseAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task<T>> defaultValueFactory)
    {
        ArgumentNullException.ThrowIfNull(defaultValueFactory);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            T fallback = await RailwayContracts.Output(defaultValueFactory(fail.Error)).ConfigureAwait(false);
            output = new Result<T>.Ok(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

    /// <summary>Awaits the incoming task, then invokes the asynchronous fallback only on failure.</summary>
    /// <remarks>Incoming faults propagate. Null callback tasks/results are programming errors; nullable values are allowed.</remarks>
    public static async Task<Result<T>> OrElseWithAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Error, Task<Result<T>>> alternativeFactory)
    {
        ArgumentNullException.ThrowIfNull(alternativeFactory);
        ArgumentNullException.ThrowIfNull(resultTask);
        Result<T> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = result;
        if (result is Result<T>.Fail fail)
        {
            Result<T> fallback = await RailwayContracts.Output(alternativeFactory(fail.Error)).ConfigureAwait(false);

            output = RailwayContracts.Output(fallback);
        }
        RailwayLogging.Logger?.LogOperation("OrElseWithAsync", result, output, timer.GetElapsed(), executed: result is Result<T>.Fail);
        return output;
    }

}
