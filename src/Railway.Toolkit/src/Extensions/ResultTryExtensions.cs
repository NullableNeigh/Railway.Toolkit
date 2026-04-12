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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        // Static Try has no Result input — use Unit to represent "starting fresh"
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<T> output;

        try
        {
            output = new Result<T>.Ok(func());
        }
        catch (Exception ex)
        {
            output = new Result<T>.Fail(Error.FromException(ex, errorCode));
        }

        RailwayLogging.Logger?.LogOperation("Try", input, output, timer.GetElapsed());
        return output;
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<T> output;

        try
        {
            output = new Result<T>.Ok(await func().ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            output = new Result<T>.Fail(Error.FromException(ex, errorCode));
        }

        RailwayLogging.Logger?.LogOperation("TryAsync", input, output, timer.GetElapsed());
        return output;
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<Unit> output;

        try
        {
            action();
            output = new Result<Unit>.Ok(Unit.Value);
        }
        catch (Exception ex)
        {
            output = new Result<Unit>.Fail(Error.FromException(ex, errorCode));
        }

        RailwayLogging.Logger?.LogOperation("Try", input, output, timer.GetElapsed());
        return output;
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<Unit> output;

        try
        {
            await action().ConfigureAwait(false);
            output = new Result<Unit>.Ok(Unit.Value);
        }
        catch (Exception ex)
        {
            output = new Result<Unit>.Fail(Error.FromException(ex, errorCode));
        }

        RailwayLogging.Logger?.LogOperation("TryAsync", input, output, timer.GetElapsed());
        return output;
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            try
            {
                output = new Result<TOut>.Ok(mapper(ok.Value));
            }
            catch (Exception ex)
            {
                output = new Result<TOut>.Fail(Error.FromException(ex, errorCode));
            }
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("TryMap", result, output, timer.GetElapsed());
        return output;
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            try
            {
                output = new Result<TOut>.Ok(await mapper(ok.Value).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                output = new Result<TOut>.Fail(Error.FromException(ex, errorCode));
            }
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("TryMapAsync", result, output, timer.GetElapsed());
        return output;
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            try
            {
                output = binder(ok.Value);
            }
            catch (Exception ex)
            {
                output = new Result<TOut>.Fail(Error.FromException(ex, errorCode));
            }
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("TryBind", result, output, timer.GetElapsed());
        return output;
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<TOut> output;

        if (result is Result<TIn>.Ok ok)
        {
            try
            {
                output = await binder(ok.Value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                output = new Result<TOut>.Fail(Error.FromException(ex, errorCode));
            }
        }
        else
        {
            Result<TIn>.Fail fail = (Result<TIn>.Fail)result;
            output = new Result<TOut>.Fail(fail.Error);
        }

        RailwayLogging.Logger?.LogOperation("TryBindAsync", result, output, timer.GetElapsed());
        return output;
    }
}
