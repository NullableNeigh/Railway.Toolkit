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
        return first.Match<Result<(T1, T2)>>(
            ok1 => second.Match<Result<(T1, T2)>>(
                ok2 => (ok1.Value, ok2.Value),
                fail2 => fail2.Error
            ),
            fail1 => fail1.Error
        );
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
        ArgumentNullException.ThrowIfNull(selector);

        return first.Zip(second).Map(tuple => selector(tuple.Item1, tuple.Item2));
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
        return first.Zip(second).Match<Result<(T1, T2, T3)>>(
            ok12 => third.Match<Result<(T1, T2, T3)>>(
                ok3 => (ok12.Value.Item1, ok12.Value.Item2, ok3.Value),
                fail3 => fail3.Error
            ),
            fail => fail.Error
        );
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
        ArgumentNullException.ThrowIfNull(selector);

        return first.Zip(second, third).Map(tuple => selector(tuple.Item1, tuple.Item2, tuple.Item3));
    }

    /// <summary>
    /// Asynchronously combines two Results into a tuple.
    /// </summary>
    public static async Task<Result<(T1, T2)>> ZipAsync<T1, T2>(
        this Task<Result<T1>> firstTask,
        Task<Result<T2>> secondTask)
    {
        var first = await firstTask.ConfigureAwait(false);
        var second = await secondTask.ConfigureAwait(false);
        return first.Zip(second);
    }

    /// <summary>
    /// Asynchronously combines two Results using a selector function.
    /// </summary>
    public static async Task<Result<TResult>> ZipAsync<T1, T2, TResult>(
        this Task<Result<T1>> firstTask,
        Task<Result<T2>> secondTask,
        Func<T1, T2, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var first = await firstTask.ConfigureAwait(false);
        var second = await secondTask.ConfigureAwait(false);
        return first.Zip(second, selector);
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
    /// Combines multiple Results into a single Result containing a list.
    /// All must succeed for the result to be Ok. If any fails, returns the first error (fail-fast).
    /// </summary>
    public static Result<IReadOnlyList<T>> Combine<T>(this IEnumerable<Result<T>> results)
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
    /// Asynchronously combines multiple Results.
    /// All must succeed for the result to be Ok. If any fails, returns the first error (fail-fast).
    /// </summary>
    public static async Task<Result<IReadOnlyList<T>>> CombineAsync<T>(params Task<Result<T>>[] resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        var results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        return Combine(results);
    }

    /// <summary>
    /// Asynchronously combines multiple Results, collecting all errors before failing.
    /// </summary>
    public static async Task<Result<IReadOnlyList<T>>> CombineAllAsync<T>(params Task<Result<T>>[] resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        var results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        return CombineAll(results);
    }
}
