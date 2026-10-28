using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser]
public class CompositionBenchmarks
{
    private readonly Error _error = Error.Create("Missing", "Benchmark.Missing");
    private Result<int> _input = null!;

    [Params(false, true)]
    public bool Failure { get; set; }

    [GlobalSetup]
    public void Setup() => _input = Failure ? Result.Fail<int>(_error) : Result.Ok(10);

    [Benchmark]
    public Result<int> Map() => _input.Map(static value => value + 1);

    [Benchmark]
    public Result<int> Bind() => _input.Bind(static value => Result.Ok(value * 2));

    [Benchmark]
    public Result<int> RailwayPipeline() => _input
        .Map(static value => value + 1)
        .Bind(static value => Result.Ok(value * 2))
        .Ensure(static value => value > 0, _error);

    [Benchmark]
    public (bool Success, int Value, Error? Error) PlainPipeline()
    {
        if (Failure)
        {
            return (false, 0, _error);
        }
        int mapped = 10 + 1;
        int bound = mapped * 2;
        return bound > 0 ? (true, bound, null) : (false, 0, _error);
    }
}
