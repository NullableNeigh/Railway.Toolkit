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

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            List<TOut> results = new List<TOut>();
            int processedCount = 0;

            foreach (TIn? item in source)
            {
                Result<TOut> result = selector(item);

                (bool isOk, TOut? value, Error? error) matched = result.Match<(bool isOk, TOut? value, Error? error)>(
                    ok => (true, ok.Value, null),
                    fail => (false, default, fail.Error)
                );

                processedCount++;

                if (!matched.isOk)
                {
                    RailwayLogging.Logger?.LogOperation("Traverse", $"failed at element {processedCount}", timer.Elapsed, matched.error);
                    return matched.error!;
                }

                results.Add(matched.value!);
            }

            RailwayLogging.Logger?.LogOperation("Traverse", $"all {processedCount} element(s) succeeded", timer.Elapsed);
            return results;
        }
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

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            List<TOut> results = new List<TOut>();
            List<Error> errors = new List<Error>();

            foreach (TIn? item in source)
            {
                Result<TOut> result = selector(item);

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

            if (errors.Count > 0)
            {
                Error aggregateError = Error.Aggregate(errors); // Error.Aggregate already logs
                RailwayLogging.Logger?.LogOperation("TraverseAll", $"failed with {errors.Count} error(s)", timer.Elapsed, aggregateError);
                return aggregateError;
            }

            RailwayLogging.Logger?.LogOperation("TraverseAll", $"all {results.Count} element(s) succeeded", timer.Elapsed);
            return results;
        }
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

        List<TOut> results = new List<TOut>();

        foreach (TIn? item in source)
        {
            Result<TOut> result = await selector(item).ConfigureAwait(false);

            (bool isOk, TOut? value, Error? error) matched = result.Match<(bool isOk, TOut? value, Error? error)>(
                ok => (true, ok.Value, null),
                fail => (false, default, fail.Error)
            );

            if (!matched.isOk)
            {
                return matched.error!;
            }

            results.Add(matched.value!);
        }

        return results;
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

        List<TOut> results = new List<TOut>();
        List<Error> errors = new List<Error>();

        foreach (TIn? item in source)
        {
            Result<TOut> result = await selector(item).ConfigureAwait(false);

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

        if (errors.Count > 0)
        {
            return Error.Aggregate(errors);
        }

        return results;
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

        List<T> values = new List<T>();

        foreach (Result<T> result in results)
        {
            (bool isOk, T? value, Error? error) matched = result.Match<(bool isOk, T? value, Error? error)>(
                ok => (true, ok.Value, null),
                fail => (false, default, fail.Error)
            );

            if (!matched.isOk)
            {
                return matched.error!;
            }

            values.Add(matched.value!);
        }

        return values;
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

        List<T> values = new List<T>();
        List<Error> errors = new List<Error>();

        foreach (Result<T> result in results)
        {
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
            return Error.Aggregate(errors);
        }

        return values;
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

        List<T> values = new List<T>();

        foreach (Task<Result<T>> resultTask in resultTasks)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

            (bool isOk, T? value, Error? error) matched = result.Match<(bool isOk, T? value, Error? error)>(
                ok => (true, ok.Value, null),
                fail => (false, default, fail.Error)
            );

            if (!matched.isOk)
            {
                return matched.error!;
            }

            values.Add(matched.value!);
        }

        return values;
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

        List<T> values = new List<T>();
        List<Error> errors = new List<Error>();

        foreach (Task<Result<T>> resultTask in resultTasks)
        {
            Result<T> result = await resultTask.ConfigureAwait(false);

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
            return Error.Aggregate(errors);
        }

        return values;
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
            if (result is Result<T>.Ok ok)
            {
                values.Add(ok.Value);
            }
        }

        return values;
    }
}
