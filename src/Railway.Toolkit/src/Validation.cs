namespace Railway.Toolkit;

/// <summary>
/// Helpers for running multiple validations and collecting all errors before failing.
/// Use with <see cref="ResultValidationExtensions"/> and <see cref="ResultUtilityExtensions.ToUnit{T}"/>
/// to build a complete validation pipeline.
/// </summary>
/// <example>
/// <code>
/// string email = request.Email;
/// int age = request.Age;
///
/// Result&lt;User&gt; result = Validation.Combine(
///     new[]
///     {
///         Result.Ok(email).NotNullOrEmpty("Email").ToUnit(),
///         Result.Ok(age).GreaterThan(18, "Age").ToUnit(),
///     },
///     () => new User(email, age));
/// </code>
/// </example>
public static class Validation
{
    /// <summary>
    /// Runs all validations and, if all pass, calls the builder to construct the result value.
    /// If any validations fail, all their Details are merged into a single aggregate error.
    /// Non-structured errors (without Details) are added to a general "" bucket.
    /// </summary>
    /// <typeparam name="T">The type of object to build on success.</typeparam>
    /// <param name="validationResults">The validation results to evaluate.</param>
    /// <param name="builder">Function to build the success value when all validations pass.</param>
    /// <returns>Ok with the built value if all pass, or Fail with a merged validation error.</returns>
    public static Result<T> Combine<T>(
        IEnumerable<Result<Unit>> validationResults,
        Func<T> builder)
    {
        ArgumentNullException.ThrowIfNull(validationResults);
        ArgumentNullException.ThrowIfNull(builder);

        using IRailwayTimer timer = RailwayLogging.StartOperation();

        ErrorDetailsBuilder errorBuilder = ErrorDetailsBuilder.Create();
        bool hasErrors = false;

        foreach (Result<Unit> validation in validationResults)
        {
            if (validation is Result<Unit>.Fail fail)
            {
                hasErrors = true;

                if (fail.Error.Details != null)
                {
                    foreach (KeyValuePair<string, string[]> detail in fail.Error.Details)
                    {
                        foreach (string message in detail.Value)
                        {
                            errorBuilder.AddDetail(detail.Key, message);
                        }
                    }
                }
                else
                {
                    errorBuilder.AddDetail("", fail.Error.Message);
                }
            }
        }

        Result<Unit> syntheticInput = new Result<Unit>.Ok(Unit.Value);
        Result<T> output;

        if (hasErrors)
        {
            output = new Result<T>.Fail(new Error
            {
                Message = "Validation failed",
                Code = "Validation.Failed",
                Details = errorBuilder.Build()
            });
        }
        else
        {
            output = new Result<T>.Ok(builder());
        }

        RailwayLogging.Logger?.LogOperation("Validation.Combine", syntheticInput, output, timer.GetElapsed());
        return output;
    }

    /// <summary>
    /// Runs all validations and returns Ok(Unit) if all pass.
    /// If any validations fail, all their Details are merged into a single aggregate error.
    /// </summary>
    /// <param name="validationResults">The validation results to evaluate.</param>
    /// <returns>Ok(Unit) if all pass, or Fail with a merged validation error.</returns>
    public static Result<Unit> Combine(IEnumerable<Result<Unit>> validationResults)
    {
        ArgumentNullException.ThrowIfNull(validationResults);

        return Combine(validationResults, () => Unit.Value);
    }
}
