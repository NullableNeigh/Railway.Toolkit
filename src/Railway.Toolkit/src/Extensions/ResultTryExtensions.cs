namespace Railway.Toolkit;

/// <summary>
/// Extension methods for wrapping exception-throwing code into Results.
/// Try operations convert imperative exception-based code into functional Result-based code.
/// </summary>
public static class ResultTryExtensions
{
    /// <summary>
    /// Executes a function and catches any exceptions, converting them to a Result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="errorCode">Optional error code to use when an exception is caught.</param>
    /// <returns>Ok with the function result, or Fail with the exception converted to an Error.</returns>
    public static Result<T> Try<T>(
        Func<T> func,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(func);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            try
            {
                T result = func();
                RailwayLogging.Logger?.LogOperation("Try", "succeeded", timer.Elapsed);
                return new Result<T>.Ok(result);
            }
            catch (Exception ex)
            {
                Error error = Error.FromException(ex, errorCode);
                RailwayLogging.Logger?.LogOperation("Try", "caught exception", timer.Elapsed, error);
                return new Result<T>.Fail(error);
            }
        }
    }

    /// <summary>
    /// Executes an asynchronous function and catches any exceptions, converting them to a Result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The async function to execute.</param>
    /// <param name="errorCode">Optional error code to use when an exception is caught.</param>
    /// <returns>A task containing Ok with the function result, or Fail with the exception.</returns>
    public static async Task<Result<T>> TryAsync<T>(
        Func<Task<T>> func,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(func);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            try
            {
                T result = await func().ConfigureAwait(false);
                RailwayLogging.Logger?.LogOperation("TryAsync", "succeeded", timer.Elapsed);
                return new Result<T>.Ok(result);
            }
            catch (Exception ex)
            {
                Error error = Error.FromException(ex, errorCode);
                RailwayLogging.Logger?.LogOperation("TryAsync", "caught exception", timer.Elapsed, error);
                return new Result<T>.Fail(error);
            }
        }
    }

    /// <summary>
    /// Wraps an action in a Result, returning Unit on success.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="errorCode">Optional error code to use when an exception is caught.</param>
    /// <returns>Ok with Unit if successful, or Fail with the exception converted to an Error.</returns>
    public static Result<Unit> Try(
        Action action,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            try
            {
                action();
                RailwayLogging.Logger?.LogOperation("Try", "succeeded", timer.Elapsed);
                return new Result<Unit>.Ok(Unit.Value);
            }
            catch (Exception ex)
            {
                Error error = Error.FromException(ex, errorCode);
                RailwayLogging.Logger?.LogOperation("Try", "caught exception", timer.Elapsed, error);
                return new Result<Unit>.Fail(error);
            }
        }
    }

    /// <summary>
    /// Wraps an asynchronous action in a Result, returning Unit on success.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="errorCode">Optional error code to use when an exception is caught.</param>
    /// <returns>A task containing Ok with Unit if successful, or Fail with the exception.</returns>
    public static async Task<Result<Unit>> TryAsync(
        Func<Task> action,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            await action().ConfigureAwait(false);
            return new Result<Unit>.Ok(Unit.Value);
        }
        catch (Exception ex)
        {
            return new Result<Unit>.Fail(Error.FromException(ex, errorCode));
        }
    }

    /// <summary>
    /// Applies a function to the success value and catches any exceptions.
    /// Combines Map with exception handling.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="mapper">The function to apply to the success value.</param>
    /// <param name="errorCode">Optional error code to use when an exception is caught.</param>
    /// <returns>Ok with the mapped value, or Fail with the error or exception.</returns>
    public static Result<TOut> TryMap<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (result is Result<TIn>.Ok ok)
        {
            try
            {
                return new Result<TOut>.Ok(mapper(ok.Value));
            }
            catch (Exception ex)
            {
                return new Result<TOut>.Fail(Error.FromException(ex, errorCode));
            }
        }

        Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
        return new Result<TOut>.Fail(fail.Error);
    }

    /// <summary>
    /// Applies an asynchronous function to the success value and catches any exceptions.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to map.</param>
    /// <param name="mapper">The async function to apply to the success value.</param>
    /// <param name="errorCode">Optional error code to use when an exception is caught.</param>
    /// <returns>A task containing Ok with the mapped value, or Fail with the error or exception.</returns>
    public static async Task<Result<TOut>> TryMapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapper,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        if (result is Result<TIn>.Ok ok)
        {
            try
            {
                TOut value = await mapper(ok.Value).ConfigureAwait(false);
                return new Result<TOut>.Ok(value);
            }
            catch (Exception ex)
            {
                return new Result<TOut>.Fail(Error.FromException(ex, errorCode));
            }
        }

        Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
        return new Result<TOut>.Fail(fail.Error);
    }

    /// <summary>
    /// Applies a binder function to the success value and catches any exceptions.
    /// Combines Bind with exception handling.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The function that returns a new Result.</param>
    /// <param name="errorCode">Optional error code to use when an exception is caught.</param>
    /// <returns>The result of the binder, or Fail with the error or exception.</returns>
    public static Result<TOut> TryBind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Result<TOut>> binder,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result is Result<TIn>.Ok ok)
        {
            try
            {
                return binder(ok.Value);
            }
            catch (Exception ex)
            {
                return new Result<TOut>.Fail(Error.FromException(ex, errorCode));
            }
        }

        Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
        return new Result<TOut>.Fail(fail.Error);
    }

    /// <summary>
    /// Applies an asynchronous binder function to the success value and catches any exceptions.
    /// </summary>
    /// <typeparam name="TIn">The input value type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The async function that returns a new Result.</param>
    /// <param name="errorCode">Optional error code to use when an exception is caught.</param>
    /// <returns>A task containing the result of the binder, or Fail with the error or exception.</returns>
    public static async Task<Result<TOut>> TryBindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> binder,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result is Result<TIn>.Ok ok)
        {
            try
            {
                return await binder(ok.Value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new Result<TOut>.Fail(Error.FromException(ex, errorCode));
            }
        }

        Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
        return new Result<TOut>.Fail(fail.Error);
    }
}
