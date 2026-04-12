namespace Railway.Toolkit;

/// <summary>
/// General-purpose utility extension methods for Result.
/// </summary>
public static class ResultUtilityExtensions
{
    /// <summary>
    /// Converts a Result of any type to a Result of Unit.
    /// Useful when passing typed validation results to <see cref="Validation.Combine"/>.
    /// On success, discards the value and returns Ok(Unit).
    /// On failure, passes the error through unchanged.
    /// </summary>
    public static Result<Unit> ToUnit<T>(this Result<T> result)
    {
        if (result is Result<T>.Ok)
        {
            return new Result<Unit>.Ok(Unit.Value);
        }

        Result<T>.Fail fail = (Result<T>.Fail)result;
        return new Result<Unit>.Fail(fail.Error);
    }
}
