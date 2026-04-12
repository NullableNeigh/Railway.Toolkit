# Railway.Toolkit Refactor Plan

## Overview

Major refactor to align with best practices from RailwayKit while maintaining Railway.Toolkit's comprehensive API and test coverage.

**Core Goals:**
1. Add structured error validation support (Details property + ErrorDetailsBuilder)
2. Switch extension methods to use pattern matching internally (prepare for C# DU)
3. Refactor logging to use AsyncLocal, Strategy pattern, CallerMemberName, and full Result context
4. Add validation helpers with field-level error support
5. Maintain 100% test pass rate throughout

**Branch Strategy:** Create new branch `refactor/railway-improvements` from `SetupResult`

---

## Phase 1: Error Structure Enhancement

**Goal:** Add structured validation support without breaking existing code.

### 1.1 Update Error Type
- [ ] Make `Error` sealed
- [ ] Add `Details` property: `IReadOnlyDictionary<string, string[]>? Details { get; init; }`
- [ ] Update XML documentation to explain Details vs InnerErrors distinction
- [ ] No breaking changes - purely additive

**File:** `src/Railway.Toolkit/src/Error.cs`

**Validation:**
- [ ] All existing tests pass
- [ ] No compilation errors

### 1.2 Create ErrorDetailsBuilder
- [ ] Create new file: `src/Railway.Toolkit/src/ErrorDetailsBuilder.cs`
- [ ] Implement fluent builder pattern:
  - `AddDetail(string key, string message)` - add single message
  - `AddDetails(string key, params string[] messages)` - add multiple messages
  - `Build()` - returns `IReadOnlyDictionary<string, string[]>`
  - `static Create()` - factory method
- [ ] Handle null/empty keys (use empty string as default)
- [ ] Filter out null/whitespace messages
- [ ] Return `ReadOnlyDictionary` from Build()

**Validation:**
- [ ] Write unit tests for ErrorDetailsBuilder
- [ ] Test null handling, empty collections, fluent chaining

### 1.3 Update Error.Aggregate
- [ ] Update `Error.Aggregate` to merge Details from multiple errors
- [ ] If errors have Details, combine them into aggregated error's Details
- [ ] If errors have no Details, add message to general bucket (key = "")

**Validation:**
- [ ] Update existing Error.Aggregate tests
- [ ] Add tests for Detail aggregation

**Estimated Time:** 2-3 hours
**Risk:** Low - additive changes only

---

## Phase 2: Result DU Refinement

**Goal:** Simplify Result implementation and prepare for future C# DU.

### 2.1 Merge Result Files
- [ ] Copy all content from `Result.Generated.cs` into `Result.cs`
- [ ] Delete `Result.Generated.cs`
- [ ] Update namespace, remove `#pragma warning` directives
- [ ] Keep all Match method overloads initially (will simplify later)

**Files:**
- Merge: `src/Railway.Toolkit/src/Result.Generated.cs` → `src/Railway.Toolkit/src/Result.cs`
- Delete: `src/Railway.Toolkit/src/Result.Generated.cs`

**Validation:**
- [ ] All 166 tests pass
- [ ] No compilation errors
- [ ] Git diff shows no logic changes, just file reorganization

### 2.2 Simplify Match Overloads (Optional - Can Skip)
- [ ] Keep essential Match methods:
  - `Match<TOut>(Func<Ok, TOut>, Func<Fail, TOut>)` - core pattern
  - `MatchOk<TOut>(Func<Ok, TOut>, Func<TOut>)` - success-only logic
  - `MatchFail<TOut>(Func<Fail, TOut>, Func<TOut>)` - error-only logic
- [ ] Consider removing stateful variants (TState parameter) - niche use case

**Validation:**
- [ ] All tests updated to use remaining Match overloads
- [ ] Build succeeds

**Decision Point:** Discuss with user whether to simplify or keep all overloads.

**Estimated Time:** 1-2 hours
**Risk:** Low - file move only

---

## Phase 3: Extension Methods - Pattern Matching

**Goal:** Switch all extension method internals from Match callbacks to `is` pattern matching.

### 3.1 Refactor Extension Methods (One File at a Time)

**Pattern to follow:**
```csharp
// OLD (Match callback)
return result.Match<Result<TOut>>(
    ok => mapper(ok.Value),
    fail => fail.Error
);

// NEW (Pattern matching)
if (result is Result<TIn>.Ok ok)
{
    return new Result<TOut>.Ok(mapper(ok.Value));
}
var fail = (Result<TIn>.Fail)result;
return new Result<TOut>.Fail(fail.Error);
```

**Files to update (in order):**
1. [ ] `ResultMapExtensions.cs` - Simple, good starting point
2. [ ] `ResultBindExtensions.cs` - Core operations
3. [ ] `ResultTapExtensions.cs` - Side effects
4. [ ] `ResultEnsureExtensions.cs` - Validations (including EnsureAll)
5. [ ] `ResultTryExtensions.cs` - Exception handling
6. [ ] `ResultErrorExtensions.cs` - Error operations (MapError, OrElse, etc.)
7. [ ] `ResultCombineExtensions.cs` - Zip, Combine
8. [ ] `ResultCollectionExtensions.cs` - Traverse, Sequence, etc.

**Strategy:**
- Update one file at a time
- Run tests after each file
- Commit after each successful file update
- Pattern is straightforward, mostly mechanical

**Validation (after each file):**
- [ ] All tests pass
- [ ] No regressions
- [ ] Code is more readable

**Estimated Time:** 4-6 hours (8 files, ~30-45 min each)
**Risk:** Low - pattern is simple, tests catch any issues

---

## Phase 4: Logging Architecture Refactor

**Goal:** Implement AsyncLocal context, Strategy pattern for timers, CallerMemberName, and full Result context logging.

### 4.1 Create Timer Infrastructure

**New files in `src/Railway.Toolkit/src/Logging/Timing/`:**

1. [ ] `IRailwayTimer.cs` - Interface
   ```csharp
   public interface IRailwayTimer
   {
       IDisposable StartTiming();
       TimeSpan GetElapsed();
       void Reset();
   }
   ```

2. [ ] `NullRailwayTimer.cs` - Zero overhead null object
   - Singleton pattern
   - `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on all methods
   - Returns `TimeSpan.Zero`, `NullDisposable.Instance`

3. [ ] `StopwatchRailwayTimer.cs` - Standard Stopwatch implementation
   - Uses `System.Diagnostics.Stopwatch`

4. [ ] `HighResolutionRailwayTimer.cs` - High precision implementation
   - Uses `Stopwatch` with QueryPerformanceCounter
   - For development/testing scenarios

5. [ ] `SamplingRailwayTimer.cs` - Decorator pattern
   - Wraps another `IRailwayTimer`
   - Only times every Nth operation (configurable)
   - Thread-safe with `Interlocked.Increment`

6. [ ] `RailwayTimerFactory.cs` - Factory pattern
   - `Create(RailwayLoggingOptions)` - creates based on strategy
   - `CreateForEnvironment(string, RailwayLoggingOptions)` - environment-aware
   - Support custom strategy registration

**Validation:**
- [ ] Write unit tests for each timer implementation
- [ ] Test factory pattern
- [ ] Verify zero overhead for NullTimer
- [ ] Test sampling logic

### 4.2 Update RailwayLoggingOptions

**File:** `src/Railway.Toolkit/src/Logging/RailwayLoggingOptions.cs`

Changes:
- [ ] Add `bool LogSuccessValues { get; set; } = false` - control value logging
- [ ] Add `bool LogSlowOperationsOnly { get; set; } = false` - noise reduction
- [ ] Add `double SlowOperationThresholdMs { get; set; } = 100` - threshold
- [ ] Add `int SampleRate { get; set; } = 1` - for sampling strategy
- [ ] Add `string? CustomStrategyName { get; set; }` - custom timers
- [ ] Change `TimingStrategy` to enum (keep for now, factory uses it)
- [ ] Add environment-aware `Default` property
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
              "Staging" => new() { TimingStrategy = TimingStrategy.Sampling, SampleRate = 50 },
              _ => new() { LogTimings = false, TimingStrategy = TimingStrategy.None }
          };
      }
  }
  ```

### 4.3 Refactor RailwayLogging to AsyncLocal

**File:** `src/Railway.Toolkit/src/Logging/RailwayLogging.cs`

Major changes:
- [ ] Replace static fields with `AsyncLocal<T>`:
  ```csharp
  private static readonly AsyncLocal<ILogger> _contextLogger = new();
  private static readonly AsyncLocal<RailwayLoggingOptions> _options = new();
  private static readonly AsyncLocal<IRailwayTimer> _timer = new();
  ```
- [ ] Add `EnableLogging(ILogger, RailwayLoggingOptions?)` method
  - Returns `IDisposable` for scope management
  - Stores previous values, restores on dispose
  - Creates timer from factory
- [ ] Update `Logger` property to read from `AsyncLocal`
- [ ] Update `Options` property to read from `AsyncLocal`
- [ ] Remove `Configure()` method (replaced by EnableLogging)

**Breaking Change:** Yes, but pre-1.0 so acceptable.

### 4.4 Create New IRailwayLogger Interface

**File:** `src/Railway.Toolkit/src/Logging/IRailwayLogger.cs`

New signature:
```csharp
internal interface IRailwayLogger
{
    void LogOperation<TIn, TOut>(
        string operation,
        Result<TIn> input,
        Result<TOut> output,
        TimeSpan elapsed);
}
```

**Key change:** Logger receives full Result context, not status strings.

### 4.5 Update RailwayLogger Implementation

**File:** `src/Railway.Toolkit/src/Logging/RailwayLogger.cs`

Major changes:
- [ ] Implement new `LogOperation<TIn, TOut>` signature
- [ ] Use pattern matching to determine what happened:
  ```csharp
  switch ((input, output))
  {
      case (Result<TIn>.Ok, Result<TOut>.Ok):
          // Success -> Success
      case (Result<TIn>.Ok, Result<TOut>.Fail):
          // Success -> Failure (track switch)
      case (Result<TIn>.Fail, _):
          // Already failed (skipped)
  }
  ```
- [ ] Implement `LogSlowOperationsOnly` filtering
- [ ] Implement `LogSuccessValues` conditional value logging
- [ ] Add structured logging with `ILogger.BeginScope`:
  ```csharp
  var context = new Dictionary<string, object>
  {
      ["Operation"] = operation,
      ["InputType"] = typeof(TIn).Name,
      ["OutputType"] = typeof(TOut).Name,
      ["Success"] = output is Result<TOut>.Ok,
      ["DurationMs"] = elapsed.TotalMilliseconds
  };
  using var scope = logger.BeginScope(context);
  ```
- [ ] Use emoji: 🚂 normal, 🐌 slow, ✓ success
- [ ] `[MethodImpl(MethodImplOptions.AggressiveInlining)]` on hot paths
- [ ] Early exit when logging disabled

### 4.6 Update All Extension Methods for New Logging

**Pattern to follow:**
```csharp
public static Result<TOut> Map<TIn, TOut>(
    this Result<TIn> result,
    Func<TIn, TOut> mapper,
    [CallerMemberName] string operation = "Map")  // NEW: CallerMemberName
{
    using var timing = RailwayLogging.StartOperation();  // NEW: Get from factory

    Result<TOut> output;
    if (result is Result<TIn>.Ok ok)
    {
        output = new Result<TOut>.Ok(mapper(ok.Value));
    }
    else
    {
        var fail = (Result<TIn>.Fail)result;
        output = new Result<TOut>.Fail(fail.Error);
    }

    // NEW: Pass full Result context
    RailwayLogging.Logger?.LogOperation(operation, result, output, timing.GetElapsed());
    return output;
}
```

**Changes for ALL extension methods:**
- [ ] Add `[CallerMemberName] string operation = "MethodName"` parameter
- [ ] Use `RailwayLogging.StartOperation()` for timing (not OperationTimer directly)
- [ ] Call `LogOperation(operation, input, output, elapsed)` with full context
- [ ] Remove manual status string construction

**Files to update (same as Phase 3):**
1. [ ] ResultMapExtensions.cs
2. [ ] ResultBindExtensions.cs
3. [ ] ResultTapExtensions.cs
4. [ ] ResultEnsureExtensions.cs
5. [ ] ResultTryExtensions.cs
6. [ ] ResultErrorExtensions.cs
7. [ ] ResultCombineExtensions.cs
8. [ ] ResultCollectionExtensions.cs

**Strategy:** Can combine with Phase 3 to avoid touching files twice.

**Validation:**
- [ ] All tests pass
- [ ] Manual testing with ILogger to verify log output
- [ ] Verify AsyncLocal context isolation (parallel tests)

**Estimated Time:** 6-8 hours
**Risk:** Medium - significant changes to logging infrastructure

---

## Phase 5: Validation Helpers

**Goal:** Provide common validation extension methods with field-level error support.

### 5.1 Create Validation Extension Methods

**New file:** `src/Railway.Toolkit/src/Extensions/ResultValidationExtensions.cs`

**Implement common validators (all with `fieldName` parameter):**

String validators:
- [ ] `NotNullOrEmpty(this Result<string> result, string fieldName = "Value", [CallerMemberName] string operation = "NotNullOrEmpty")`
- [ ] `NotNullOrWhiteSpace(this Result<string> result, string fieldName, [CallerMemberName] string operation = "NotNullOrWhiteSpace")`
- [ ] `HasMinLength(this Result<string> result, int minLength, string fieldName, [CallerMemberName]...)`
- [ ] `HasMaxLength(this Result<string> result, int maxLength, string fieldName, [CallerMemberName]...)`
- [ ] `MatchesPattern(this Result<string> result, Regex pattern, string fieldName, [CallerMemberName]...)` - for email, etc.

Numeric validators (generic `T where T : IComparable<T>`):
- [ ] `GreaterThan(this Result<T> result, T minimum, string fieldName, [CallerMemberName]...)`
- [ ] `LessThan(this Result<T> result, T maximum, string fieldName, [CallerMemberName]...)`
- [ ] `InRange(this Result<T> result, T min, T max, string fieldName, [CallerMemberName]...)`

Specific numeric:
- [ ] `GreaterThanZero(this Result<decimal> result, string fieldName, [CallerMemberName]...)`
- [ ] `GreaterThanZero(this Result<int> result, string fieldName, [CallerMemberName]...)`

Collection validators:
- [ ] `NotNullOrEmpty<T>(this Result<T> result, Func<T, IEnumerable> selector, string fieldName, [CallerMemberName]...)` where T : class
- [ ] `HasMinCount<T>(this Result<T> result, Func<T, IEnumerable> selector, int minCount, string fieldName, [CallerMemberName]...)`
- [ ] `HasMaxCount<T>(this Result<T> result, Func<T, IEnumerable> selector, int maxCount, string fieldName, [CallerMemberName]...)`

Property validators:
- [ ] `NotNull<T>(this Result<T> result, Func<T, object?> selector, string fieldName, [CallerMemberName]...)`

**Pattern for implementation:**
```csharp
public static Result<string> NotNullOrEmpty(
    this Result<string> result,
    string fieldName = "Value",
    [CallerMemberName] string operation = "NotNullOrEmpty")
{
    using var timing = RailwayLogging.StartOperation();

    var output = result.Ensure(
        value => !string.IsNullOrEmpty(value),
        $"{fieldName} cannot be null or empty",
        "Validation.Required"
    );

    RailwayLogging.Logger?.LogOperation(operation, result, output, timing.GetElapsed());
    return output;
}
```

**Key points:**
- All validators use `Ensure` internally
- All accept `fieldName` parameter (for Details dictionary)
- All use `[CallerMemberName]` for logging
- Error codes use namespaced format: `"Validation.Required"`, `"Validation.Range"`, etc.

### 5.2 Create Validation.Combine Helper

**File:** `src/Railway.Toolkit/src/Validation.cs` (new static class)

Implement:
- [ ] `Combine<T>(IEnumerable<Result<Unit>> validations, Func<T> builder, [CallerMemberName]...)`
  - Runs all validations
  - Collects all errors with Details
  - If any fail, merges Details into single error
  - If all pass, calls builder function

- [ ] `Combine(IEnumerable<Result<Unit>> validations, [CallerMemberName]...)` - returns Unit

**Pattern:**
```csharp
public static Result<T> Combine<T>(
    IEnumerable<Result<Unit>> validations,
    Func<T> builder,
    [CallerMemberName] string operation = "Combine")
{
    using var timing = RailwayLogging.StartOperation();

    var errorBuilder = ErrorDetailsBuilder.Create();
    var hasErrors = false;

    foreach (var validation in validations)
    {
        if (validation is Result<Unit>.Fail fail)
        {
            hasErrors = true;
            // Merge Details from validation error
            if (fail.Error.Details != null)
            {
                foreach (var detail in fail.Error.Details)
                {
                    foreach (var message in detail.Value)
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

    Result<T> output;
    if (hasErrors)
    {
        var error = new Error
        {
            Message = "Validation failed",
            Code = "Validation.Failed",
            Details = errorBuilder.Build()
        };
        output = new Result<T>.Fail(error);
    }
    else
    {
        output = new Result<T>.Ok(builder());
    }

    RailwayLogging.Logger?.LogOperation(operation, validations.FirstOrDefault(), output, timing.GetElapsed());
    return output;
}
```

### 5.3 Write Comprehensive Tests

**Test file:** `src/Railway.Toolkit.Tests/ValidationTests.cs`

Test all validators:
- [ ] Happy path (validation passes)
- [ ] Sad path (validation fails)
- [ ] Error message includes fieldName
- [ ] Error has correct Code
- [ ] CallerMemberName works correctly

Test Validation.Combine:
- [ ] All validations pass - builder called
- [ ] Some validations fail - Details merged
- [ ] Multiple errors per field - array combined
- [ ] Logging works correctly

**Estimated Time:** 6-8 hours
**Risk:** Low - new functionality, doesn't affect existing code

---

## Phase 6: Documentation & Examples

**Goal:** Update documentation to reflect new patterns and capabilities.

### 6.1 Update README.md

Add sections:
- [ ] Structured Error Validation (Details + ErrorDetailsBuilder)
- [ ] Validation Helpers (with field-level examples)
- [ ] Validation.Combine pattern for forms
- [ ] Logging configuration (AsyncLocal, environment-aware defaults)
- [ ] Pattern matching examples (encourage over Match)

### 6.2 Create Migration Guide

**File:** `MIGRATION.md`

Document:
- [ ] Error structure changes (Details property added)
- [ ] Logging changes (AsyncLocal, EnableLogging)
- [ ] Pattern matching encouragement (Match still works)
- [ ] Validation helpers available

### 6.3 Add Code Examples

**Directory:** `examples/` or `samples/`

Create example projects:
- [ ] Form validation example (ASP.NET Core)
- [ ] API validation example (minimal API)
- [ ] Batch processing with error aggregation
- [ ] Logging configuration examples

**Estimated Time:** 4-6 hours
**Risk:** Low

---

## Phase 7: Final Testing & Cleanup

### 7.1 Test Suite Verification

- [ ] Run all 166+ tests
- [ ] Add integration tests for new features
- [ ] Test async scenarios with AsyncLocal
- [ ] Performance smoke tests (logging overhead)

### 7.2 Code Cleanup

- [ ] Remove deprecated/unused code
- [ ] Update all XML documentation
- [ ] Run `dotnet format`
- [ ] Fix any analyzer warnings

### 7.3 Version Bump

- [ ] Update version to 0.9.0 (pre-1.0 major refactor)
- [ ] Update CHANGELOG.md
- [ ] Tag release

**Estimated Time:** 2-3 hours
**Risk:** Low

---

## Summary Timeline

| Phase | Description | Estimated Time | Risk |
|-------|-------------|----------------|------|
| 1 | Error Structure | 2-3 hours | Low |
| 2 | Result DU Refinement | 1-2 hours | Low |
| 3 | Pattern Matching | 4-6 hours | Low |
| 4 | Logging Refactor | 6-8 hours | Medium |
| 5 | Validation Helpers | 6-8 hours | Low |
| 6 | Documentation | 4-6 hours | Low |
| 7 | Testing & Cleanup | 2-3 hours | Low |
| **Total** | | **25-36 hours** | |

**Recommended Approach:**
- Tackle in order (phases build on each other)
- Commit after each phase
- Run full test suite after each phase
- Can combine Phase 3 & 4 (pattern matching + logging) to avoid touching files twice

**Breaking Changes:**
- Logging API (AsyncLocal, EnableLogging) - acceptable pre-1.0
- All extension methods add CallerMemberName parameter - non-breaking (optional parameter)
- Error structure - additive only, non-breaking

**Success Criteria:**
- ✅ All 166+ tests passing
- ✅ No compilation warnings
- ✅ Validation helpers working with field-level errors
- ✅ Logging using AsyncLocal and full Result context
- ✅ Pattern matching used internally
- ✅ Documentation updated

---

## Notes for Future Sessions

**When resuming work:**
1. Review this plan
2. Check current branch status
3. Identify which phase you're in
4. Run tests before starting
5. Follow phase checklist
6. Commit after completing each phase

**Key Design Decisions Made:**
- Error codes stay as `string` (not enum) for flexibility
- Error gets `Details` property for field-level validation
- ErrorDetailsBuilder for convenience (not required)
- Result files merged (Result.Generated.cs → Result.cs)
- Match methods kept for compatibility but discouraged
- Pattern matching used internally for all extensions
- Logging uses AsyncLocal, Strategy pattern, CallerMemberName
- All extension methods get CallerMemberName parameter

**References:**
- Principles: `.claude/claude.md`
- Sample code: `Sample/` directory (RailwayKit examples)
- Current tests: `src/Railway.Toolkit.Tests/`
