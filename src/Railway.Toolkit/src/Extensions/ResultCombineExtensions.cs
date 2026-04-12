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
        using IRailwayTimer timer = RailwayLogging.StartOperation();

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

        RailwayLogging.Logger?.LogOperation("Zip", first, output, timer.GetElapsed());
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
        Result<(T1, T2)> zipped = first.Zip(second);

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
        Result<T1> first = await firstTask.ConfigureAwait(false);
        Result<T2> second = await secondTask.ConfigureAwait(false);
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

        Result<T1> first = await firstTask.ConfigureAwait(false);
        Result<T2> second = await secondTask.ConfigureAwait(false);
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        // No single Result input — use Unit to represent "aggregate operation starting"
        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();

        foreach (Result<T> result in results)
        {
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
        List<T> values = new List<T>();

        foreach (Result<T> result in results)
        {
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

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        Result<Unit> input = new Result<Unit>.Ok(Unit.Value);
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

        Result<IReadOnlyList<T>> output = errors.Count > 0
            ? new Result<IReadOnlyList<T>>.Fail(Error.Aggregate(errors))
            : new Result<IReadOnlyList<T>>.Ok(values);

        RailwayLogging.Logger?.LogOperation("CombineAll", input, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Asynchronously combines multiple Results.
    /// All must succeed for the result to be Ok. If any fails, returns the first error (fail-fast).
    /// </summary>
    public static async Task<Result<IReadOnlyList<T>>> CombineAsync<T>(params Task<Result<T>>[] resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        Result<T>[] results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        return Combine(results);
    }

    /// <summary>
    /// Asynchronously combines multiple Results, collecting all errors before failing.
    /// </summary>
    public static async Task<Result<IReadOnlyList<T>>> CombineAllAsync<T>(params Task<Result<T>>[] resultTasks)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        Result<T>[] results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
        return CombineAll(results);
    }
}
