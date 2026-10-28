namespace Railway.Toolkit;

/// <summary>Converts exceptions from the supplied operation into errors. Incoming task faults propagate.</summary>
public static class ResultTryExtensions
{
    /// <summary>Runs the supplied function operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="T">The T value type.</typeparam>
    /// <param name="func">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static Result<T> Try<T>(
        Func<T> func,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(func);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<T> output;
        Exception? caught = null;
        T returned = default!;
        try
        {
            returned = func();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            caught = ex;
        }
        if (caught is not null)
        {
            Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
            output = new Result<T>.Fail(error);
        }
        else
        {
            output = new Result<T>.Ok(returned);
        }
        RailwayLogging.Logger?.LogOperation("Try", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied function operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="T">The T value type.</typeparam>
    /// <param name="func">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static async Task<Result<T>> TryAsync<T>(
        Func<Task<T>> func,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(func);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<T> output;
        Exception? caught = null;
        Task<T>? returned = default;
        try
        {
            returned = func();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            caught = ex;
        }
        T value = default!;
        if (caught is null)
        {
            Task<T> work = RailwayContracts.Output(returned);
            try
            {
                value = await work.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
        }
        if (caught is not null)
        {
            Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
            output = new Result<T>.Fail(error);
        }
        else
        {
            output = new Result<T>.Ok(value);
        }
        RailwayLogging.Logger?.LogOperation("TryAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied function operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="T">The T value type.</typeparam>
    /// <param name="func">The callback to invoke on the selected track.</param>
    /// <param name="cancellationToken">The governing cancellation token.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// Cancellation exceptions propagate when the governing token is requested; otherwise they are mapped. Cancellation is checked before/after work and after incoming awaits.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static async Task<Result<T>> TryAsync<T>(
        Func<CancellationToken, Task<T>> func,
        CancellationToken cancellationToken,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(func);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<T> output;
        Exception? caught = null;
        Task<T>? returned = default;
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            returned = func(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            caught = ex;
        }
        T value = default!;
        if (caught is null)
        {
            Task<T> work = RailwayContracts.Output(returned);
            try
            {
                value = await work.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                caught = ex;
            }
        }
        if (caught is not null)
        {
            Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
            output = new Result<T>.Fail(error);
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = new Result<T>.Ok(value);
        }
        RailwayLogging.Logger?.LogOperation("TryAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied action operation, converting eligible callback exceptions to errors.</summary>
    /// <param name="action">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static Result<Unit> Try(
        Action action,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<Unit> output;
        Exception? caught = null;
        try
        {
            action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            caught = ex;
        }
        if (caught is not null)
        {
            Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
            output = new Result<Unit>.Fail(error);
        }
        else
        {
            output = new Result<Unit>.Ok(Unit.Value);
        }
        RailwayLogging.Logger?.LogOperation("Try", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied action operation, converting eligible callback exceptions to errors.</summary>
    /// <param name="action">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static async Task<Result<Unit>> TryAsync(
        Func<Task> action,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<Unit> output;
        Exception? caught = null;
        Task? returned = default;
        try
        {
            returned = action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            caught = ex;
        }
        if (caught is null)
        {
            Task work = RailwayContracts.Output(returned);
            try
            {
                await work.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
        }
        if (caught is not null)
        {
            Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
            output = new Result<Unit>.Fail(error);
        }
        else
        {
            output = new Result<Unit>.Ok(Unit.Value);
        }
        RailwayLogging.Logger?.LogOperation("TryAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied action operation, converting eligible callback exceptions to errors.</summary>
    /// <param name="action">The callback to invoke on the selected track.</param>
    /// <param name="cancellationToken">The governing cancellation token.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// Cancellation exceptions propagate when the governing token is requested; otherwise they are mapped. Cancellation is checked before/after work and after incoming awaits.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static async Task<Result<Unit>> TryAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        Result<Unit> output;
        Exception? caught = null;
        Task? returned = default;
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            returned = action(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            caught = ex;
        }
        if (caught is null)
        {
            Task work = RailwayContracts.Output(returned);
            try
            {
                await work.ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                caught = ex;
            }
        }
        if (caught is not null)
        {
            Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
            output = new Result<Unit>.Fail(error);
        }
        else
        {
            cancellationToken.ThrowIfCancellationRequested();
            output = new Result<Unit>.Ok(Unit.Value);
        }
        RailwayLogging.Logger?.LogOperation("TryAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied map operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="mapper">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static Result<TOut> TryMap<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            TOut returned = default!;
            try
            {
                returned = mapper(ok.Value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                output = new Result<TOut>.Ok(returned);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryMap", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied map operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="mapper">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static async Task<Result<TOut>> TryMapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapper,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Task<TOut>? returned = default;
            try
            {
                returned = mapper(ok.Value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
            TOut value = default!;
            if (caught is null)
            {
                Task<TOut> work = RailwayContracts.Output(returned);
                try
                {
                    value = await work.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    caught = ex;
                }
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                output = new Result<TOut>.Ok(value);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryMapAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied map operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="mapper">The callback to invoke on the selected track.</param>
    /// <param name="cancellationToken">The governing cancellation token.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// Cancellation exceptions propagate when the governing token is requested; otherwise they are mapped. Cancellation is checked before/after work and after incoming awaits.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static async Task<Result<TOut>> TryMapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, CancellationToken, Task<TOut>> mapper,
        CancellationToken cancellationToken,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Task<TOut>? returned = default;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                returned = mapper(ok.Value, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                caught = ex;
            }
            TOut value = default!;
            if (caught is null)
            {
                Task<TOut> work = RailwayContracts.Output(returned);
                try
                {
                    value = await work.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    caught = ex;
                }
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                output = new Result<TOut>.Ok(value);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryMapAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied map operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="resultTask">The incoming task; its faults propagate without conversion.</param>
    /// <param name="mapper">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Incoming tasks are awaited normally before callback exception handling.</remarks>
    public static async Task<Result<TOut>> TryMapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, TOut> mapper,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);
        Result<TIn> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            TOut returned = default!;
            try
            {
                returned = mapper(ok.Value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                output = new Result<TOut>.Ok(returned);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryMapAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied map operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="resultTask">The incoming task; its faults propagate without conversion.</param>
    /// <param name="mapper">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Incoming tasks are awaited normally before callback exception handling.</remarks>
    public static async Task<Result<TOut>> TryMapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<TOut>> mapper,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);
        Result<TIn> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Task<TOut>? returned = default;
            try
            {
                returned = mapper(ok.Value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
            TOut value = default!;
            if (caught is null)
            {
                Task<TOut> work = RailwayContracts.Output(returned);
                try
                {
                    value = await work.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    caught = ex;
                }
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                output = new Result<TOut>.Ok(value);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryMapAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied map operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="resultTask">The incoming task; its faults propagate without conversion.</param>
    /// <param name="mapper">The callback to invoke on the selected track.</param>
    /// <param name="cancellationToken">The governing cancellation token.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// Cancellation exceptions propagate when the governing token is requested; otherwise they are mapped. Cancellation is checked before/after work and after incoming awaits.
    /// Incoming tasks are awaited normally before callback exception handling.</remarks>
    public static async Task<Result<TOut>> TryMapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, CancellationToken, Task<TOut>> mapper,
        CancellationToken cancellationToken,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);
        Result<TIn> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Task<TOut>? returned = default;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                returned = mapper(ok.Value, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                caught = ex;
            }
            TOut value = default!;
            if (caught is null)
            {
                Task<TOut> work = RailwayContracts.Output(returned);
                try
                {
                    value = await work.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    caught = ex;
                }
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                output = new Result<TOut>.Ok(value);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryMapAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied map operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="resultTask">The incoming task; its faults propagate without conversion.</param>
    /// <param name="mapper">The callback to invoke on the selected track.</param>
    /// <param name="cancellationToken">The governing cancellation token.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// Cancellation exceptions propagate when the governing token is requested; otherwise they are mapped. Cancellation is checked before/after work and after incoming awaits.
    /// Incoming tasks are awaited normally before callback exception handling.</remarks>
    public static async Task<Result<TOut>> TryMapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, CancellationToken, TOut> mapper,
        CancellationToken cancellationToken,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(mapper);
        Result<TIn> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            TOut returned = default!;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                returned = mapper(ok.Value, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                caught = ex;
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                output = new Result<TOut>.Ok(returned);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryMapAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied bind operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="binder">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static Result<TOut> TryBind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Result<TOut>> binder,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(binder);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Result<TOut>? returned = default;
            try
            {
                returned = binder(ok.Value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                output = RailwayContracts.Output(returned);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryBind", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied bind operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="binder">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static async Task<Result<TOut>> TryBindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> binder,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(binder);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Task<Result<TOut>>? returned = default;
            try
            {
                returned = binder(ok.Value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
            Result<TOut>? value = default;
            if (caught is null)
            {
                Task<Result<TOut>> work = RailwayContracts.Output(returned);
                try
                {
                    value = await work.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    caught = ex;
                }
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                output = RailwayContracts.Output(value);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryBindAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied bind operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="result">The incoming result.</param>
    /// <param name="binder">The callback to invoke on the selected track.</param>
    /// <param name="cancellationToken">The governing cancellation token.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// Cancellation exceptions propagate when the governing token is requested; otherwise they are mapped. Cancellation is checked before/after work and after incoming awaits.
    /// Only callback invocation and its asynchronous completion are converted.</remarks>
    public static async Task<Result<TOut>> TryBindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, CancellationToken, Task<Result<TOut>>> binder,
        CancellationToken cancellationToken,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(binder);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Task<Result<TOut>>? returned = default;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                returned = binder(ok.Value, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                caught = ex;
            }
            Result<TOut>? value = default;
            if (caught is null)
            {
                Task<Result<TOut>> work = RailwayContracts.Output(returned);
                try
                {
                    value = await work.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    caught = ex;
                }
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                output = RailwayContracts.Output(value);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryBindAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied bind operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="resultTask">The incoming task; its faults propagate without conversion.</param>
    /// <param name="binder">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Incoming tasks are awaited normally before callback exception handling.</remarks>
    public static async Task<Result<TOut>> TryBindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Result<TOut>> binder,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(binder);
        Result<TIn> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Result<TOut>? returned = default;
            try
            {
                returned = binder(ok.Value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                output = RailwayContracts.Output(returned);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryBindAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied bind operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="resultTask">The incoming task; its faults propagate without conversion.</param>
    /// <param name="binder">The callback to invoke on the selected track.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// OperationCanceledException propagates without mapping.
    /// Incoming tasks are awaited normally before callback exception handling.</remarks>
    public static async Task<Result<TOut>> TryBindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> binder,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(binder);
        Result<TIn> result = RailwayContracts.Output(await resultTask.ConfigureAwait(false));
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Task<Result<TOut>>? returned = default;
            try
            {
                returned = binder(ok.Value);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caught = ex;
            }
            Result<TOut>? value = default;
            if (caught is null)
            {
                Task<Result<TOut>> work = RailwayContracts.Output(returned);
                try
                {
                    value = await work.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    caught = ex;
                }
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                output = RailwayContracts.Output(value);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryBindAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied bind operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="resultTask">The incoming task; its faults propagate without conversion.</param>
    /// <param name="binder">The callback to invoke on the selected track.</param>
    /// <param name="cancellationToken">The governing cancellation token.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// Cancellation exceptions propagate when the governing token is requested; otherwise they are mapped. Cancellation is checked before/after work and after incoming awaits.
    /// Incoming tasks are awaited normally before callback exception handling.</remarks>
    public static async Task<Result<TOut>> TryBindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, CancellationToken, Task<Result<TOut>>> binder,
        CancellationToken cancellationToken,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(binder);
        Result<TIn> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Task<Result<TOut>>? returned = default;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                returned = binder(ok.Value, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                caught = ex;
            }
            Result<TOut>? value = default;
            if (caught is null)
            {
                Task<Result<TOut>> work = RailwayContracts.Output(returned);
                try
                {
                    value = await work.ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    caught = ex;
                }
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                output = RailwayContracts.Output(value);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryBindAsync", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Runs the supplied bind operation, converting eligible callback exceptions to errors.</summary>
    /// <typeparam name="TIn">The TIn value type.</typeparam>
    /// <typeparam name="TOut">The TOut value type.</typeparam>
    /// <param name="resultTask">The incoming task; its faults propagate without conversion.</param>
    /// <param name="binder">The callback to invoke on the selected track.</param>
    /// <param name="cancellationToken">The governing cancellation token.</param>
    /// <param name="errorCode">Default conversion code; ignored when a custom mapper is supplied.</param>
    /// <param name="exceptionMapper">Optional synchronous mapper; owns the complete Error and exception retention. Null selects default conversion.</param>
    /// <returns>The operation result, or a converted exception.</returns>
    /// <remarks>Required arguments are validated before work. Mapper exceptions and null structure outputs propagate.
    /// Cancellation exceptions propagate when the governing token is requested; otherwise they are mapped. Cancellation is checked before/after work and after incoming awaits.
    /// Incoming tasks are awaited normally before callback exception handling.</remarks>
    public static async Task<Result<TOut>> TryBindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, CancellationToken, Result<TOut>> binder,
        CancellationToken cancellationToken,
        string? errorCode = null,
        Func<Exception, Error>? exceptionMapper = null)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(binder);
        Result<TIn> result = await resultTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        result = RailwayContracts.Output(result);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TOut> output;
        if (result is Result<TIn>.Ok ok)
        {
            Exception? caught = null;
            Result<TOut>? returned = default;
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                returned = binder(ok.Value, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                caught = ex;
            }
            if (caught is not null)
            {
                Error error = exceptionMapper is null ? Error.FromException(caught, errorCode) : RailwayContracts.Output(exceptionMapper(caught));
                output = new Result<TOut>.Fail(error);
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                output = RailwayContracts.Output(returned);
            }
        }
        else
        {
            output = new Result<TOut>.Fail(((Result<TIn>.Fail)result).Error);
        }
        RailwayLogging.Logger?.LogOperation("TryBindAsync", result, output, timer.GetElapsed());
        return output;
    }

}
