using BenchmarkDotNet.Attributes;

namespace Railway.Toolkit;

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class CollectionBenchmarks
{
    private readonly Error _error = Error.Create("Invalid", "Benchmark.Invalid");
    private int[] _values = null!;
    private Result<int>[] _success = null!, _early = null!, _late = null!, _failures = null!;
    private Task<Result<int>>[] _tasks = null!, _earlyTasks = null!, _lateTasks = null!;
    private Func<int, Result<int>> _earlySelector = null!, _lateSelector = null!, _failureSelector = null!;
    [Params(8, 128)] public int Count { get; set; }
    [GlobalSetup]
    public void Setup()
    {
        _values = Enumerable.Range(0, Count).ToArray();
        _success = _values.Select(Result.Ok).ToArray();
        _early = (Result<int>[])_success.Clone();
        _late = (Result<int>[])_success.Clone();
        _early[0] = Result.Fail<int>(_error);
        _late[^1] = Result.Fail<int>(_error);
        _failures = _values.Select(_ => Result.Fail<int>(_error)).ToArray();
        _tasks = _success.Select(Task.FromResult).ToArray();
        _earlyTasks = _early.Select(Task.FromResult).ToArray();
        _lateTasks = _late.Select(Task.FromResult).ToArray();
        _earlySelector = value => _early[value];
        _lateSelector = value => _late[value];
        _failureSelector = value => _failures[value];
    }
    [Benchmark] public Result<IReadOnlyList<int>> TraverseSuccess() => _values.Traverse(static value => Result.Ok(value));
    [Benchmark] public Result<IReadOnlyList<int>> TraverseEarlyFailure() => _values.Traverse(_earlySelector);
    [Benchmark] public Result<IReadOnlyList<int>> TraverseLateFailure() => _values.Traverse(_lateSelector);
    [Benchmark] public Result<IReadOnlyList<int>> TraverseAllErrors() => _values.TraverseAll(_failureSelector);
    [Benchmark] public Result<IReadOnlyList<int>> SequenceSuccess() => _success.Sequence();
    [Benchmark] public Result<IReadOnlyList<int>> SequenceEarlyFailure() => _early.Sequence();
    [Benchmark] public Result<IReadOnlyList<int>> SequenceLateFailure() => _late.Sequence();
    [Benchmark] public Result<IReadOnlyList<int>> SequenceAllErrors() => _failures.SequenceAll();
    [Benchmark] public Result<IReadOnlyList<int>> CombineSuccess() => ResultCombineExtensions.Combine(_success);
    [Benchmark] public Result<IReadOnlyList<int>> CombineAllErrors() => ResultCombineExtensions.CombineAll(_failures);
    [Benchmark] public Task<Result<IReadOnlyList<int>>> AsyncTraverseSuccess() => _values.TraverseAsync(static value => Task.FromResult(Result.Ok(value)));
    [Benchmark] public Task<Result<IReadOnlyList<int>>> AsyncSequenceSuccess() => _tasks.SequenceAsync();
    [Benchmark] public Task<Result<IReadOnlyList<int>>> AsyncSequenceEarlyFailure() => _earlyTasks.SequenceAsync();
    [Benchmark] public Task<Result<IReadOnlyList<int>>> AsyncSequenceLateFailure() => _lateTasks.SequenceAsync();

    internal void Verify()
    {
        foreach (Result<IReadOnlyList<int>> result in new[] { TraverseSuccess(), SequenceSuccess(), CombineSuccess(), AsyncTraverseSuccess().GetAwaiter().GetResult(), AsyncSequenceSuccess().GetAwaiter().GetResult() })
        {
            ExpandedVerification.Require(result is Result<IReadOnlyList<int>>.Ok ok && ok.Value.SequenceEqual(_values), "Collection values/order");
        }
        foreach (Result<IReadOnlyList<int>> result in new[] { TraverseEarlyFailure(), TraverseLateFailure(), SequenceEarlyFailure(), SequenceLateFailure(), AsyncSequenceEarlyFailure().GetAwaiter().GetResult(), AsyncSequenceLateFailure().GetAwaiter().GetResult() })
        {
            ExpandedVerification.Require(result is Result<IReadOnlyList<int>>.Fail fail && ReferenceEquals(fail.Error, _error), "Collection first error");
        }
        foreach (Result<IReadOnlyList<int>> result in new[] { TraverseAllErrors(), SequenceAllErrors(), CombineAllErrors() })
        {
            ExpandedVerification.Require(result is Result<IReadOnlyList<int>>.Fail fail && fail.Error.InnerErrors?.Count == Count, "Collection aggregate errors");
        }
    }
}

[MemoryDiagnoser, BenchmarkCategory("Expanded")]
public class ZipBenchmarks
{
    private Result<int> _first = null!;
    private readonly Result<int> _second = Result.Ok(2);
    [Params(false, true)] public bool Failure { get; set; }
    [GlobalSetup] public void Setup() => _first = Failure ? Result.Fail<int>("Invalid", "Benchmark.Invalid") : Result.Ok(1);
    [Benchmark] public Result<(int, int)> Pair() => _first.Zip(_second);
    [Benchmark] public Result<int> Selector() => _first.Zip(_second, static (first, second) => first + second);
    internal void Verify()
    {
        ExpandedVerification.CheckTrack(Pair(), !Failure);
        ExpandedVerification.CheckTrack(Selector(), !Failure);
        if (!Failure) ExpandedVerification.Require(Selector() is Result<int>.Ok { Value: 3 }, "Zip selector output");
    }
}
