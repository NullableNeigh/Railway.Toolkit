using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class PipelineShapeBenchmarks
{
    private Result<int> _value = null!;
    private Result<string> _reference = null!;
    [Params(1, 16)] public int Steps { get; set; }
    [Params(false, true)] public bool Failure { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        Error error = Error.Create("Invalid", "Benchmark.Invalid");
        _value = Failure ? Result.Fail<int>(error) : Result.Ok(10);
        _reference = Failure ? Result.Fail<string>(error) : Result.Ok("payload");
    }
    [Benchmark]
    public Result<int> ValuePayload()
    {
        Result<int> result = _value;
        for (int step = 0; step < Steps; step++) result = result.Map(static value => value);
        return result;
    }
    [Benchmark]
    public Result<string> ReferencePayload()
    {
        Result<string> result = _reference;
        for (int step = 0; step < Steps; step++) result = result.Map(static value => value);
        return result;
    }
    internal void Verify()
    {
        ExpandedVerification.CheckTrack(ValuePayload(), !Failure);
        ExpandedVerification.CheckTrack(ReferencePayload(), !Failure);
        if (!Failure) ExpandedVerification.Require(ValuePayload() is Result<int>.Ok { Value: 10 } && ReferencePayload() is Result<string>.Ok { Value: "payload" }, "Payload preservation");
    }
}
