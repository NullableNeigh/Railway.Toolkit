namespace Railway.Toolkit;

/// <summary>
/// Extension methods for working with collections of Results.
/// Provides operations to traverse collections and sequence Results.
/// </summary>
public static class ResultCollectionExtensions
{
    /// <summary>
    /// Applies a Result-returning function to each element in a collection.
    /// Stops at the first error encountered (fail-fast).
    /// </summary>
    /// <typeparam name="TIn">The input element type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="selector">Function that returns a Result for each element.</param>
    /// <returns>Ok with a list of all values if all succeed, or Fail with the first error encountered.</returns>
    public static Result<IReadOnlyList<TOut>> Traverse<TIn, TOut>(
        this IEnumerable<TIn> source,
        Func<TIn, Result<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<TOut> results = new List<TOut>();
        Error? firstError = null;

        foreach (TIn? item in source)
        {
            Result<TOut> result = selector(item);
            result = RailwayContracts.Output(result);

            if (result is Result<TOut>.Ok ok)
            {
                results.Add(ok.Value);
            }
            else
            {
                Result<TOut>.Fail fail = (Result<TOut>.Fail)result;
                firstError = fail.Error;
                break;
            }
        }

        Result<IReadOnlyList<TOut>> output = firstError is not null
            ? new Result<IReadOnlyList<TOut>>.Fail(firstError)
            : new Result<IReadOnlyList<TOut>>.Ok(results);
        RailwayLogging.Logger?.LogOperation("Traverse", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Applies a Result-returning function to each element in a collection.
    /// Processes all elements and collects all errors before failing (validate-all).
    /// </summary>
    /// <typeparam name="TIn">The input element type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="selector">Function that returns a Result for each element.</param>
    /// <returns>Ok with a list of all values if all succeed, or Fail with an aggregate error containing all failures.</returns>
    public static Result<IReadOnlyList<TOut>> TraverseAll<TIn, TOut>(
        this IEnumerable<TIn> source,
        Func<TIn, Result<TOut>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<TOut> results = new List<TOut>();
        List<Error> errors = new List<Error>();

        foreach (TIn? item in source)
        {
            Result<TOut> result = selector(item);
            result = RailwayContracts.Output(result);

            if (result is Result<TOut>.Ok ok)
            {
                results.Add(ok.Value);
            }
            else
            {
                Result<TOut>.Fail fail = (Result<TOut>.Fail)result;
                errors.Add(fail.Error);
            }
        }

        Result<IReadOnlyList<TOut>> output = errors.Count > 0
            ? new Result<IReadOnlyList<TOut>>.Fail(Error.Aggregate(errors))
            : new Result<IReadOnlyList<TOut>>.Ok(results);

        RailwayLogging.Logger?.LogOperation("TraverseAll", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Asynchronously applies a Result-returning function to each element in a collection.
    /// Stops at the first error encountered (fail-fast).
    /// </summary>
    /// <typeparam name="TIn">The input element type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="selector">Async function that returns a Result for each element.</param>
    /// <returns>A task containing Ok with all values if all succeed, or Fail with the first error.</returns>
    public static async Task<Result<IReadOnlyList<TOut>>> TraverseAsync<TIn, TOut>(
        this IEnumerable<TIn> source,
        Func<TIn, Task<Result<TOut>>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<TOut> results = new List<TOut>();
        Error? firstError = null;

        foreach (TIn? item in source)
        {
            Result<TOut> result = await RailwayContracts.Output(selector(item)).ConfigureAwait(false);
            result = RailwayContracts.Output(result);

            if (result is Result<TOut>.Ok ok)
            {
                results.Add(ok.Value);
            }
            else
            {
                Result<TOut>.Fail fail = (Result<TOut>.Fail)result;
                firstError = fail.Error;
                break;
            }
        }

        Result<IReadOnlyList<TOut>> output = firstError is not null
            ? new Result<IReadOnlyList<TOut>>.Fail(firstError)
            : new Result<IReadOnlyList<TOut>>.Ok(results);
        RailwayLogging.Logger?.LogOperation("TraverseAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Asynchronously applies a Result-returning function to each element in a collection.
    /// Processes all elements and collects all errors before failing (validate-all).
    /// </summary>
    /// <typeparam name="TIn">The input element type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="selector">Async function that returns a Result for each element.</param>
    /// <returns>A task containing Ok with all values if all succeed, or Fail with an aggregate error.</returns>
    public static async Task<Result<IReadOnlyList<TOut>>> TraverseAllAsync<TIn, TOut>(
        this IEnumerable<TIn> source,
        Func<TIn, Task<Result<TOut>>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<TOut> results = new List<TOut>();
        List<Error> errors = new List<Error>();

        foreach (TIn? item in source)
        {
            Result<TOut> result = await RailwayContracts.Output(selector(item)).ConfigureAwait(false);
            result = RailwayContracts.Output(result);

            if (result is Result<TOut>.Ok ok)
            {
                results.Add(ok.Value);
            }
            else
            {
                Result<TOut>.Fail fail = (Result<TOut>.Fail)result;
                errors.Add(fail.Error);
            }
        }

        Result<IReadOnlyList<TOut>> output = errors.Count > 0
            ? new Result<IReadOnlyList<TOut>>.Fail(Error.Aggregate(errors))
            : new Result<IReadOnlyList<TOut>>.Ok(results);

        RailwayLogging.Logger?.LogOperation("TraverseAllAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Converts a collection of Results into a Result containing a collection.
    /// Stops at the first error encountered (fail-fast).
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="results">The collection of Results to sequence.</param>
    /// <returns>Ok with a list of all values if all succeed, or Fail with the first error encountered.</returns>
    public static Result<IReadOnlyList<T>> Sequence<T>(this IEnumerable<Result<T>> results)
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

        RailwayLogging.Logger?.LogOperation("Sequence", input, output, timer.GetElapsed());

        return output;
    }

    /// <summary>
    /// Converts a collection of Results into a Result containing a collection.
    /// Processes all Results and collects all errors before failing (validate-all).
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="results">The collection of Results to sequence.</param>
    /// <returns>Ok with a list of all values if all succeed, or Fail with an aggregate error containing all failures.</returns>
    public static Result<IReadOnlyList<T>> SequenceAll<T>(this IEnumerable<Result<T>> results)
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

        if (errors.Count > 0)
        {
            Result<IReadOnlyList<T>> failureOutput = new Result<IReadOnlyList<T>>.Fail(Error.Aggregate(errors));
            RailwayLogging.Logger?.LogOperation("SequenceAll", input, failureOutput, timer.GetElapsed());
            return failureOutput;
        }

        Result<IReadOnlyList<T>> output = new Result<IReadOnlyList<T>>.Ok(values);

        RailwayLogging.Logger?.LogOperation("SequenceAll", input, output, timer.GetElapsed());

        return output;
    }

    /// <summary>
    /// Asynchronously converts a collection of async Results into a Result containing a collection.
    /// Stops at the first error encountered (fail-fast).
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTasks">The collection of async Results to sequence.</param>
    /// <returns>A task containing Ok with all values if all succeed, or Fail with the first error.</returns>
    public static async Task<Result<IReadOnlyList<T>>> SequenceAsync<T>(
        this IEnumerable<Task<Result<T>>> resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();
        Error? firstError = null;

        foreach (Task<Result<T>> resultTask in resultTasks)
        {
            ArgumentNullException.ThrowIfNull(resultTask);
            Result<T> result = await resultTask.ConfigureAwait(false);
            result = RailwayContracts.Output(result);

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

        RailwayLogging.Logger?.LogOperation("SequenceAsync", input, output, timer.GetElapsed());

        return output;
    }

    /// <summary>
    /// Asynchronously converts a collection of async Results into a Result containing a collection.
    /// Processes all Results and collects all errors before failing (validate-all).
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTasks">The collection of async Results to sequence.</param>
    /// <returns>A task containing Ok with all values if all succeed, or Fail with an aggregate error.</returns>
    public static async Task<Result<IReadOnlyList<T>>> SequenceAllAsync<T>(
        this IEnumerable<Task<Result<T>>> resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();
        List<Error> errors = new List<Error>();

        foreach (Task<Result<T>> resultTask in resultTasks)
        {
            ArgumentNullException.ThrowIfNull(resultTask);
            Result<T> result = await resultTask.ConfigureAwait(false);
            result = RailwayContracts.Output(result);

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

        if (errors.Count > 0)
        {
            Result<IReadOnlyList<T>> failureOutput = new Result<IReadOnlyList<T>>.Fail(Error.Aggregate(errors));
            RailwayLogging.Logger?.LogOperation("SequenceAllAsync", input, failureOutput, timer.GetElapsed());
            return failureOutput;
        }

        Result<IReadOnlyList<T>> output = new Result<IReadOnlyList<T>>.Ok(values);

        RailwayLogging.Logger?.LogOperation("SequenceAllAsync", input, output, timer.GetElapsed());

        return output;
    }

    /// <summary>
    /// Splits a collection of Results into successful values and errors.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="results">The collection of Results to partition.</param>
    /// <returns>A tuple containing lists of successful values and errors.</returns>
    public static (IReadOnlyList<T> Successes, IReadOnlyList<Error> Failures) Partition<T>(
        this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        List<T> successes = new List<T>();
        List<Error> failures = new List<Error>();

        foreach (Result<T> result in results)
        {
            ArgumentNullException.ThrowIfNull(result);
            if (result is Result<T>.Ok ok)
            {
                successes.Add(ok.Value);
            }
            else
            {
                Result<T>.Fail fail = (Result<T>.Fail)result;
                failures.Add(fail.Error);
            }
        }

        return (successes, failures);
    }

    /// <summary>
    /// Extracts only the successful values from a collection of Results, discarding failures.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="results">The collection of Results to filter.</param>
    /// <returns>A list containing only the successful values.</returns>
    public static IReadOnlyList<T> Choose<T>(this IEnumerable<Result<T>> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        List<T> values = new List<T>();

        foreach (Result<T> result in results)
        {
            ArgumentNullException.ThrowIfNull(result);
            if (result is Result<T>.Ok ok)
            {
                values.Add(ok.Value);
            }
        }

        return values;
    }

    /// <summary>
    /// Asynchronously applies a Result-returning function to each element in a collection.
    /// Stops at the first error encountered (fail-fast).
    /// </summary>
    /// <typeparam name="TIn">The input element type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="selector">Async function that returns a Result for each element.</param>
    /// <param name="cancellationToken">The required governing token passed unchanged to the delegate.</param>
    /// <returns>A task containing Ok with all values if all succeed, or Fail with the first error.</returns>
    /// <exception cref="ArgumentNullException">A required delegate or operation argument is null.</exception>
    /// <exception cref="OperationCanceledException">The governing token is requested at an observation point, or consumer work raises cancellation.</exception>
    /// <remarks>Checks cancellation before obtaining the enumerator, before each MoveNext, before each selector invocation, after each normal selector await, and after enumeration and disposal.
    /// Every OperationCanceledException propagates; partial values and errors are discarded.
    /// Propagated cancellation emits no Railway operation log. Delegates that ignore cancellation are not interrupted.</remarks>
    public static async Task<Result<IReadOnlyList<TOut>>> TraverseAsync<TIn, TOut>(
        this IEnumerable<TIn> source,
        Func<TIn, CancellationToken, Task<Result<TOut>>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<TOut> results = new List<TOut>();
        Error? firstError = null;

        cancellationToken.ThrowIfCancellationRequested();
        using (IEnumerator<TIn> enumerator = source.GetEnumerator())
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                {
                    break;
                }

                TIn item = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                Result<TOut> result = await RailwayContracts.Output(selector(item, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                result = RailwayContracts.Output(result);

                if (result is Result<TOut>.Ok ok)
                {
                    results.Add(ok.Value);
                }
                else
                {
                    Result<TOut>.Fail fail = (Result<TOut>.Fail)result;
                    firstError = fail.Error;
                    break;
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        Result<IReadOnlyList<TOut>> output = firstError is not null
            ? new Result<IReadOnlyList<TOut>>.Fail(firstError)
            : new Result<IReadOnlyList<TOut>>.Ok(results);
        RailwayLogging.Logger?.LogOperation("TraverseAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Asynchronously applies a Result-returning function to each element in a collection.
    /// Processes all elements and collects all errors before failing (validate-all).
    /// </summary>
    /// <typeparam name="TIn">The input element type.</typeparam>
    /// <typeparam name="TOut">The output value type.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="selector">Async function that returns a Result for each element.</param>
    /// <param name="cancellationToken">The required governing token passed unchanged to the delegate.</param>
    /// <returns>A task containing Ok with all values if all succeed, or Fail with an aggregate error.</returns>
    /// <exception cref="ArgumentNullException">A required delegate or operation argument is null.</exception>
    /// <exception cref="OperationCanceledException">The governing token is requested at an observation point, or consumer work raises cancellation.</exception>
    /// <remarks>Checks cancellation before obtaining the enumerator, before each MoveNext, before each selector invocation, after each normal selector await, and after enumeration and disposal.
    /// Every OperationCanceledException propagates; partial values and errors are discarded.
    /// Propagated cancellation emits no Railway operation log. Delegates that ignore cancellation are not interrupted.</remarks>
    public static async Task<Result<IReadOnlyList<TOut>>> TraverseAllAsync<TIn, TOut>(
        this IEnumerable<TIn> source,
        Func<TIn, CancellationToken, Task<Result<TOut>>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<TOut> results = new List<TOut>();
        List<Error> errors = new List<Error>();

        cancellationToken.ThrowIfCancellationRequested();
        using (IEnumerator<TIn> enumerator = source.GetEnumerator())
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                {
                    break;
                }

                TIn item = enumerator.Current;
                cancellationToken.ThrowIfCancellationRequested();
                Result<TOut> result = await RailwayContracts.Output(selector(item, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                result = RailwayContracts.Output(result);

                if (result is Result<TOut>.Ok ok)
                {
                    results.Add(ok.Value);
                }
                else
                {
                    Result<TOut>.Fail fail = (Result<TOut>.Fail)result;
                    errors.Add(fail.Error);
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        Result<IReadOnlyList<TOut>> output = errors.Count > 0
            ? new Result<IReadOnlyList<TOut>>.Fail(Error.Aggregate(errors))
            : new Result<IReadOnlyList<TOut>>.Ok(results);

        RailwayLogging.Logger?.LogOperation("TraverseAllAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Sequences supplied tasks in input order with one governing cancellation token.</summary>
    /// <param name="resultTasks">Tasks whose underlying work received the token when created by the caller.</param>
    /// <param name="cancellationToken">The required governing token.</param>
    /// <returns>All values, or the first error.</returns>
    /// <remarks>Checks before obtaining the enumerator and each MoveNext. A yielded task is awaited normally,
    /// including when enumeration requested cancellation; cancellation is checked before processing its result.
    /// Enumeration and disposal complete before the final check. No partial result is returned on cancellation.
    /// Already-started siblings remain the caller's responsibility. Ordinary task faults propagate.</remarks>
    public static async Task<Result<IReadOnlyList<T>>> SequenceAsync<T>(
        this IEnumerable<Task<Result<T>>> resultTasks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();
        Error? firstError = null;
        cancellationToken.ThrowIfCancellationRequested();
        using (IEnumerator<Task<Result<T>>> enumerator = resultTasks.GetEnumerator())
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                {
                    break;
                }

                // MoveNext or Current may start this task; do not abandon its normal await.
                Result<T> result = await (enumerator.Current ?? throw new ArgumentNullException("resultTasks")).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                result = RailwayContracts.Output(result);
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
        }

        cancellationToken.ThrowIfCancellationRequested();
        Result<IReadOnlyList<T>> output = firstError is not null
            ? new Result<IReadOnlyList<T>>.Fail(firstError)
            : new Result<IReadOnlyList<T>>.Ok(values);
        RailwayLogging.Logger?.LogOperation("SequenceAsync", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>Sequences supplied tasks in input order with one governing cancellation token.</summary>
    /// <param name="resultTasks">Tasks whose underlying work received the token when created by the caller.</param>
    /// <param name="cancellationToken">The required governing token.</param>
    /// <returns>All values, or an aggregate of all errors.</returns>
    /// <remarks>Checks before obtaining the enumerator and each MoveNext. A yielded task is awaited normally,
    /// including when enumeration requested cancellation; cancellation is checked before processing its result.
    /// Enumeration and disposal complete before the final check. No partial result is returned on cancellation.
    /// Already-started siblings remain the caller's responsibility. Ordinary task faults propagate.</remarks>
    public static async Task<Result<IReadOnlyList<T>>> SequenceAllAsync<T>(
        this IEnumerable<Task<Result<T>>> resultTasks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();
        List<Error> errors = new List<Error>();
        cancellationToken.ThrowIfCancellationRequested();
        using (IEnumerator<Task<Result<T>>> enumerator = resultTasks.GetEnumerator())
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                {
                    break;
                }

                // MoveNext or Current may start this task; do not abandon its normal await.
                Result<T> result = await (enumerator.Current ?? throw new ArgumentNullException("resultTasks")).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                result = RailwayContracts.Output(result);
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
        }

        cancellationToken.ThrowIfCancellationRequested();
        Result<IReadOnlyList<T>> output = errors.Count > 0
            ? new Result<IReadOnlyList<T>>.Fail(Error.Aggregate(errors))
            : new Result<IReadOnlyList<T>>.Ok(values);
        RailwayLogging.Logger?.LogOperation("SequenceAllAsync", input, output, timer.GetElapsed());
        return output;
    }
}
