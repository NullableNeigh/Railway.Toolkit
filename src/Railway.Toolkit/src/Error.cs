namespace Railway.Toolkit
{
    /// <summary>
    /// Represents an error with a message, error code, and optional exception.
    /// </summary>
    public record Error
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
        /// Gets the collection of inner errors if this is an aggregate error.
        /// Used when combining multiple validation failures.
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
            List<Error> errorList = errors.ToList();

            if (errorList.Count == 0)
            {
                throw new ArgumentException("Cannot create aggregate error from empty collection", nameof(errors));
            }

            if (errorList.Count == 1)
            {
                return errorList[0];
            }

            string message = $"{errorList.Count} errors occurred: {string.Join("; ", errorList.Select(e => e.Message))}";

            return new Error
            {
                Message = message,
                Code = code,
                InnerErrors = errorList
            };
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
            List<Error> errorList = errors.ToList();

            if (errorList.Count == 0)
            {
                throw new ArgumentException("Cannot create aggregate error from empty collection", nameof(errors));
            }

            return new Error
            {
                Message = message,
                Code = code,
                InnerErrors = errorList
            };
        }

        public override string ToString() => $"[{Code}] {Message}";
    }
}
