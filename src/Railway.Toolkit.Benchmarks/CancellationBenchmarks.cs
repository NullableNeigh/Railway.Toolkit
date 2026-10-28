using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class CancellationBenchmarks
{
    private Result<int> _input = null!;
    [Params(false, true)] public bool Failure { get; set; }
    [GlobalSetup] public void Setup() => _input = Failure ? Result.Fail<int>("Invalid", "Benchmark.Invalid") : Result.Ok(10);
    [Benchmark] public Task<Result<int>> Tokenless() => _input.MapAsync(static value => Task.FromResult(value + 1));
    [Benchmark] public Task<Result<int>> UncancelledToken() => _input.MapAsync(static (value, token) => Task.FromResult(value + 1), CancellationToken.None);
    internal void Verify()
    {
        ExpandedVerification.CheckTrack(Tokenless().GetAwaiter().GetResult(), !Failure);
        ExpandedVerification.CheckTrack(UncancelledToken().GetAwaiter().GetResult(), !Failure);
    }
}

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class CancellationExceptionBenchmarks
{
    private readonly Result<int> _input = Result.Ok(10);
    private readonly CancellationToken _cancelled = new CancellationToken(true);
    private Task<Result<int>> _task = null!;
    [GlobalSetup] public void Setup() => _task = Task.FromResult(_input);
    [Benchmark] public Task<bool> BeforeDirectCallback() => Capture(_input.MapAsync(static (value, token) => Task.FromResult(value + 1), _cancelled));
    [Benchmark] public Task<bool> AfterIncomingTask() => Capture(_task.MapAsync(static (value, token) => Task.FromResult(value + 1), _cancelled));
    private static async Task<bool> Capture(Task<Result<int>> operation)
    {
        try { await operation.ConfigureAwait(false); return false; }
        catch (OperationCanceledException) { return true; }
    }
    internal void Verify() => ExpandedVerification.Require(BeforeDirectCallback().GetAwaiter().GetResult() && AfterIncomingTask().GetAwaiter().GetResult(), "Cancellation boundaries");
}
