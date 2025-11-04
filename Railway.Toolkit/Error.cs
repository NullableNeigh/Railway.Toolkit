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

        public override string ToString() => $"[{Code}] {Message}";
    }
}
