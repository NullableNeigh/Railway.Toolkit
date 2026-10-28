using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class IncomingTaskBenchmarks
{
    private Result<int> _input = null!;
    private Task<Result<int>> _completed = null!;
    [Params(false, true)] public bool Failure { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        _input = Failure ? Result.Fail<int>("Invalid", "Benchmark.Invalid") : Result.Ok(10);
        _completed = Task.FromResult(_input);
    }
    [Benchmark] public Task<Result<int>> CompletedIncomingTask() => _completed.MapAsync(static value => value + 1);
    [Benchmark]
    public Task<Result<int>> PendingIncomingTask()
    {
        TaskCompletionSource<Result<int>> source = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Result<int>> operation = source.Task.MapAsync(static value => value + 1);
        ThreadPool.QueueUserWorkItem(static state => state.Source.SetResult(state.Input), (Source: source, Input: _input), preferLocal: false);
        return operation;
    }
    internal void Verify()
    {
        ExpandedVerification.CheckTrack(CompletedIncomingTask().GetAwaiter().GetResult(), !Failure);
        ExpandedVerification.CheckTrack(PendingIncomingTask().GetAwaiter().GetResult(), !Failure);
        if (!Failure) ExpandedVerification.Require(PendingIncomingTask().GetAwaiter().GetResult() is Result<int>.Ok { Value: 11 }, "Pending task output");
    }
}
