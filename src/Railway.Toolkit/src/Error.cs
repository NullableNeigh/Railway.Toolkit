namespace Railway.Toolkit
{
    /// <summary>
    /// Represents an error with a message, error code, and optional exception.
    /// </summary>
    public sealed record Error
    {
        /// <summary>
        /// Gets the error message describing what went wrong.
        /// </summary>
        public required string Message { get; init; }

        /// <summary>
        /// Gets the error code for categorization and identification.
        /// </summary>
        public required string Code { get; init; }

        /// <summary>
        /// Gets the underlying exception if this error was caused by one.
        /// </summary>
        public Exception? Exception { get; init; }

        /// <summary>
        /// Gets structured field-level validation details, mapping field names to their error messages.
        /// Use this for form/API validation where errors need to be associated with specific fields.
        /// Example: { "Email": ["Invalid format", "Already exists"], "Age": ["Must be >= 18"] }
        /// Distinct from <see cref="InnerErrors"/> which aggregates multiple distinct errors (e.g. batch operations).
        /// </summary>
        public IReadOnlyDictionary<string, string[]>? Details { get; init; }

        /// <summary>
        /// Gets the collection of inner errors if this is an aggregate error.
        /// Used when combining multiple distinct errors (e.g. batch operations).
        /// Distinct from <see cref="Details"/> which holds field-level validation messages.
        /// </summary>
        public IReadOnlyList<Error>? InnerErrors { get; init; }

        /// <summary>
        /// Creates a new Error from an exception.
        /// </summary>
        public static Error FromException(Exception exception, string? code = null)
        {
            return new Error
            {
                Message = exception.Message,
                Code = code ?? exception.GetType().Name,
                Exception = exception
            };
        }

        /// <summary>
        /// Creates a new Error with a message and code.
        /// </summary>
        public static Error Create(string message, string code)
        {
            return new Error
            {
                Message = message,
                Code = code
            };
        }

        /// <summary>
        /// Combines multiple errors into a single aggregate error.
        /// </summary>
        /// <param name="errors">The collection of errors to combine.</param>
        /// <param name="code">Optional error code (defaults to "AggregateError").</param>
        /// <returns>An aggregate error containing all the individual errors.</returns>
        public static Error Aggregate(IEnumerable<Error> errors, string code = "AggregateError")
        {
            using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
            {
                List<Error> errorList = errors.ToList();

                if (errorList.Count == 0)
                {
                    throw new ArgumentException("Cannot create aggregate error from empty collection", nameof(errors));
                }

                if (errorList.Count == 1)
                {
                    RailwayLogging.Logger?.LogOperation("Error.Aggregate", "single error (returning as-is)", timer.Elapsed, errorList[0]);
                    return errorList[0];
                }

                string message = $"{errorList.Count} errors occurred: {string.Join("; ", errorList.Select(e => e.Message))}";

                IReadOnlyDictionary<string, string[]>? mergedDetails = MergeDetails(errorList);

                Error aggregatedError = new Error
                {
                    Message = message,
                    Code = code,
                    InnerErrors = errorList,
                    Details = mergedDetails
                };

                RailwayLogging.Logger?.LogOperation("Error.Aggregate", $"combined {errorList.Count} errors", timer.Elapsed, aggregatedError);
                return aggregatedError;
            }
        }

        /// <summary>
        /// Combines multiple errors into a single aggregate error with a custom message.
        /// </summary>
        /// <param name="errors">The collection of errors to combine.</param>
        /// <param name="message">The aggregate error message.</param>
        /// <param name="code">The error code.</param>
        /// <returns>An aggregate error containing all the individual errors.</returns>
        public static Error Aggregate(IEnumerable<Error> errors, string message, string code)
        {
            using (OperationTimer timer = new OperationTimer(RailwayLogging.Options.TimingStrategy))
            {
                List<Error> errorList = errors.ToList();

                if (errorList.Count == 0)
                {
                    throw new ArgumentException("Cannot create aggregate error from empty collection", nameof(errors));
                }

                IReadOnlyDictionary<string, string[]>? mergedDetails = MergeDetails(errorList);

                Error aggregatedError = new Error
                {
                    Message = message,
                    Code = code,
                    InnerErrors = errorList,
                    Details = mergedDetails
                };

                RailwayLogging.Logger?.LogOperation("Error.Aggregate", $"combined {errorList.Count} errors with custom message", timer.Elapsed, aggregatedError);
                return aggregatedError;
            }
        }

        /// <summary>
        /// Merges Details from a list of errors. Errors with Details contribute their field messages;
        /// errors without Details contribute their Message to a general "" bucket.
        /// Returns null if no errors had any details or messages to merge.
        /// </summary>
        private static IReadOnlyDictionary<string, string[]>? MergeDetails(List<Error> errorList)
        {
            ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();

            foreach (Error error in errorList)
            {
                if (error.Details != null)
                {
                    foreach (KeyValuePair<string, string[]> detail in error.Details)
                    {
                        foreach (string msg in detail.Value)
                        {
                            builder.AddDetail(detail.Key, msg);
                        }
                    }
                }
            }

            return builder.HasDetails ? builder.Build() : null;
        }

        public override string ToString() => $"[{Code}] {Message}";
    }
}
