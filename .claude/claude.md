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

## Common Tasks


