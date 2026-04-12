# Railway.Toolkit - Claude Guidelines

## Project Overview

This is a functional programming toolkit for C# implementing Railway-Oriented Programming with `Result<T>` types for clean error handling and functional composition.

## Development Principles

### Workflow

**Discussion Before Code**
- Start with discussion and exploration
- Make as many decisions as possible upfront
- Only begin coding once direction is clear
- Ask clarifying questions early

### Code Style

**Type Declarations**
- No `var` - use explicit type declarations
- Example: `Result<int> result = Result.Ok(5);` not `var result = Result.Ok(5);`

**Naming Conventions**
- Follow Microsoft C# coding conventions as base
- Add functional programming conventions where appropriate
- Use FP-familiar names: `Bind`, `Map`, `Traverse`, `Sequence`, etc.
- Prefer functional terminology even if not typical C# (e.g., `Ok`/`Fail` vs `Success`/`Failure`)
- Async methods always end with `Async` suffix
- Blend C# and functional programming worlds

### Design Philosophy

**SOLID Principles**
- Apply SOLID principles where possible
- Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion

**DRY (Don't Repeat Yourself)**
- Avoid code duplication
- Extract common patterns into reusable components

**Generics Usage**
- Keep generics simple and avoid complexity
- Prefer clear, straightforward code over heavily generic abstractions
- If generics make code harder to understand, reconsider the approach

**Design Patterns**
- Use proper design patterns: Strategy, Factory, Builder, etc.
- Apply patterns where they add clarity and maintainability
- Don't force patterns where they don't fit naturally

### Testing Requirements

**Testing Framework**
- Use xUnit version 3 (latest version)
- Test as much as possible - aim for comprehensive coverage
- All new features should include tests

**Code Comments**
- Only add comments where extra explanation is needed
- Don't mirror the code with obvious comments
- Focus on "why" not "what" when commenting

**Logging**
- Use sensible logging throughout
- Add extra detailed logs in complex areas
- Log important state changes and decision points
- See "Logging Architecture" section below for detailed logging patterns

## Architecture Notes

**Development Approach**
- Create core functionality first
- Add extra features using plugins/extensions
- Keep core simple and extensible

**Documentation Strategy**
- Leave documentation until last
- This prevents knock-on effects when code changes
- Document once the implementation is stable

**Versioning Strategy**
- Pre-1.0: Use 0.x.x versions with flexibility for breaking changes
- Post-1.0: Strict Semantic Versioning (MAJOR.MINOR.PATCH)
  - MAJOR: Breaking changes
  - MINOR: New backward-compatible features
  - PATCH: Bug fixes only
- Plugins must match core version exactly (e.g., core 2.3.3 requires plugin 2.3.3)
- All packages (core + plugins) version together
- Can revisit this strategy if needed

**Public API Surface**
- Minimal public surface - only expose what users need
- Everything else marked as `internal` by default
- Maintain control of the library evolution
- If users need functionality, expose it intentionally after validation
- Ensures all public APIs work as intended before release
- Makes breaking changes easier to avoid

**Backward Compatibility**
- Pre-1.0 (0.x.x): Break freely as needed to get the API right
- Use the pre-1.0 phase to experiment and stabilize
- Post-1.0: Strict backward compatibility within major versions
- Never break compatibility without warning and proper major version bump
- Keep users in mind - their code should keep working

**Dependencies**
- Minimal strategic dependencies - avoid unless justified
- Each runtime dependency must be:
  - Stable and well-maintained
  - Adding significant value
  - Justified and documented
- Dev/test dependencies allowed for improved development experience
- These don't affect users, so less concern
- Review dependencies regularly

**File Organization**
- Use feature folders to collect related functionality
- Files should follow single responsibility principle
- Related types can live in the same file (e.g., `Result<T>`, `Ok<T>`, `Fail<T>` in `Result.cs`)
- If an interface is only used by one class, put them in the same file (interface at top)
- Extension methods grouped by category (e.g., `ResultMapExtensions.cs`, `ResultBindExtensions.cs`)
- Balance between organization and practicality

**Namespace Strategy**
- Flat namespace: Everything in `Railway.Toolkit`
- Users need only one `using` statement
- Simple, immediate access to all functionality
- No point limiting access when users need it
- Feature folders organize code internally, not namespaces

**Purity & Side Effects**
- Assume purity by default - this is a functional library
- Method names communicate intent (Map, Bind = pure; Tap = side effects)
- Document side effects clearly when they exist
- No need for `[Pure]` attributes - keep it simple
- Side-effect methods (like Tap, TapAsync) should clearly document their purpose

**Immutability**
- Public API must be immutable - no public setters
- Use `record` types for core types (Result, Error, etc.)
- Properties use `init` or readonly fields
- Methods return new instances, never modify existing ones
- Internal implementation can use mutation for performance if needed
- Users see immutability, internals can optimize
- Pragmatic balance between functional purity and performance

**Railway Pattern Guidelines**
- Use pattern catalog approach - show complete, working examples
- Document common patterns: Validation Pipeline, Error Recovery, Batch Processing, etc.
- Each pattern should be a complete scenario users can copy and adapt
- Explain the mindset change from imperative to functional thinking
- Include diagrams where possible to visualize the railway concept
- Show the mental model: success track vs failure track
- Help C# developers transition from try-catch to railway patterns

**Error Messages**
- Technical and precise - this library is for developers, not end users
- Include details to help debug: "Validation failed: Age must be >= 18 (provided: 15)"
- Error messages should teach what went wrong in the pipeline
- Provide context for logging and troubleshooting
- Application layer can simplify for end users if needed
- Better to have too much information than too little

**Error Codes**
- Recommend namespaced/hierarchical format: `"Validation.InvalidEmail"`, `"Database.NotFound"`
- Shows error categories and enables pattern matching on prefixes
- Developers have freedom to use what makes sense for their context
- Can use simple codes like `"NOT_FOUND"` if namespacing isn't needed
- Consistency within a project is more important than strict enforcement
- Library examples should demonstrate the namespaced approach

**Async Patterns - ConfigureAwait**
- Library code always uses `ConfigureAwait(false)` internally
- Best practice for library code - avoids deadlocks and context issues
- Developers calling the library control their own async context
- No configuration needed - standard library pattern
- Can be revisited if needed

**Async Patterns - Cancellation Tokens**
- Pass-through approach: library doesn't explicitly take CancellationToken parameters
- Users pass tokens to their own async functions as needed
- Keeps API simple and lightweight
- Library does orchestration, user functions do actual async work
- May add selective support (e.g., TraverseAsync on large collections) if requested
- Can evolve post-1.0 based on user feedback

**Performance - Allocation Concerns**
- Be performance aware - write clean code but avoid obvious waste
- Don't use LINQ when a simple loop is better
- Be mindful of allocations without obsessing
- Balance readability and performance
- Users needing extreme performance can bypass railway chaining
- Do work, return Result as single event rather than chaining everything
- Railway pattern provides clean code; performance-critical paths can opt out

**Performance - Benchmarking**
- Not a priority at this time
- Focus on getting the API right first
- Can add benchmarking infrastructure later if needed
- Performance awareness is sufficient for now

## Core Type Design

**Result Type - Discriminated Union Pattern**
- `Result<T>` must be a true discriminated union with nested `Ok` and `Fail` types
- This is fundamental to Railway-Oriented Programming and type safety
- Pattern matching with `is` operator provides compile-time guarantees
- Future C# versions will formalize discriminated unions - we align with that direction
- **Why this matters:**
  - Compile-time exhaustiveness checking (compiler ensures all cases handled)
  - Better IntelliSense and tooling support
  - Clearer intent - you're either on success track or failure track, never both
  - More idiomatic C# than callback-based Match methods
  - Prepares codebase for future C# DU features

**DU Implementation Strategy**
- Current code was generated from DUNet and extracted as static code
- Nested type pattern (`Result<T>.Ok`, `Result<T>.Fail`) aligns with likely future C# DU syntax
- **Match Methods Strategy:**
  - Keep Match methods for user compatibility and functional style preference
  - Switch ALL extension method internals to use `is` pattern matching (not Match callbacks)
  - Document and encourage pattern matching in examples, not Match
  - Mark Match as "legacy/compatibility" approach in documentation
  - Simplify Match overloads - keep essential ones, remove niche stateful variants
  - When C# DU arrives: deprecate Match with `[Obsolete]`, eventually remove in next major
- **File Organization:**
  - Merge Result.Generated.cs into Result.cs (no longer generated, now first-class API)
  - Single file is simpler and easier to navigate
  - Future migration will replace entire file anyway
- **Migration Preparation:**
  - Use pattern matching internally NOW to align with future
  - Nested type syntax is close enough to future C# DU for easy migration
  - Main changes when C# DU arrives: syntax (`enum` keyword), pattern matching syntax
  - Concept remains the same - we're already aligned

**Result Type Structure**
```csharp
public abstract record Result<T>
{
    // Prevent external inheritance
    private Result() { }

    // Nested types for the two possible states
    public sealed record Ok(T Value) : Result<T>;
    public sealed record Fail(Error Error) : Result<T>;
}
```

**Usage Pattern**
```csharp
// Pattern matching - compile-time safe
if (result is Result<int>.Ok success)
{
    Console.WriteLine(success.Value);
}
else // Must be Fail - compiler knows this
{
    var failure = (Result<int>.Fail)result;
    Console.WriteLine(failure.Error);
}

// Or with switch expressions
var message = result switch
{
    Result<int>.Ok ok => $"Success: {ok.Value}",
    Result<int>.Fail fail => $"Error: {fail.Error}",
    _ => throw new InvalidOperationException("Impossible")
};
```

**Extension Methods**
- Keep extension method approach for fluent API (`result.Map(x => x * 2).Bind(Validate)`)
- Extension methods operate on the `Result<T>` base type
- Internally use pattern matching to handle Ok/Fail cases
- Best of both worlds: fluent composition + type-safe pattern matching

## Error Structure

**Error Type - Structured Details**
- Errors must support structured field-level validation details
- Critical for real-world validation scenarios (form validation, API errors, etc.)
- Simple error messages are insufficient for complex validation
- Error must be `sealed` - data container not meant for inheritance

**Error Structure**
```csharp
public sealed record Error
{
    public required string Message { get; init; }
    public required string Code { get; init; }

    // Structured validation details: field name -> array of error messages
    // Example: { "Email": ["Invalid format", "Already exists"], "Age": ["Must be >= 18"] }
    // Use string[] not IReadOnlyList<string> - simpler, better performance, JSON serialization friendly
    public IReadOnlyDictionary<string, string[]>? Details { get; init; }

    // Nested errors for aggregation scenarios (different from Details)
    // Details = field-level validation (multiple messages per field)
    // InnerErrors = error aggregation (multiple distinct errors, e.g. batch operations)
    public IReadOnlyList<Error>? InnerErrors { get; init; }

    // Exception wrapping for railway pattern
    public Exception? Exception { get; init; }
}
```

**Error Code Design - String not Enum**
- Use `string` for error codes, NOT enums
- Rationale: This is a library for developers - let them use it how they like
- Apps need to define their own error codes beyond what library provides
- Hierarchical codes supported: `"Validation.InvalidEmail"`, `"Database.Connection.Timeout"`
- Library cannot predict all error scenarios users will encounter
- Developers have full flexibility to organize codes as needed
- Trade compile-time safety for extensibility and flexibility

**Error Details Builder Pattern**
- Provide `ErrorDetailsBuilder` to make it easy for users to construct Details dictionary
- Fluent API for adding field-level validation messages
- Users can still construct dictionary manually if they prefer
- Builder is a convenience, not a requirement
- Implementation:
```csharp
public sealed class ErrorDetailsBuilder
{
    private readonly Dictionary<string, List<string>> _details = new();

    public ErrorDetailsBuilder AddDetail(string key, string message)
    {
        if (!_details.TryGetValue(key ?? "", out var list))
        {
            list = new List<string>();
            _details[key ?? ""] = list;
        }
        list.Add(message);
        return this;
    }

    public ErrorDetailsBuilder AddDetails(string key, params string[] messages)
    {
        foreach (var message in messages)
            AddDetail(key, message);
        return this;
    }

    public IReadOnlyDictionary<string, string[]> Build()
    {
        return new ReadOnlyDictionary<string, string[]>(
            _details.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray())
        );
    }

    public static ErrorDetailsBuilder Create() => new();
}
```
- Usage example:
```csharp
var errorBuilder = ErrorDetailsBuilder.Create();
errorBuilder.AddDetail("Email", "Invalid email format");
errorBuilder.AddDetail("Email", "Email already registered");
errorBuilder.AddDetail("Age", "Must be at least 18 years old");

var error = new Error
{
    Message = "Validation failed",
    Code = "Validation.Failed",
    Details = errorBuilder.Build()
};
```

**Error Details Use Cases**
- Form validation: map errors to specific input fields
- API responses: return structured validation errors to clients
- Batch operations: collect all validation errors before failing
- Better user experience: show all validation issues at once, not one at a time

## Logging Architecture

**Logging Pattern - Full Context to Logger**
- Logger receives complete Result context (input + output Results)
- Logger makes intelligent decisions about what and how to log
- Operations don't decide log format - separation of concerns
- Enables rich, context-aware logging strategies

**CallerMemberName for Operation Names**
- Use `[CallerMemberName]` attribute to auto-capture operation names
- Eliminates hardcoded string literals and typos
- Compiler guarantees correctness
- Example:
```csharp
public static Result<TOut> Map<TIn, TOut>(
    this Result<TIn> result,
    Func<TIn, TOut> mapper,
    [CallerMemberName] string operation = "Map")
{
    using var timing = RailwayLogger.StartOperation();

    // ... perform operation ...

    RailwayLogger.LogOperation(operation, result, output, timing.GetElapsed());
    return output;
}
```

**Logger Signature**
```csharp
// Logger receives full context - can inspect values, errors, types, etc.
void LogOperation<TIn, TOut>(
    string operation,
    Result<TIn> input,
    Result<TOut> output,
    TimeSpan elapsed);
```

**Why This Pattern is Superior**
- **Separation of Concerns**: Operations focus on logic, logger focuses on logging strategy
- **Rich Context**: Logger sees everything - can log values, errors, type information, etc.
- **Flexible Logging**: Logger can decide based on context:
  - Log only failures
  - Log slow operations
  - Log track transitions (success → failure or failure → success)
  - Include/exclude values based on type or configuration
  - Sample operations (log every Nth call)
- **Zero String Duplication**: CallerMemberName means "Map" operation always logs as "Map"
- **Testability**: Can verify logger receives correct inputs/outputs
- **Performance**: Logger can optimize - skip expensive logging when disabled

**Logging Configuration**
- Keep existing timing strategies (HighPrecision, Standard, None)
- Keep existing filtering (slow operation threshold, sampling rate)
- Logger implementation decides what to emit to ILogger
- Operations remain simple and focused on their purpose

**Migration Note**
- Current approach has operations decide what to log (strings like "executed", "failed at element 3")
- New approach: operations call logger with full context, logger decides what to log
- Logger can generate the same messages but has more flexibility
- Cleaner separation and more powerful for users who want custom logging

**Timer Strategy Pattern**
- Use Strategy pattern for timer implementations (not enum)
- Define `IRailwayTimer` interface with multiple implementations
- Factory pattern for timer creation based on configuration
- Null Object pattern with `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for zero overhead when disabled
- Support decorator pattern (e.g., `SamplingRailwayTimer` wraps another timer)
- Allow custom timer strategy registration for extensibility
- Example implementations:
  - `NullRailwayTimer` - Zero overhead when timing disabled
  - `StopwatchRailwayTimer` - Standard Stopwatch-based timing
  - `HighResolutionRailwayTimer` - QueryPerformanceCounter for high precision
  - `SamplingRailwayTimer` - Only measures every Nth operation
```csharp
public interface IRailwayTimer
{
    IDisposable StartTiming();
    TimeSpan GetElapsed();
    void Reset();
}
```

**AsyncLocal Context for Thread Safety**
- Use `AsyncLocal<T>` for thread-safe async context propagation
- Avoid global static state (race conditions in concurrent scenarios)
- Scoped logging with `IDisposable` pattern
- Each async flow can have isolated logging configuration
- Example:
```csharp
private static readonly AsyncLocal<ILogger> _contextLogger = new();
private static readonly AsyncLocal<RailwayLoggingOptions> _options = new();
private static readonly AsyncLocal<IRailwayTimer> _timer = new();

public static IDisposable EnableLogging(ILogger logger, RailwayLoggingOptions? options = null)
{
    var previousLogger = _contextLogger.Value;
    var previousOptions = _options.Value;

    _contextLogger.Value = logger;
    _options.Value = options ?? RailwayLoggingOptions.Default;

    return new LoggerScope(() =>
    {
        _contextLogger.Value = previousLogger;
        _options.Value = previousOptions;
    });
}
```

**Environment-Aware Configuration**
- Read `ASPNETCORE_ENVIRONMENT` environment variable for automatic defaults
- Development: verbose logging, detailed timings, log success values
- Testing: high-resolution timings for precision, minimal verbosity
- Staging: sampling strategy to reduce overhead, moderate logging
- Production: minimal/no overhead by default, errors only
- Example:
```csharp
public static RailwayLoggingOptions Default
{
    get
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        return env switch
        {
            "Development" => new() { LogTimings = true, LogSuccessValues = true },
            "Testing" => new() { TimingStrategy = TimingStrategy.HighResolution },
            "Staging" => new() { TimingStrategy = TimingStrategy.Sampling },
            _ => new() { LogTimings = false, TimingStrategy = TimingStrategy.None }
        };
    }
}
```

**Structured Logging with Scopes**
- Use `ILogger.BeginScope` to add structured context to all log entries
- Include metadata: operation name, input type, success/failure, duration
- Enables powerful querying in observability tools (Seq, Application Insights, etc.)
- Example:
```csharp
var context = new Dictionary<string, object>
{
    ["Operation"] = operation,
    ["InputType"] = typeof(T).Name,
    ["Success"] = output is Result<T>.Ok,
    ["DurationMs"] = duration.TotalMilliseconds
};

using var scope = logger.BeginScope(context);
logger.LogDebug("🚂 {Operation}: Success ✓", operation);
```

**Logging Verbosity Control**
- `LogSlowOperationsOnly` - Skip logging fast operations (reduce noise)
- `LogSuccessValues` - Control whether to log actual values (security/verbosity)
- `SlowOperationThresholdMs` - Define what "slow" means
- Early exit when logging disabled or filtered out
- Example slow operation detection:
```csharp
if (options.LogSlowOperationsOnly && duration < threshold)
    return; // Skip logging

if (duration > options.SlowOperationThresholdMs)
{
    logger.LogWarning("🐌 Slow {Operation}: {Duration}ms", operation, duration);
}
```

**Emoji for Visual Scanning**
- Use emoji in log messages for quick visual identification
- `🚂` - Normal railway operation
- `🐌` - Slow operation (exceeds threshold)
- `✓` - Success indicator
- Makes logs easier to scan visually in log viewers

**Logging Performance Optimization**
- Use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on hot paths
- Early exit checks before expensive operations
- Null timer pattern for zero overhead when disabled
- JIT will optimize away null checks in hot paths

## Validation Architecture

**Validation Helpers**
- Provide common validation extension methods for typical scenarios
- All validation methods accept `fieldName` parameter for structured error details
- Build structured errors that map to form fields or API properties
- Common validators to provide:
  - String: `NotNullOrEmpty`, `NotNullOrWhiteSpace`, `HasMinLength`, `HasMaxLength`
  - Numeric: `GreaterThan`, `LessThan`, `InRange`, `GreaterThanZero`
  - Collections: `NotNullOrEmpty`, `HasMinCount`, `HasMaxCount`
  - Properties: `NotNull` (for selected property)

**Field-Level Validation Pattern**
```csharp
public static Result<string> NotNullOrEmpty(
    this Result<string> result,
    string fieldName = "Value",
    [CallerMemberName] string operation = "NotNullOrEmpty")
{
    return result.Ensure(
        value => !string.IsNullOrEmpty(value),
        ErrorCode.Validation,
        $"{fieldName} cannot be null or empty"
    );
}
```

**Validation Combine Pattern**
- Combine multiple validations and collect ALL errors before failing
- Better UX - show all validation issues at once, not one at a time
- Build structured error with all field-level failures
- Example:
```csharp
var validations = new[]
{
    email.NotNullOrEmpty(fieldName: "Email"),
    age.GreaterThan(18, fieldName: "Age"),
    name.HasMinLength(2, fieldName: "Name")
};

return Validation.Combine(validations, () => new User(email, age, name));
// Returns single error with Details: { "Email": [...], "Age": [...], "Name": [...] }
```

## Common Tasks


