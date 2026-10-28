using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class LoggingFailureBenchmarks
{
    private readonly Result<int> _success = Result.Ok(10);
    private readonly Result<int> _failure = Result.Fail<int>("Invalid", "Benchmark.Invalid");
    private readonly Error _error = Error.Create("Invalid", "Benchmark.Invalid");
    private LoggingBenchmarks _logging = null!;
    [Params(false, true)] public bool Enabled { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        _logging = new LoggingBenchmarks { Mode = Enabled ? BenchmarkLoggingMode.EnabledUntimed : BenchmarkLoggingMode.Absent };
        _logging.Setup();
    }
    [Benchmark] public Result<int> NewFailure() => _success.Ensure(static _ => false, _error);
    [Benchmark] public Result<int> PropagatedFailure() => _failure.Map(static value => value + 1);
    [Benchmark] public Result<int> Recovery() => _failure.OrElse(7);
    [GlobalCleanup] public void Cleanup() => _logging.Cleanup();
    internal void Verify()
    {
        ExpandedVerification.CheckTrack(NewFailure(), false);
        ExpandedVerification.CheckTrack(PropagatedFailure(), false);
        ExpandedVerification.Require(Recovery() is Result<int>.Ok { Value: 7 }, "Logged recovery output");
        ExpandedVerification.Require(_logging.EventCount == (Enabled ? 3 : 0), "Failure/recovery logging events");
    }
}
