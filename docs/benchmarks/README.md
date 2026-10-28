# Railway.Toolkit benchmarks

`src/Railway.Toolkit.Benchmarks` is a non-packable net8.0 console project referencing core
and BenchmarkDotNet 0.15.8. Benchmark dependencies do not flow into the core package.
Run from the repository root with consistent runtime/machine conditions. The suite
contains 141 representative cases; running it generates local reports.

```powershell
dotnet run -c Release --project src/Railway.Toolkit.Benchmarks -- --verify
dotnet run -c Release --project src/Railway.Toolkit.Benchmarks -- --list flat

# Run the short measurement profile.
dotnet run -c Release --project src/Railway.Toolkit.Benchmarks -- --filter '*' --job short --iterationTime 100

# Run only the expanded families, or select a class with --filter '*CollectionBenchmarks*'.
dotnet run -c Release --project src/Railway.Toolkit.Benchmarks -- --anyCategories Expanded --job short --iterationTime 100

# Longer controlled sampling before close comparisons or publication.
dotnet run -c Release --project src/Railway.Toolkit.Benchmarks -- --filter '*'
```

All classes use MemoryDiagnoser. Returned values/tasks are consumed by BenchmarkDotNet.
Raw CSV, GitHub Markdown and HTML reports appear beneath ignored BenchmarkDotNet.Artifacts.
Generated reports remain local and ignored by Git. Save the source revision, environment
and exact sampling profile when comparing runs. Do not commit generated reports, harnesses
or binaries.
Unrelated methods have no shared Ratio baseline. `--verify` checks expected results,
collection ordering/aggregation, matching alternatives and enabled logging event counts.

## Coverage and workload boundaries

| Area | Representative coverage |
|---|---|
| Results | Creation with reused/new errors; Match and C# pattern matching on both tracks. |
| Composition | Map, Bind, successful pipelines and failure short-circuiting. |
| Fallback | OrElse/OrElseWith, successful bypass, successful/failed alternatives, async callbacks and task receivers. |
| Try | No exception, thrown/faulted-task exception, custom mapper, bypassed failure, fluent task Map/Bind. |
| Validation | Passing/failing Ensure, fixed/factory error, multiple rules, async predicate and collected details. |
| Collections | 8/128 items; Traverse, Sequence, Combine, Zip, early/late failure, collect-all and async processing. |
| Errors | Aggregation/structured detail construction at 2/16 errors/fields; sync/async MapError and bypass. |
| Observation | Tap, TapError, Finally, async Tap and task cleanup on both railway tracks. |
| Cancellation | Tokenless/uncancelled mapping on both tracks; requested cancellation before callbacks/after incoming tasks. |
| Pipeline shape | 1/16 identity Map steps; int and reused string payloads; both tracks. |
| Tasks | Completed and yielding pipelines, plus a guaranteed-pending incoming-task boundary. |
| Logging | Absent, disabled, provider-disabled, enabled untimed/timestamp/Stopwatch; new/propagated failures and recovery. |

Inputs, reusable errors/results/tasks, selector delegates and logging scopes are prepared
outside measurement, except where their construction is the explicit workload:

- The plain synchronous pipeline returns a tuple; plain async comparisons return an int
  with -1 as failure. Their constant arithmetic is intentionally cheap. These are lower
  bounds with simpler contracts, not equivalent libraries or representative applications.
- TraverseSuccess constructs results per element; early/late selectors reuse fixture
  results. Compare early against late to isolate position; cross-scenario allocation
  differences also include result construction. Async traversal constructs completed
  tasks/results; async sequencing consumes pre-created tasks. Collected validations
  consume pre-computed validation results; the Ensure cases measure predicates separately.
- Try exception cases construct exceptions per invocation; the sync case throws directly,
  while the async case returns a faulted task. Error factory/detail/aggregate workloads
  intentionally measure construction. No exception-to-normal-path subtraction is implied.
- Observation uses no-op callbacks to isolate plumbing. Reference payloads are reused;
  identity pipelines measure result wrapping, not payload creation or business logic.
- Cancellation exception cases include throwing, catching and async completion overhead.
  They are separate from the ordinary uncancelled-token comparison. No request is fired
  from a timer and no wall-clock cancellation latency is claimed.
- PendingIncomingTask starts the railway await before queueing completion. Its measured
  work includes TaskCompletionSource allocation, thread-pool queueing and scheduling.
  Task.Yield workloads also include scheduler costs; neither represents external I/O.
- Logging uses a counting sink without provider I/O, message rendering or retained scopes.
  Success logging measures three operations; failure/recovery cases measure one operation
  each with logging absent or enabled untimed. Scope setup/disposal is not timed.

## Comparing runs

The short profile is for initial measurements and investigation: one launch, three warmups,
three measured iterations and 100 ms target iteration time. Reports include uncertainty;
small/overlapping differences and scheduler-dependent samples need longer reruns.
Compare the same methods, parameters and profile on unchanged hardware/runtime settings.
Use allocations alongside time. Extremely small values near harness overhead may not
be distinguishable from an empty method. Disclose workload warnings and concurrent work.

The workload suite is representative, not every overload/configuration combination. Add cases
when new functionality or concrete performance concerns appear. Optimization is a separate
reviewed change; this project establishes measurements without changing core behaviour.
