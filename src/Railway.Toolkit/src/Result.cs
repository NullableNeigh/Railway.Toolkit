namespace Railway.Toolkit
{
    /// <summary>
    /// Represents the result of an operation that can either succeed with a value or fail with an error.
    /// This is the base definition - the implementation is in Result.Generated.cs.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    public partial record Result<T>
    {
        /// <summary>
        /// Represents a successful result containing a value.
        /// </summary>
        public partial record Ok(T Value);

        /// <summary>
        /// Represents a failed result containing an error.
        /// </summary>
        public partial record Fail(Error Error);
    }
}
