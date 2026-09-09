using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Railway.Toolkit.Tests;

internal sealed class InMemoryLogger : ILogger
{
    private readonly bool _enabled;
    private readonly ConcurrentQueue<InMemoryLogEntry> _entries = new ConcurrentQueue<InMemoryLogEntry>();
    private readonly AsyncLocal<ScopeNode?> _currentScope = new AsyncLocal<ScopeNode?>();

    public InMemoryLogger(bool enabled = true)
    {
        _enabled = enabled;
    }

    public IReadOnlyList<InMemoryLogEntry> Entries => _entries.ToArray();

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        ScopeNode scope = new ScopeNode(this, _currentScope.Value, GetProperties(state));
        _currentScope.Value = scope;
        return scope;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _enabled;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        Dictionary<string, object?> properties = new Dictionary<string, object?>();
        List<ScopeNode> scopes = new List<ScopeNode>();

        for (ScopeNode? scope = _currentScope.Value; scope != null; scope = scope.Parent)
        {
            scopes.Add(scope);
        }

        for (int index = scopes.Count - 1; index >= 0; index--)
        {
            foreach (KeyValuePair<string, object?> property in scopes[index].Properties)
            {
                properties[property.Key] = property.Value;
            }
        }

        foreach (KeyValuePair<string, object?> property in GetProperties(state))
        {
            properties[property.Key] = property.Value;
        }

        _entries.Enqueue(new InMemoryLogEntry(logLevel, eventId, properties, exception));
    }

    public void Clear()
    {
        _entries.Clear();
    }

    private static IReadOnlyDictionary<string, object?> GetProperties<TState>(TState state)
    {
        Dictionary<string, object?> properties = new Dictionary<string, object?>();

        if (state is IEnumerable<KeyValuePair<string, object?>> structuredState)
        {
            foreach (KeyValuePair<string, object?> property in structuredState)
            {
                properties[property.Key] = property.Value;
            }
        }

        return properties;
    }

    private sealed class ScopeNode : IDisposable
    {
        private readonly InMemoryLogger _owner;
        private int _disposed;

        public ScopeNode(
            InMemoryLogger owner,
            ScopeNode? parent,
            IReadOnlyDictionary<string, object?> properties)
        {
            _owner = owner;
            Parent = parent;
            Properties = properties;
        }

        public ScopeNode? Parent { get; }

        public IReadOnlyDictionary<string, object?> Properties { get; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _owner._currentScope.Value = Parent;
            }
        }
    }
}

internal sealed record InMemoryLogEntry(
    LogLevel LogLevel,
    EventId EventId,
    IReadOnlyDictionary<string, object?> Properties,
    Exception? Exception);
