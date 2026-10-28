using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class FallbackBenchmarks
{
    private readonly Error _error = Error.Create("Missing", "Benchmark.Missing");
    private Result<int> _input = null!;
    private Task<Result<int>> _task = null!;
    [Params(false, true)] public bool Failure { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _input = Failure ? Result.Fail<int>(_error) : Result.Ok(10);
        _task = Task.FromResult(_input);
    }

    [Benchmark] public Result<int> ValueFallback() => _input.OrElse(static _ => 7);
    [Benchmark] public Result<int> ResultFallback() => _input.OrElseWith(static _ => Result.Ok(7));
    [Benchmark] public Result<int> FailedAlternative() => _input.OrElseWith(static error => Result.Fail<int>(error));
    [Benchmark] public Task<Result<int>> AsyncValueFallback() => _input.OrElseAsync(static _ => Task.FromResult(7));
    [Benchmark] public Task<Result<int>> AsyncResultFallback() => _input.OrElseWithAsync(static _ => Task.FromResult(Result.Ok(7)));
    [Benchmark] public Task<Result<int>> TaskValueFallback() => _task.OrElseAsync(static _ => 7);
    [Benchmark] public Task<Result<int>> TaskResultFallback() => _task.OrElseWithAsync(static _ => Result.Ok(7));

    internal void Verify()
    {
        foreach (Result<int> result in new[] { ValueFallback(), ResultFallback(), AsyncValueFallback().GetAwaiter().GetResult(), AsyncResultFallback().GetAwaiter().GetResult(), TaskValueFallback().GetAwaiter().GetResult(), TaskResultFallback().GetAwaiter().GetResult() })
        {
            ExpandedVerification.Require(result is Result<int>.Ok ok && ok.Value == (Failure ? 7 : 10), "Fallback output");
        }
        ExpandedVerification.CheckTrack(FailedAlternative(), !Failure);
    }
}
