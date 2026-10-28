using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser]
public class ResultBenchmarks
{
    private readonly Error _error = Error.Create("Missing", "Benchmark.Missing");
    private readonly Result<int> _success = Result.Ok(42);
    private readonly Result<int> _failure = Result.Fail<int>("Missing", "Benchmark.Missing");

    [Benchmark]
    public Result<int> CreateSuccess() => Result.Ok(42);

    [Benchmark]
    public Result<int> CreateFailureWithExistingError() => Result.Fail<int>(_error);

    [Benchmark]
    public Result<int> CreateFailureWithNewError() => Result.Fail<int>("Missing", "Benchmark.Missing");

    [Benchmark]
    public int MatchSuccess() => _success.Match(static ok => ok.Value, static fail => -1);

    [Benchmark]
    public int MatchFailure() => _failure.Match(static ok => ok.Value, static fail => -1);

    [Benchmark, BenchmarkCategory("Expanded")]
    public int PatternMatchSuccess() => _success is Result<int>.Ok ok ? ok.Value : -1;

    [Benchmark, BenchmarkCategory("Expanded")]
    public int PatternMatchFailure() => _failure is Result<int>.Ok ok ? ok.Value : -1;
}
