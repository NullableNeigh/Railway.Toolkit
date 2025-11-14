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

        var results = new List<TOut>();

        foreach (var item in source)
        {
            var result = selector(item);

            var matched = result.Match<(bool isOk, TOut? value, Error? error)>(
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

        var results = new List<TOut>();
        var errors = new List<Error>();

        foreach (var item in source)
        {
            var result = selector(item);

            result.Match(
                ok => results.Add(ok.Value),
                fail => errors.Add(fail.Error)
            );
        }

        if (errors.Count > 0)
        {
            return Error.Aggregate(errors);
        }

        return results;
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

        var results = new List<TOut>();

        foreach (var item in source)
        {
            var result = await selector(item).ConfigureAwait(false);

            var matched = result.Match<(bool isOk, TOut? value, Error? error)>(
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

        var results = new List<TOut>();
        var errors = new List<Error>();

        foreach (var item in source)
        {
            var result = await selector(item).ConfigureAwait(false);

            result.Match(
                ok => results.Add(ok.Value),
                fail => errors.Add(fail.Error)
            );
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

        var values = new List<T>();

        foreach (var result in results)
        {
            var matched = result.Match<(bool isOk, T? value, Error? error)>(
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

        var values = new List<T>();
        var errors = new List<Error>();

        foreach (var result in results)
        {
            result.Match(
                ok => values.Add(ok.Value),
                fail => errors.Add(fail.Error)
            );
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

        var values = new List<T>();

        foreach (var resultTask in resultTasks)
        {
            var result = await resultTask.ConfigureAwait(false);

            var matched = result.Match<(bool isOk, T? value, Error? error)>(
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

        var values = new List<T>();
        var errors = new List<Error>();

        foreach (var resultTask in resultTasks)
        {
            var result = await resultTask.ConfigureAwait(false);

            result.Match(
                ok => values.Add(ok.Value),
                fail => errors.Add(fail.Error)
            );
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

        var successes = new List<T>();
        var failures = new List<Error>();

        foreach (var result in results)
        {
            result.Match(
                ok => successes.Add(ok.Value),
                fail => failures.Add(fail.Error)
            );
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

        var values = new List<T>();

        foreach (var result in results)
        {
            result.MatchOk(
                ok => values.Add(ok.Value),
                () => { }
            );
        }

        return values;
    }
}
