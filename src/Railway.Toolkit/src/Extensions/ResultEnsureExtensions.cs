namespace Railway.Toolkit;

/// <summary>
/// Extension methods for validating Results and ensuring conditions are met.
/// Ensure operations allow switching from the success track to the error track based on predicates.
/// </summary>
public static class ResultEnsureExtensions
{
    /// <summary>
    /// Validates that a condition holds for the success value.
    /// If the predicate returns false, the Result becomes a Fail with the provided error.
    /// If already Fail, passes the error through unchanged.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The condition that must be true.</param>
    /// <param name="error">The error to use if the predicate fails.</param>
    /// <returns>The original result if Ok and predicate passes, otherwise Fail with the error.</returns>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            Result<T> output = result.Match<Result<T>>(
                ok =>
                {
                    bool isValid = predicate(ok.Value);
                    if (isValid)
                    {
                        RailwayLogging.Logger?.LogOperation("Ensure", "passed (stayed on success track)", timer.Elapsed);
                        return ok.Value;
                    }
                    else
                    {
                        RailwayLogging.Logger?.LogOperation("Ensure", "failed (switched to failure track)", timer.Elapsed, error);
                        return error;
                    }
                },
                fail =>
                {
                    RailwayLogging.Logger?.LogOperation("Ensure", "skipped (on failure track)", timer.Elapsed);
                    return fail.Error;
                }
            );

            return output;
        }
    }

    /// <summary>
    /// Validates that a condition holds for the success value.
    /// If the predicate returns false, creates an error using the provided factory function.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The condition that must be true.</param>
    /// <param name="errorFactory">Function to create the error if the predicate fails.</param>
    /// <returns>The original result if Ok and predicate passes, otherwise Fail with the error.</returns>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Func<T, Error> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        return result.Match<Result<T>>(
            ok => predicate(ok.Value) ? ok.Value : errorFactory(ok.Value),
            fail => fail.Error
        );
    }

    /// <summary>
    /// Validates that a condition holds for the success value with a simple error message.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The condition that must be true.</param>
    /// <param name="errorMessage">The error message if the predicate fails.</param>
    /// <param name="errorCode">The error code to use (defaults to "ValidationError").</param>
    /// <returns>The original result if Ok and predicate passes, otherwise Fail with the error.</returns>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        string errorMessage,
        string errorCode = "ValidationError")
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorMessage);

        Error error = Error.Create(errorMessage, errorCode);
        return result.Ensure(predicate, error);
    }

    /// <summary>
    /// Asynchronously validates that a condition holds for the success value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to validate.</param>
    /// <param name="predicate">The condition that must be true.</param>
    /// <param name="error">The error to use if the predicate fails.</param>
    /// <returns>A task containing the validated result.</returns>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, bool> predicate,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        Result<T> result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, error);
    }

    /// <summary>
    /// Validates that an asynchronous condition holds for the success value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The async condition that must be true.</param>
    /// <param name="error">The error to use if the predicate fails.</param>
    /// <returns>A task containing the validated result.</returns>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Result<T> result,
        Func<T, Task<bool>> predicate,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
        {
            Result<T> output = await result.Match<Task<Result<T>>>(
                async ok =>
                {
                    bool isValid = await predicate(ok.Value).ConfigureAwait(false);
                    if (isValid)
                    {
                        RailwayLogging.Logger?.LogOperation("EnsureAsync", "passed (stayed on success track)", timer.Elapsed);
                        return ok.Value;
                    }
                    else
                    {
                        RailwayLogging.Logger?.LogOperation("EnsureAsync", "failed (switched to failure track)", timer.Elapsed, error);
                        return error;
                    }
                },
                fail =>
                {
                    RailwayLogging.Logger?.LogOperation("EnsureAsync", "skipped (on failure track)", timer.Elapsed);
                    return Task.FromResult<Result<T>>(fail.Error);
                }
            ).ConfigureAwait(false);

            return output;
        }
    }

    /// <summary>
    /// Validates that an asynchronous condition holds for the success value of an async result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task containing the result to validate.</param>
    /// <param name="predicate">The async condition that must be true.</param>
    /// <param name="error">The error to use if the predicate fails.</param>
    /// <returns>A task containing the validated result.</returns>
    public static async Task<Result<T>> EnsureAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<bool>> predicate,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(error);

        Result<T> result = await resultTask.ConfigureAwait(false);
        return await result.EnsureAsync(predicate, error).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that the success value is not null.
    /// Useful for reference types and nullable value types.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="errorMessage">The error message if the value is null (defaults to "Value cannot be null").</param>
    /// <param name="errorCode">The error code to use (defaults to "NullValue").</param>
    /// <returns>The original result if Ok and value is not null, otherwise Fail with the error.</returns>
    public static Result<T> EnsureNotNull<T>(
        this Result<T> result,
        string errorMessage = "Value cannot be null",
        string errorCode = "NullValue")
    {
        return result.Ensure(
            value => value is not null,
            errorMessage,
            errorCode
        );
    }

    /// <summary>
    /// Validates multiple conditions in sequence.
    /// Short-circuits on the first failing predicate.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="validations">Array of predicate-error pairs to validate.</param>
    /// <returns>The original result if all predicates pass, otherwise Fail with the first failing error.</returns>
    public static Result<T> EnsureAll<T>(
        this Result<T> result,
        params (Func<T, bool> Predicate, Error Error)[] validations)
    {
        ArgumentNullException.ThrowIfNull(validations);

        Result<T> currentResult = result;
        foreach ((Func<T, bool>? predicate, Error? error) in validations)
        {
            currentResult = currentResult.Ensure(predicate, error);
            // Short-circuit if we've already failed
            if (currentResult.Match(_ => false, _ => true))
            {
                break;
            }
        }

        return currentResult;
    }
}
