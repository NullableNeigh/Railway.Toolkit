using System.Collections.ObjectModel;

namespace Railway.Toolkit
{
    /// <summary>
    /// Fluent builder for constructing structured field-level validation details.
    /// </summary>
    public sealed class ErrorDetailsBuilder
    {
        private readonly Dictionary<string, List<string>> _details = new();

        private ErrorDetailsBuilder() { }

        /// <summary>
        /// Creates a new <see cref="ErrorDetailsBuilder"/> instance.
        /// </summary>
        public static ErrorDetailsBuilder Create() => new();

        /// <summary>
        /// Adds a single validation message for the specified field.
        /// </summary>
        /// <param name="key">The field name. Null or empty values are stored under an empty string key.</param>
        /// <param name="message">The validation message. Null or whitespace messages are ignored.</param>
        public ErrorDetailsBuilder AddDetail(string key, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return this;
            }

            string normalizedKey = key ?? "";

            if (!_details.TryGetValue(normalizedKey, out List<string>? list))
            {
                list = new List<string>();
                _details[normalizedKey] = list;
            }

            list.Add(message);
            return this;
        }

        /// <summary>
        /// Adds multiple validation messages for the specified field.
        /// </summary>
        /// <param name="key">The field name. Null or empty values are stored under an empty string key.</param>
        /// <param name="messages">The validation messages. Null or whitespace entries are ignored.</param>
        public ErrorDetailsBuilder AddDetails(string key, params string[] messages)
        {
            foreach (string message in messages)
            {
                AddDetail(key, message);
            }

            return this;
        }

        /// <summary>
        /// Returns true if any details have been added.
        /// </summary>
        public bool HasDetails => _details.Count > 0;

        /// <summary>
        /// Builds the immutable details dictionary.
        /// </summary>
        public IReadOnlyDictionary<string, string[]> Build()
        {
            return new ReadOnlyDictionary<string, string[]>(
                _details.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray())
            );
        }
    }
}
