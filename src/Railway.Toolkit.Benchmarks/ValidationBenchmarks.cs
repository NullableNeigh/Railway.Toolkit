using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class ValidationBenchmarks
{
    private readonly Error _error = Error.Create("Invalid", "Benchmark.Invalid");
    private Result<int> _input = null!;
    private (Func<int, bool> Predicate, Error Error)[] _rules = null!;
    [Params(false, true)] public bool Pass { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        _input = Result.Ok(Pass ? 10 : -1);
        _rules = new (Func<int, bool>, Error)[] { (static value => value > 0, _error), (static value => value < 100, _error) };
    }
    [Benchmark] public Result<int> FixedError() => _input.Ensure(static value => value > 0, _error);
    [Benchmark] public Result<int> ErrorFactory() => _input.Ensure(static value => value > 0, static _ => Error.Create("Invalid", "Benchmark.Invalid"));
    [Benchmark] public Result<int> MultipleRules() => _input.EnsureAll(_rules);
    [Benchmark] public Task<Result<int>> AsyncPredicate() => _input.EnsureAsync(static value => Task.FromResult(value > 0), _error);
    internal void Verify()
    {
        foreach (Result<int> result in new[] { FixedError(), ErrorFactory(), MultipleRules(), AsyncPredicate().GetAwaiter().GetResult() })
        {
            ExpandedVerification.CheckTrack(result, Pass);
        }
    }
}

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class CombinedValidationBenchmarks
{
    private Result<Unit>[] _checks = null!;
    [Params(2, 16)] public int Count { get; set; }
    [Params(false, true)] public bool Failures { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        _checks = Enumerable.Range(0, Count).Select(index => Failures
            ? Result.Fail<Unit>(new Error { Message = "Invalid", Code = "Benchmark.Invalid", Details = new Dictionary<string, string[]> { [index.ToString()] = new[] { "Invalid" } } })
            : Result.Ok()).ToArray();
    }
    [Benchmark] public Result<Unit> CollectValidationErrors() => Validation.Combine(_checks);
    internal void Verify()
    {
        Result<Unit> result = CollectValidationErrors();
        ExpandedVerification.CheckTrack(result, !Failures);
        if (Failures) ExpandedVerification.Require(result is Result<Unit>.Fail fail && fail.Error.Details?.Count == Count, "Collected validation details");
    }
}
