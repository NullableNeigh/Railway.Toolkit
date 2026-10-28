using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser]
public class AsyncBenchmarks
{
    private readonly Error _error = Error.Create("Missing", "Benchmark.Missing");
    private Task<Result<int>> _input = null!;

    [Params(false, true)]
    public bool Failure { get; set; }

    [GlobalSetup]
    public void Setup() => _input = Task.FromResult(Failure ? Result.Fail<int>(_error) : Result.Ok(10));

    [Benchmark]
    public Task<Result<int>> CompletedMap() => _input.MapAsync(static value => value + 1);

    [Benchmark]
    public Task<Result<int>> CompletedBind() => _input.BindAsync(static value => Task.FromResult(Result.Ok(value + 1)));

    [Benchmark]
    public Task<Result<int>> CompletedPipeline() => _input
        .BindAsync(static value => Task.FromResult(Result.Ok(value + 1)))
        .MapAsync(static value => value * 2);

    [Benchmark]
    public Task<int> PlainCompletedPipeline() => Task.FromResult(Failure ? -1 : (10 + 1) * 2);

    [Benchmark]
    public Task<Result<int>> YieldingPipeline() => _input
        .BindAsync(YieldAndIncrement)
        .MapAsync(static value => value * 2);

    [Benchmark]
    public async Task<int> PlainYieldingPipeline()
    {
        if (Failure)
        {
            return -1;
        }
        await Task.Yield();
        return (10 + 1) * 2;
    }

    private static async Task<Result<int>> YieldAndIncrement(int value)
    {
        await Task.Yield();
        return Result.Ok(value + 1);
    }
}
