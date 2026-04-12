# Error Object — Scenarios & Usage Guide

## The Four Properties

```
Error
├── Message       string         — Human-readable description of what went wrong
├── Code          string         — Machine-readable identifier (namespaced recommended)
├── Details       dict?          — Field-level validation messages (null if not applicable)
├── InnerErrors   list<Error>?   — Multiple distinct errors rolled into one (null if not applicable)
└── Exception     Exception?     — The underlying exception, if one caused this error
```

`Details` and `InnerErrors` are both "multiple errors" mechanisms but they serve different purposes. That's the part worth understanding clearly.

---

## Scenario 1 — Simple Error

The most common case. Something went wrong, one reason.

```csharp
// Creating
Error error = Error.Create("User not found", "User.NotFound");

// Reading
Console.WriteLine(error.Message);   // "User not found"
Console.WriteLine(error.Code);      // "User.NotFound"
// error.Details      → null
// error.InnerErrors  → null
// error.Exception    → null
```

**When to use:** Any single failure — database miss, business rule violation, unauthorised access.

---

## Scenario 2 — Exception Wrapping

Something threw unexpectedly. You want to capture it in the railway pipeline without losing the original exception.

```csharp
try
{
    // ... something threw
}
catch (Exception ex)
{
    Error error = Error.FromException(ex, "Database.ConnectionFailed");
}

// Reading
Console.WriteLine(error.Message);    // ex.Message
Console.WriteLine(error.Code);       // "Database.ConnectionFailed"
Console.WriteLine(error.Exception);  // the original SqlException, stack trace intact
// error.Details     → null
// error.InnerErrors → null
```

**When to use:** Wrapping exceptions at the boundary (Try extension, external calls). Preserves the original exception for logging/debugging without leaking it to callers.

---

## Scenario 3 — Field-Level Validation (`Details`)

A form or API request fails validation. You want to tell the caller *which fields* failed and *why*, so the UI can highlight the right inputs.

```csharp
// Building
ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
builder.AddDetail("Email", "Invalid email format");
builder.AddDetail("Email", "Email already registered");  // same field, second message
builder.AddDetail("Age", "Must be at least 18");

Error error = new Error
{
    Message = "Validation failed",
    Code = "Validation.Failed",
    Details = builder.Build()
};

// Reading
if (error.Details != null)
{
    foreach (KeyValuePair<string, string[]> field in error.Details)
    {
        Console.WriteLine($"{field.Key}: {string.Join(", ", field.Value)}");
    }
}
// Email: Invalid email format, Email already registered
// Age: Must be at least 18
```

**What `Details` gives you:** Each field maps to an *array* of messages because a single field can fail multiple rules simultaneously. A UI can light up each input with all its specific errors at once.

**When to use:** Any validation that maps to specific fields — form validation, API request validation, domain model invariants with named properties.

**`Details` is null** on all other error types. `if (error.Details != null)` reliably means "this is a field-level validation error."

---

## Scenario 4 — Batch Operations (`InnerErrors`)

You're processing a collection (e.g. importing 100 records) and want to collect *all* failures rather than stopping at the first one. Each failure is a distinct, independent error — not related to a single object's fields.

```csharp
List<Error> failures = new List<Error>();

foreach (Order order in orders)
{
    Result<Order> result = ProcessOrder(order);
    if (result is Result<Order>.Fail fail)
    {
        failures.Add(fail.Error);
    }
}

if (failures.Count > 0)
{
    Error aggregated = Error.Aggregate(failures);
    // aggregated.Message     → "3 errors occurred: Order 12 failed; Order 47 failed; ..."
    // aggregated.Code        → "AggregateError"
    // aggregated.InnerErrors → [ error1, error2, error3 ]  ← each original error intact
    // aggregated.Details     → null  (these aren't field-level validation errors)
}
```

**Reading inner errors:**

```csharp
if (aggregated.InnerErrors != null)
{
    foreach (Error inner in aggregated.InnerErrors)
    {
        Console.WriteLine($"[{inner.Code}] {inner.Message}");
    }
}
```

**When to use:** Batch processing, collecting results across a collection, rolling up multiple pipeline failures into one return value.

**`InnerErrors` is null** on simple errors. `if (error.InnerErrors != null)` reliably means "this is an aggregate of multiple distinct errors."

---

## Scenario 5 — Aggregate With Field-Level Details (Mixed)

You're aggregating errors where *some* of them are field-level validation errors (have `Details`). `Error.Aggregate` merges only the `Details` from those that have them.

```csharp
Error emailError = new Error
{
    Message = "Email validation failed",
    Code = "Validation.Failed",
    Details = builder1.Build()  // { "Email": ["Invalid format"] }
};

Error ageError = new Error
{
    Message = "Age validation failed",
    Code = "Validation.Failed",
    Details = builder2.Build()  // { "Age": ["Must be >= 18"] }
};

Error infrastructureError = Error.Create("Cache unavailable", "Cache.Miss");
// ↑ no Details

Error aggregated = Error.Aggregate(new[] { emailError, ageError, infrastructureError });

// aggregated.InnerErrors → all three errors
// aggregated.Details     → { "Email": ["Invalid format"], "Age": ["Must be >= 18"] }
//                           ↑ only merged from errors that had Details
//                           ↑ infrastructureError contributed nothing to Details
```

**Note:** `Validation.Combine` (Phase 5) handles the *pure validation* version of this — it's designed specifically for collecting field-level failures. `Error.Aggregate` is the general-purpose tool.

---

## Decision Tree for Users

```
What kind of error is this?

One thing went wrong?
  → Error.Create(message, code)

An exception was thrown?
  → Error.FromException(ex, code)

A form/object failed field validation?
  → new Error { ..., Details = builder.Build() }
  → Check Details != null to detect this case

Multiple independent operations failed?
  → Error.Aggregate(errors)
  → Check InnerErrors != null to detect this case

Multiple validations on one object (collect all failures)?
  → Validation.Combine(...)  ← Phase 5
```

---

## What This Means for Users of the Library

**Rendering API validation errors (ASP.NET Core):**
```csharp
if (result is Result<T>.Fail fail && fail.Error.Details != null)
{
    return ValidationProblem(fail.Error.Details);  // maps directly to ProblemDetails
}
```

**Logging:**
```csharp
// Simple error      — log Message + Code
// Exception error   — log with Exception for stack trace
// Aggregate error   — log summary + iterate InnerErrors
// Field-level error — log Details for context
```

**Displaying to end users:**
- Simple/exception errors → show `Message` or a generic "something went wrong"
- Field-level errors → map `Details` keys to form inputs, show each array of messages
- Aggregate errors → show count, optionally list each `InnerError.Message`

---

## Key Distinction

**`Details`** = what's wrong with which fields on *one object*

**`InnerErrors`** = multiple separate things that each went wrong *independently*
