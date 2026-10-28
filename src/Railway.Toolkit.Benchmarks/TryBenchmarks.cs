using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class TryBenchmarks
{
    private readonly Result<int> _success = Result.Ok(10);
    private readonly Result<int> _failure = Result.Fail<int>("Missing", "Benchmark.Missing");
    private Task<Result<int>> _task = null!;
    [GlobalSetup] public void Setup() => _task = Task.FromResult(_success);

    [Benchmark] public Result<int> NoException() => ResultTryExtensions.Try(static () => 11);
    [Benchmark] public Result<int> CaughtException() => ResultTryExtensions.Try<int>(Throw);
    [Benchmark] public Result<int> CustomExceptionMapper() => ResultTryExtensions.Try<int>(Throw,
        exceptionMapper: static exception => new Error { Message = "Conversion failed", Code = "Benchmark.Conversion", Exception = exception });
    [Benchmark] public Result<int> SkippedFailure() => _failure.TryMap(static value => value + 1);
    [Benchmark] public Task<Result<int>> CompletedAsyncSuccess() => ResultTryExtensions.TryAsync(static () => Task.FromResult(11));
    [Benchmark] public Task<Result<int>> CaughtAsyncException() => ResultTryExtensions.TryAsync<int>(static () => Task.FromException<int>(new FormatException("Invalid")));
    [Benchmark] public Task<Result<int>> FluentTryMap() => _task.TryMapAsync(static value => value + 1);
    [Benchmark] public Task<Result<int>> FluentTryBind() => _task.TryBindAsync(static value => Task.FromResult(Result.Ok(value + 1)));

    private static int Throw() => throw new FormatException("Invalid");
    internal void Verify()
    {
        foreach (Result<int> result in new[] { NoException(), CompletedAsyncSuccess().GetAwaiter().GetResult(), FluentTryMap().GetAwaiter().GetResult(), FluentTryBind().GetAwaiter().GetResult() })
        {
            ExpandedVerification.Require(result is Result<int>.Ok { Value: 11 }, "Try success output");
        }
        foreach (Result<int> result in new[] { CaughtException(), CustomExceptionMapper(), CaughtAsyncException().GetAwaiter().GetResult(), SkippedFailure() })
        {
            ExpandedVerification.CheckTrack(result, false);
        }
        ExpandedVerification.Require(CustomExceptionMapper() is Result<int>.Fail { Error.Code: "Benchmark.Conversion" }, "Custom exception mapping");
    }
}
