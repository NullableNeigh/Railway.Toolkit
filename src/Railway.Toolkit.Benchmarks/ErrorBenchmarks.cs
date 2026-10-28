using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class ErrorConstructionBenchmarks
{
    private Error[] _errors = null!;
    private string[] _fields = null!;
    [Params(2, 16)] public int Count { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        _fields = Enumerable.Range(0, Count).Select(index => $"Field{index}").ToArray();
        _errors = _fields.Select(field => new Error { Message = "Invalid", Code = "Benchmark.Invalid", Details = new Dictionary<string, string[]> { [field] = new[] { "Invalid" } } }).ToArray();
    }
    [Benchmark] public Error AggregateErrors() => Error.Aggregate(_errors);
    [Benchmark]
    public Error CreateDetails()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        foreach (string field in _fields) builder.AddDetail(field, "Invalid");
        return new Error { Message = "Invalid", Code = "Benchmark.Invalid", Details = builder.Build() };
    }
    internal void Verify()
    {
        ExpandedVerification.Require(AggregateErrors().InnerErrors?.Count == Count, "Error aggregation");
        ExpandedVerification.Require(CreateDetails().Details?.Count == Count, "Error details");
    }
}

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class ErrorTransformBenchmarks
{
    private Result<int> _input = null!;
    [Params(false, true)] public bool Failure { get; set; }
    [GlobalSetup] public void Setup() => _input = Failure ? Result.Fail<int>("Invalid", "Benchmark.Invalid") : Result.Ok(10);
    [Benchmark] public Result<int> MapError() => _input.MapError(static error => Error.Create("Context", error.Code));
    [Benchmark] public Task<Result<int>> AsyncMapError() => _input.MapErrorAsync(static error => Task.FromResult(Error.Create("Context", error.Code)));
    internal void Verify()
    {
        ExpandedVerification.CheckTrack(MapError(), !Failure);
        ExpandedVerification.CheckTrack(AsyncMapError().GetAwaiter().GetResult(), !Failure);
    }
}
