namespace Railway.Toolkit;

/// <summary>
/// Extension methods for combining multiple Result instances.
/// These operations allow composing independent Results into a single Result.
/// </summary>
public static class ResultCombineExtensions
{
    /// <summary>
    /// Combines two Results into a tuple.
    /// Both must succeed for the result to be Ok. If either fails, returns the first error.
    /// </summary>
    /// <typeparam name="T1">The first value type.</typeparam>
    /// <typeparam name="T2">The second value type.</typeparam>
    /// <param name="first">The first result.</param>
    /// <param name="second">The second result.</param>
    /// <returns>Ok with both values as a tuple, or Fail with the first error encountered.</returns>
    public static Result<(T1, T2)> Zip<T1, T2>(
        this Result<T1> first,
        Result<T2> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<(T1, T2)> output = ZipCore(first, second);
        RailwayLogging.Logger?.LogOperation("Zip", first, output, timer.GetElapsed());
        return output;
    }

    private static Result<(T1, T2)> ZipCore<T1, T2>(
        this Result<T1> first,
        Result<T2> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        Result<(T1, T2)> output;

        if (first is Result<T1>.Ok ok1)
        {
            if (second is Result<T2>.Ok ok2)
            {
                output = new Result<(T1, T2)>.Ok((ok1.Value, ok2.Value));
            }
            else
            {
                Result<T2>.Fail fail2 = (Result<T2>.Fail)second;
                output = new Result<(T1, T2)>.Fail(fail2.Error);
            }
        }
        else
        {
            Result<T1>.Fail fail1 = (Result<T1>.Fail)first;
            output = new Result<(T1, T2)>.Fail(fail1.Error);
        }

        return output;
    }

    /// <summary>
    /// Combines two Results using a selector function.
    /// Both must succeed for the result to be Ok. If either fails, returns the first error.
    /// </summary>
    /// <typeparam name="T1">The first value type.</typeparam>
    /// <typeparam name="T2">The second value type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="first">The first result.</param>
    /// <param name="second">The second result.</param>
    /// <param name="selector">Function to combine the two values.</param>
    /// <returns>Ok with the combined value, or Fail with the first error encountered.</returns>
    public static Result<TResult> Zip<T1, T2, TResult>(
        this Result<T1> first,
        Result<T2> second,
        Func<T1, T2, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(selector);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TResult> output = ZipCore(first, second, selector);
        RailwayLogging.Logger?.LogOperation("Zip", first, output, timer.GetElapsed());
        return output;
    }

    private static Result<TResult> ZipCore<T1, T2, TResult>(
        this Result<T1> first,
        Result<T2> second,
        Func<T1, T2, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(selector);

        Result<(T1, T2)> zipped = first.ZipCore(second);
        return zipped is Result<(T1, T2)>.Ok ok
            ? new Result<TResult>.Ok(selector(ok.Value.Item1, ok.Value.Item2))
            : new Result<TResult>.Fail(((Result<(T1, T2)>.Fail)zipped).Error);
    }

    /// <summary>
    /// Combines three Results into a tuple.
    /// All must succeed for the result to be Ok. If any fails, returns the first error.
    /// </summary>
    public static Result<(T1, T2, T3)> Zip<T1, T2, T3>(
        this Result<T1> first,
        Result<T2> second,
        Result<T3> third)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<(T1, T2, T3)> output = ZipCore(first, second, third);
        RailwayLogging.Logger?.LogOperation("Zip", first, output, timer.GetElapsed());
        return output;
    }

    private static Result<(T1, T2, T3)> ZipCore<T1, T2, T3>(
        this Result<T1> first,
        Result<T2> second,
        Result<T3> third)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);

        Result<(T1, T2)> zipped = first.ZipCore(second);

        if (zipped is Result<(T1, T2)>.Ok ok12)
        {
            if (third is Result<T3>.Ok ok3)
            {
                return new Result<(T1, T2, T3)>.Ok((ok12.Value.Item1, ok12.Value.Item2, ok3.Value));
            }

            Result<T3>.Fail fail3 = (Result<T3>.Fail)third;
            return new Result<(T1, T2, T3)>.Fail(fail3.Error);
        }

        Result<(T1, T2)>.Fail fail = (Result<(T1, T2)>.Fail)zipped;
        return new Result<(T1, T2, T3)>.Fail(fail.Error);
    }

    /// <summary>
    /// Combines three Results using a selector function.
    /// </summary>
    public static Result<TResult> Zip<T1, T2, T3, TResult>(
        this Result<T1> first,
        Result<T2> second,
        Result<T3> third,
        Func<T1, T2, T3, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentNullException.ThrowIfNull(selector);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TResult> output = ZipCore(first, second, third, selector);
        RailwayLogging.Logger?.LogOperation("Zip", first, output, timer.GetElapsed());
        return output;
    }

    private static Result<TResult> ZipCore<T1, T2, T3, TResult>(
        this Result<T1> first,
        Result<T2> second,
        Result<T3> third,
        Func<T1, T2, T3, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentNullException.ThrowIfNull(selector);

        Result<(T1, T2, T3)> zipped = first.ZipCore(second, third);
        return zipped is Result<(T1, T2, T3)>.Ok ok
            ? new Result<TResult>.Ok(selector(ok.Value.Item1, ok.Value.Item2, ok.Value.Item3))
            : new Result<TResult>.Fail(((Result<(T1, T2, T3)>.Fail)zipped).Error);
    }

    /// <summary>
    /// Asynchronously combines two Results into a tuple.
    /// </summary>
    public static async Task<Result<(T1, T2)>> ZipAsync<T1, T2>(
        this Task<Result<T1>> firstTask,
        Task<Result<T2>> secondTask)
    {
        ArgumentNullException.ThrowIfNull(firstTask);
        ArgumentNullException.ThrowIfNull(secondTask);

        Result<T1> first = await firstTask.ConfigureAwait(false);
        first = RailwayContracts.Output(first);
        Result<T2> second = await secondTask.ConfigureAwait(false);
        second = RailwayContracts.Output(second);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<(T1, T2)> output = first.ZipCore(second);
        RailwayLogging.Logger?.LogOperation("ZipAsync", first, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Asynchronously combines two Results using a selector function.
    /// </summary>
    public static async Task<Result<TResult>> ZipAsync<T1, T2, TResult>(
        this Task<Result<T1>> firstTask,
        Task<Result<T2>> secondTask,
        Func<T1, T2, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(firstTask);
        ArgumentNullException.ThrowIfNull(secondTask);
        ArgumentNullException.ThrowIfNull(selector);

        Result<T1> first = await firstTask.ConfigureAwait(false);
        first = RailwayContracts.Output(first);
        Result<T2> second = await secondTask.ConfigureAwait(false);
        second = RailwayContracts.Output(second);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<TResult> output = first.ZipCore(second, selector);
        RailwayLogging.Logger?.LogOperation("ZipAsync", first, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Combines multiple Results into a single Result containing a list.
    /// All must succeed for the result to be Ok. If any fails, returns the first error (fail-fast).
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>Ok with a list of all values, or Fail with the first error encountered.</returns>
    public static Result<IReadOnlyList<T>> Combine<T>(params Result<T>[] results)
    {
        ArgumentNullException.ThrowIfNull(results);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        // No single Result input — use Unit to represent "aggregate operation starting"
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();

        foreach (Result<T> result in results)
        {
            ArgumentNullException.ThrowIfNull(result);
            if (result is Result<T>.Ok ok)
            {
                values.Add(ok.Value);
            }
            else
            {
                Result<T>.Fail fail = (Result<T>.Fail)result;
                Result<IReadOnlyList<T>> failOutput = new Result<IReadOnlyList<T>>.Fail(fail.Error);
                RailwayLogging.Logger?.LogOperation("Combine", input, failOutput, timer.GetElapsed());
                return failOutput;
            }
        }

        Result<IReadOnlyList<T>> output = new Result<IReadOnlyList<T>>.Ok(values);
        RailwayLogging.Logger?.LogOperation("Combine", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Combines multiple Results into a single Result containing a list.
    /// All must succeed for the result to be Ok. If any fails, returns the first error (fail-fast).
    /// </summary>
    public static Result<IReadOnlyList<T>> Combine<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return CombineCore(results, "Combine");
    }

    private static Result<IReadOnlyList<T>> CombineCore<T>(this IEnumerable<Result<T>> results, string operation)
    {
        ArgumentNullException.ThrowIfNull(results);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();
        Error? firstError = null;

        foreach (Result<T> result in results)
        {
            ArgumentNullException.ThrowIfNull(result);
            if (result is Result<T>.Ok ok)
            {
                values.Add(ok.Value);
            }
            else
            {
                Result<T>.Fail fail = (Result<T>.Fail)result;
                firstError = fail.Error;
                break;
            }
        }

        Result<IReadOnlyList<T>> output = firstError is not null
            ? new Result<IReadOnlyList<T>>.Fail(firstError)
            : new Result<IReadOnlyList<T>>.Ok(values);
        RailwayLogging.Logger?.LogOperation(operation, input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Combines multiple Results, collecting all errors before failing.
    /// If all succeed, returns Ok with all values. If any fail, returns Fail with an aggregate error containing all failures.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="results">The results to combine.</param>
    /// <returns>Ok with a list of all values, or Fail with an aggregate error of all failures.</returns>
    public static Result<IReadOnlyList<T>> CombineAll<T>(params Result<T>[] results)
    {
        ArgumentNullException.ThrowIfNull(results);

        return CombineAll(results.AsEnumerable());
    }

    /// <summary>
    /// Combines multiple Results, collecting all errors before failing.
    /// If all succeed, returns Ok with all values. If any fail, returns Fail with an aggregate error containing all failures.
    /// </summary>
    public static Result<IReadOnlyList<T>> CombineAll<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return CombineAllCore(results, "CombineAll");
    }

    private static Result<IReadOnlyList<T>> CombineAllCore<T>(this IEnumerable<Result<T>> results, string operation)
    {
        ArgumentNullException.ThrowIfNull(results);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();
        List<Error> errors = new List<Error>();

        foreach (Result<T> result in results)
        {
            ArgumentNullException.ThrowIfNull(result);
            if (result is Result<T>.Ok ok)
            {
                values.Add(ok.Value);
            }
            else
            {
                Result<T>.Fail fail = (Result<T>.Fail)result;
                errors.Add(fail.Error);
            }
        }

        Result<IReadOnlyList<T>> output = errors.Count > 0
            ? new Result<IReadOnlyList<T>>.Fail(Error.Aggregate(errors))
            : new Result<IReadOnlyList<T>>.Ok(values);

        RailwayLogging.Logger?.LogOperation(operation, input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Asynchronously combines multiple Results.
    /// All must succeed for the result to be Ok. If any fails, returns the first error (fail-fast).
    /// </summary>
    public static async Task<Result<IReadOnlyList<T>>> CombineAsync<T>(params Task<Result<T>>[] resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);
        foreach (Task<Result<T>> task in resultTasks)
        {
            ArgumentNullException.ThrowIfNull(task, nameof(resultTasks));
        }

        Result<T>[] results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        foreach (Result<T> result in results)
        {
            RailwayContracts.Output(result);
        }
        return CombineCore(results, "CombineAsync");
    }

    /// <summary>
    /// Asynchronously combines multiple Results, collecting all errors before failing.
    /// </summary>
    public static async Task<Result<IReadOnlyList<T>>> CombineAllAsync<T>(params Task<Result<T>>[] resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);
        foreach (Task<Result<T>> task in resultTasks)
        {
            ArgumentNullException.ThrowIfNull(task, nameof(resultTasks));
        }

        Result<T>[] results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        foreach (Result<T> result in results)
        {
            RailwayContracts.Output(result);
        }
        return CombineAllCore(results, "CombineAllAsync");
    }

    /// <summary>Awaits all supplied tasks, then combines their results with cancellation precedence.</summary>
    /// <param name="cancellationToken">The required governing token, supplied when creating the underlying work.</param>
    /// <param name="resultTasks">Already-started tasks in result order.</param>
    /// <returns>The combined result using Combine's error policy.</returns>
    /// <remarks>Validates tasks before cancellation. After the initial check, awaits Task.WhenAll normally;
    /// ordinary faults retain Task.WhenAll behaviour. Cancellation after normal completion prevents result processing.
    /// This operation cannot retrofit work cancellation or stop sibling tasks independently.</remarks>
    public static async Task<Result<IReadOnlyList<T>>> CombineAsync<T>(
        CancellationToken cancellationToken,
        params Task<Result<T>>[] resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);
        foreach (Task<Result<T>> task in resultTasks)
        {
            ArgumentNullException.ThrowIfNull(task, nameof(resultTasks));
        }

        cancellationToken.ThrowIfCancellationRequested();
        Result<T>[] results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Result<T> result in results)
        {
            RailwayContracts.Output(result);
        }
        return CombineCore(results, "CombineAsync");
    }

    /// <summary>Awaits all supplied tasks, then combines their results with cancellation precedence.</summary>
    /// <param name="cancellationToken">The required governing token, supplied when creating the underlying work.</param>
    /// <param name="resultTasks">Already-started tasks in result order.</param>
    /// <returns>The combined result using CombineAll's error policy.</returns>
    /// <remarks>Validates tasks before cancellation. After the initial check, awaits Task.WhenAll normally;
    /// ordinary faults retain Task.WhenAll behaviour. Cancellation after normal completion prevents result processing.
    /// This operation cannot retrofit work cancellation or stop sibling tasks independently.</remarks>
    public static async Task<Result<IReadOnlyList<T>>> CombineAllAsync<T>(
        CancellationToken cancellationToken,
        params Task<Result<T>>[] resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);
        foreach (Task<Result<T>> task in resultTasks)
        {
            ArgumentNullException.ThrowIfNull(task, nameof(resultTasks));
        }

        cancellationToken.ThrowIfCancellationRequested();
        Result<T>[] results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        foreach (Result<T> result in results)
        {
            RailwayContracts.Output(result);
        }
        return CombineAllCore(results, "CombineAllAsync");
    }

    /// <summary>Combines two supplied result tasks into a tuple with cancellation precedence.</summary>
    /// <param name="firstTask">The first task.</param>
    /// <param name="secondTask">The second task.</param>
    /// <param name="cancellationToken">The governing token supplied by the caller when creating both tasks.</param>
    /// <returns>A tuple of values or the first railway error.</returns>
    /// <remarks>Awaits the first task normally, then checks cancellation before advancing to the second await.
    /// Checks again after the second normal await. Unreached siblings remain the caller's responsibility;
    /// ordinary faults propagate. No wait-only cancellation or independent sibling cancellation is performed.</remarks>
    public static async Task<Result<(T1, T2)>> ZipAsync<T1, T2>(
        this Task<Result<T1>> firstTask,
        Task<Result<T2>> secondTask,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(firstTask);
        ArgumentNullException.ThrowIfNull(secondTask);

        Result<T1> first = await firstTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        first = RailwayContracts.Output(first);
        Result<T2> second = await secondTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        second = RailwayContracts.Output(second);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<(T1, T2)> output = first.ZipCore(second);
        RailwayLogging.Logger?.LogOperation("ZipAsync", first, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Combines two supplied result tasks using a synchronous token-aware selector.</summary>
    /// <param name="firstTask">The first task.</param>
    /// <param name="secondTask">The second task.</param>
    /// <param name="selector">Called only for two successful inputs, with the unchanged governing token.</param>
    /// <param name="cancellationToken">The governing token supplied when creating both tasks.</param>
    /// <returns>The selected value or the first railway error.</returns>
    /// <remarks>Awaits tasks sequentially and normally, checking cancellation after each normal await.
    /// Checks before selector invocation and after its normal return. Selector faults propagate.
    /// Already-started siblings not reached remain the caller's responsibility. Side effects are not rolled back.</remarks>
    public static async Task<Result<TResult>> ZipAsync<T1, T2, TResult>(
        this Task<Result<T1>> firstTask,
        Task<Result<T2>> secondTask,
        Func<T1, T2, CancellationToken, TResult> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(firstTask);
        ArgumentNullException.ThrowIfNull(secondTask);
        ArgumentNullException.ThrowIfNull(selector);

        Result<T1> first = await firstTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        first = RailwayContracts.Output(first);
        Result<T2> second = await secondTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        second = RailwayContracts.Output(second);
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        if (first is Result<T1>.Fail firstFail)
        {
            Result<TResult> failure = new Result<TResult>.Fail(firstFail.Error);
            RailwayLogging.Logger?.LogOperation("ZipAsync", first, failure, timer.GetElapsed());
            return failure;
        }
        if (second is Result<T2>.Fail secondFail)
        {
            Result<TResult> failure = new Result<TResult>.Fail(secondFail.Error);
            RailwayLogging.Logger?.LogOperation("ZipAsync", first, failure, timer.GetElapsed());
            return failure;
        }

        cancellationToken.ThrowIfCancellationRequested();
        TResult value = selector(((Result<T1>.Ok)first).Value, ((Result<T2>.Ok)second).Value, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        Result<TResult> output = new Result<TResult>.Ok(value);
        RailwayLogging.Logger?.LogOperation("ZipAsync", first, output, timer.GetElapsed());
        return output;
    }
}
