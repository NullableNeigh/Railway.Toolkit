namespace Railway.Toolkit;

/// <summary>
/// Extension methods for validating Result values with field-level error details.
/// All validators produce structured errors with the field name as a key in Details,
/// making them suitable for use with <see cref="Validation.Combine"/>.
/// </summary>
public static class ResultValidationExtensions
{
    /// <summary>
    /// Ensures the string value is not null or empty.
    /// </summary>
    public static Result<string> NotNullOrEmpty(
        this Result<string> result,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<string> output = ValidateField(result, v => !string.IsNullOrEmpty(v), fieldName, "cannot be null or empty", "Validation.Required");
        RailwayLogging.Logger?.LogOperation("NotNullOrEmpty", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the string value is not null, empty, or whitespace.
    /// </summary>
    public static Result<string> NotNullOrWhiteSpace(
        this Result<string> result,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<string> output = ValidateField(result, v => !string.IsNullOrWhiteSpace(v), fieldName, "cannot be null, empty, or whitespace", "Validation.Required");
        RailwayLogging.Logger?.LogOperation("NotNullOrWhiteSpace", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the string value is at least <paramref name="minLength"/> characters long.
    /// </summary>
    public static Result<string> HasMinLength(
        this Result<string> result,
        int minLength,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<string> output = ValidateField(result, v => v != null && v.Length >= minLength, fieldName, $"must be at least {minLength} characters", "Validation.Length");
        RailwayLogging.Logger?.LogOperation("HasMinLength", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the string value does not exceed <paramref name="maxLength"/> characters.
    /// </summary>
    public static Result<string> HasMaxLength(
        this Result<string> result,
        int maxLength,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<string> output = ValidateField(result, v => v == null || v.Length <= maxLength, fieldName, $"must not exceed {maxLength} characters", "Validation.Length");
        RailwayLogging.Logger?.LogOperation("HasMaxLength", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the value is greater than <paramref name="minimum"/>.
    /// </summary>
    public static Result<T> GreaterThan<T>(
        this Result<T> result,
        T minimum,
        string fieldName = "Value")
        where T : IComparable<T>
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = ValidateField(result, v => v.CompareTo(minimum) > 0, fieldName, $"must be greater than {minimum}", "Validation.Range");
        RailwayLogging.Logger?.LogOperation("GreaterThan", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the value is less than <paramref name="maximum"/>.
    /// </summary>
    public static Result<T> LessThan<T>(
        this Result<T> result,
        T maximum,
        string fieldName = "Value")
        where T : IComparable<T>
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = ValidateField(result, v => v.CompareTo(maximum) < 0, fieldName, $"must be less than {maximum}", "Validation.Range");
        RailwayLogging.Logger?.LogOperation("LessThan", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the value is between <paramref name="min"/> and <paramref name="max"/> (inclusive).
    /// </summary>
    public static Result<T> InRange<T>(
        this Result<T> result,
        T min,
        T max,
        string fieldName = "Value")
        where T : IComparable<T>
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<T> output = ValidateField(result, v => v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0, fieldName, $"must be between {min} and {max}", "Validation.Range");
        RailwayLogging.Logger?.LogOperation("InRange", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the integer value is greater than zero.
    /// </summary>
    public static Result<int> GreaterThanZero(
        this Result<int> result,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<int> output = ValidateField(result, v => v > 0, fieldName, "must be greater than zero", "Validation.Range");
        RailwayLogging.Logger?.LogOperation("GreaterThanZero", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the decimal value is greater than zero.
    /// </summary>
    public static Result<decimal> GreaterThanZero(
        this Result<decimal> result,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<decimal> output = ValidateField(result, v => v > 0m, fieldName, "must be greater than zero", "Validation.Range");
        RailwayLogging.Logger?.LogOperation("GreaterThanZero", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the collection is not null or empty.
    /// </summary>
    public static Result<IEnumerable<T>> NotNullOrEmpty<T>(
        this Result<IEnumerable<T>> result,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<IEnumerable<T>> output = ValidateField(result, v => v != null && v.Any(), fieldName, "cannot be null or empty", "Validation.Required");
        RailwayLogging.Logger?.LogOperation("NotNullOrEmpty", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the collection contains at least <paramref name="minCount"/> items.
    /// </summary>
    public static Result<IEnumerable<T>> HasMinCount<T>(
        this Result<IEnumerable<T>> result,
        int minCount,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<IEnumerable<T>> output = ValidateField(result, v => v != null && v.Count() >= minCount, fieldName, $"must contain at least {minCount} items", "Validation.Count");
        RailwayLogging.Logger?.LogOperation("HasMinCount", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Ensures the collection does not contain more than <paramref name="maxCount"/> items.
    /// </summary>
    public static Result<IEnumerable<T>> HasMaxCount<T>(
        this Result<IEnumerable<T>> result,
        int maxCount,
        string fieldName = "Value")
    {
        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<IEnumerable<T>> output = ValidateField(result, v => v == null || v.Count() <= maxCount, fieldName, $"must not contain more than {maxCount} items", "Validation.Count");
        RailwayLogging.Logger?.LogOperation("HasMaxCount", result, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Applies the predicate to the success value and, on failure, produces an error with
    /// <paramref name="fieldMessage"/> stored under <paramref name="fieldName"/> in Details.
    /// The Error.Message combines both for standalone display.
    /// </summary>
    private static Result<T> ValidateField<T>(
        Result<T> result,
        Func<T, bool> predicate,
        string fieldName,
        string fieldMessage,
        string code)
    {
        if (result is Result<T>.Ok ok)
        {
            if (predicate(ok.Value))
            {
                return result;
            }

            return new Result<T>.Fail(new Error
            {
                Message = $"{fieldName} {fieldMessage}",
                Code = code,
                Details = new Dictionary<string, string[]> { { fieldName, new[] { fieldMessage } } }
            });
        }

        Result<T>.Fail fail = (Result<T>.Fail)result;
        return new Result<T>.Fail(fail.Error);
    }
}
