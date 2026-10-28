using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class ObservationBenchmarks
{
    private Result<int> _input = null!;
    private Task<Result<int>> _task = null!;
    [Params(false, true)] public bool Failure { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        _input = Failure ? Result.Fail<int>("Invalid", "Benchmark.Invalid") : Result.Ok(10);
        _task = Task.FromResult(_input);
    }
    [Benchmark] public Result<int> Tap() => _input.Tap(static _ => { });
    [Benchmark] public Result<int> TapError() => _input.TapError(static _ => { });
    [Benchmark] public Result<int> Finally() => _input.Finally(static () => { });
    [Benchmark] public Task<Result<int>> AsyncTap() => _input.TapAsync(static _ => Task.CompletedTask);
    [Benchmark] public Task<Result<int>> TaskFinally() => _task.FinallyAsync(static () => Task.CompletedTask);
    internal void Verify()
    {
        foreach (Result<int> result in new[] { Tap(), TapError(), Finally(), AsyncTap().GetAwaiter().GetResult(), TaskFinally().GetAwaiter().GetResult() })
        {
            ExpandedVerification.Require(ReferenceEquals(_input, result), "Observation result preservation");
        }
    }
}
