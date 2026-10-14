using System.Collections;

namespace Railway.Toolkit.Tests;

// Cancellation is controlled by each test rather than the runner.
#pragma warning disable xUnit1051

[Trait("Category", "Cancellation")]
public class UnifiedCancellationTests
{
    private static readonly Error Failure = Error.Create("Failed", "Test.Failed");

    public static TheoryData<string> Collections => new TheoryData<string> { "Sequence", "SequenceAll", "Combine", "CombineAll" };

    private static Task<Result<IReadOnlyList<int>>> Run(string operation, Task<Result<int>>[] tasks, CancellationToken token) => operation switch
    {
        "Sequence" => tasks.SequenceAsync(token),
        "SequenceAll" => tasks.SequenceAllAsync(token),
        "Combine" => ResultCombineExtensions.CombineAsync(token, tasks),
        "CombineAll" => ResultCombineExtensions.CombineAllAsync(token, tasks),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    private static Task<Result<IReadOnlyList<int>>> Sequence(bool all, IEnumerable<Task<Result<int>>> tasks, CancellationToken token) =>
        all ? tasks.SequenceAllAsync(token) : tasks.SequenceAsync(token);

    [Theory]
    [MemberData(nameof(Collections))]
    public async Task Collection_NormalResultsPreserveOrderingAndErrorPolicy(string operation)
    {
        Task<Result<int>>[] values = { Task.FromResult(Result.Ok(3)), Task.FromResult(Result.Ok(1)) };
        Result<IReadOnlyList<int>> success = await Run(operation, values, default);
        Assert.Equal(new[] { 3, 1 }, Assert.IsType<Result<IReadOnlyList<int>>.Ok>(success).Value);
        Task<Result<int>>[] failures = { Task.FromResult(Result.Fail<int>(Failure)), Task.FromResult(Result.Fail<int>(Failure)) };
        Error error = Assert.IsType<Result<IReadOnlyList<int>>.Fail>(await Run(operation, failures, default)).Error;
        if (operation.EndsWith("All", StringComparison.Ordinal))
            Assert.Equal(2, error.InnerErrors!.Count);
        else
            Assert.Same(Failure, error);
        Assert.Empty(Assert.IsType<Result<IReadOnlyList<int>>.Ok>(await Run(operation, Array.Empty<Task<Result<int>>>(), default)).Value);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(operation, Array.Empty<Task<Result<int>>>(), new CancellationToken(true)));
    }

    [Theory]
    [MemberData(nameof(Collections))]
    public async Task Collection_WaitsNormallyThenCancellationWinsOverOkAndFail(string operation)
    {
        foreach (bool fail in new[] { false, true })
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            InMemoryLogger logger = new InMemoryLogger();
            using IDisposable scope = RailwayLogging.EnableLogging(logger);
            TaskCompletionSource<Result<int>> work = new TaskCompletionSource<Result<int>>();
            Task<Result<IReadOnlyList<int>>> pending = Run(operation, new[] { work.Task }, source.Token);
            source.Cancel();
            Assert.False(pending.IsCompleted);
            work.SetResult(fail ? Result.Fail<int>(Failure) : Result.Ok(1));
            OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            Assert.Equal(source.Token, exception.CancellationToken);
            Assert.Empty(logger.Entries);
        }
    }

    [Theory]
    [MemberData(nameof(Collections))]
    public async Task Collection_OrdinaryAwaitFaultWinsOverRequestedCancellation(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Result<int>> work = new TaskCompletionSource<Result<int>>();
        Task pending = Run(operation, new[] { work.Task }, source.Token);
        source.Cancel();
        InvalidOperationException expected = new InvalidOperationException("work fault");
        work.SetException(expected);
        Assert.Same(expected, await Record.ExceptionAsync(() => pending));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sequence_LazyEnumerationStartingWorkDuringCancellationStillAwaitsIt(bool all)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Result<int>> work = new TaskCompletionSource<Result<int>>();
        bool disposed = false;
        int steps = 0;
        IEnumerable<Task<Result<int>>> Tasks()
        {
            try
            {
                steps++;
                source.Cancel();
                yield return work.Task;
                steps++;
                yield return Task.FromResult(Result.Ok(2));
            }
            finally { disposed = true; }
        }
        Task pending = Sequence(all, Tasks(), source.Token);
        Assert.False(pending.IsCompleted);
        Assert.False(disposed);
        work.SetResult(Result.Fail<int>(Failure));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, steps);
        Assert.True(disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sequence_PreCancelledDoesNotObtainEnumerator(bool all)
    {
        TaskSource source = new TaskSource(() => throw new InvalidOperationException("must not enumerate"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Sequence(all, source, new CancellationToken(true)));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Sequence_ObservesTerminalMoveNextAndDisposalCancellation(bool all, bool dispose)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskSource tasks = new TaskSource(() => { }, dispose ? null : source.Cancel, dispose ? source.Cancel : null);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Sequence(all, tasks, source.Token));
        Assert.True(tasks.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sequence_DoesNotDrainAlreadyStartedSiblingAfterCancellation(bool all)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Result<int>> first = new TaskCompletionSource<Result<int>>();
        TaskCompletionSource<Result<int>> sibling = new TaskCompletionSource<Result<int>>();
        Task pending = Sequence(all, new[] { first.Task, sibling.Task }, source.Token);
        source.Cancel();
        first.SetResult(Result.Ok(1));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.False(sibling.Task.IsCompleted);
        sibling.SetException(new InvalidOperationException("caller-owned sibling"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sibling.Task);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Combine_WaitsEverySiblingAndPreservesWhenAllFaultPrecedence(bool all)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Result<int>> sibling = new TaskCompletionSource<Result<int>>();
        Task pending = Run(all ? "CombineAll" : "Combine", new[] { Task.FromResult(Result.Fail<int>(Failure)), sibling.Task }, source.Token);
        source.Cancel();
        Assert.False(pending.IsCompleted);
        InvalidOperationException expected = new InvalidOperationException("sibling fault");
        sibling.SetException(expected);
        Assert.Same(expected, await Record.ExceptionAsync(() => pending));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Zip_CancellationAfterFirstAwaitLeavesSecondToCaller(bool selector)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Result<int>> first = new TaskCompletionSource<Result<int>>();
        TaskCompletionSource<Result<int>> second = new TaskCompletionSource<Result<int>>();
        Task pending = selector
            ? first.Task.ZipAsync<int, int, int>(second.Task, (int a, int b, CancellationToken _) => throw new InvalidOperationException("selector must not run"), source.Token)
            : first.Task.ZipAsync(second.Task, source.Token);
        source.Cancel();
        Assert.False(pending.IsCompleted);
        first.SetResult(Result.Fail<int>(Failure));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.False(second.Task.IsCompleted);
        second.SetResult(Result.Ok(2));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Zip_CancellationDuringSecondAwaitWinsOverEitherTrack(bool failure)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Result<int>> second = new TaskCompletionSource<Result<int>>();
        Task pending = Task.FromResult(Result.Ok(1)).ZipAsync(second.Task, source.Token);
        source.Cancel();
        Assert.False(pending.IsCompleted);
        second.SetResult(failure ? Result.Fail<int>(Failure) : Result.Ok(2));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task Zip_SelectorReceivesTokenAndChecksAfterNormalReturnButPreservesFault()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger);
        Task<Result<int>> first = Task.FromResult(Result.Ok(1));
        Task<Result<int>> second = Task.FromResult(Result.Ok(2));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.ZipAsync(second, (a, b, token) =>
        {
            Assert.Equal(source.Token, token);
            source.Cancel();
            return a + b;
        }, source.Token));
        Assert.Empty(logger.Entries);
        using CancellationTokenSource faultSource = new CancellationTokenSource();
        InvalidOperationException expected = new InvalidOperationException("selector fault");
        Task<Result<int>> pending = first.ZipAsync<int, int, int>(second, (_, _, _) =>
        {
            faultSource.Cancel();
            throw expected;
        }, faultSource.Token);
        Assert.Same(expected, await Record.ExceptionAsync(() => pending));
        Assert.Empty(logger.Entries);
        Assert.Equal(3, Assert.IsType<Result<int>.Ok>(await first.ZipAsync(second, (a, b, _) => a + b, default)).Value);
        Assert.Equal((1, 2), Assert.IsType<Result<(int, int)>.Ok>(await first.ZipAsync(second, default)).Value);
    }

    [Fact]
    public async Task CollectionValidationPrecedesCancellation()
    {
        CancellationToken token = new CancellationToken(true);
        await Assert.ThrowsAsync<ArgumentNullException>(() => ResultCollectionExtensions.SequenceAsync<int>(null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ResultCollectionExtensions.SequenceAllAsync<int>(null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ResultCombineExtensions.CombineAsync<int>(token, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ResultCombineExtensions.CombineAllAsync<int>(token, null!));
        await Assert.ThrowsAsync<ArgumentException>(() => ResultCombineExtensions.CombineAsync<int>(token, new Task<Result<int>>[] { null! }));
        await Assert.ThrowsAsync<ArgumentException>(() => ResultCombineExtensions.CombineAllAsync<int>(token, new Task<Result<int>>[] { null! }));
        Task<Result<int>> never = new TaskCompletionSource<Result<int>>().Task;
        await Assert.ThrowsAsync<ArgumentNullException>(() => never.ZipAsync<int, int>(null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => never.ZipAsync<int, int, int>(never, null!, token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResultReturningDelegates_CancellationWinsOverReturnedFailure(bool tryBoundary)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Result<int>> work = new TaskCompletionSource<Result<int>>();
        Task pending = tryBoundary
            ? Result.Ok(1).TryBindAsync((_, _) => work.Task, source.Token)
            : Result.Ok(1).BindAsync((_, _) => work.Task, source.Token);
        source.Cancel();
        Assert.False(pending.IsCompleted);
        work.SetResult(Result.Fail<int>(Failure));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Traversal_LastFailureAndTerminalDisposalObserveCancellation(bool all)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        Func<int, CancellationToken, Task<Result<int>>> selector = (_, _) =>
        {
            source.Cancel();
            return Task.FromResult(Result.Fail<int>(Failure));
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => all
            ? new[] { 1 }.TraverseAllAsync(selector, source.Token)
            : new[] { 1 }.TraverseAsync(selector, source.Token));
        using CancellationTokenSource disposed = new CancellationTokenSource();
        IEnumerable<int> Items()
        {
            try { yield return 1; }
            finally { disposed.Cancel(); }
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => all
            ? Items().TraverseAllAsync((value, _) => Task.FromResult(Result.Ok(value)), disposed.Token)
            : Items().TraverseAsync((value, _) => Task.FromResult(Result.Ok(value)), disposed.Token));
    }

    [Fact]
    public async Task FanOutWorkflow_PassesGoverningTokenWhenCreatingWork()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        List<CancellationToken> tokens = new List<CancellationToken>();
        Task<Result<int>> Fetch(int id, CancellationToken token)
        {
            tokens.Add(token);
            return Task.FromResult(Result.Ok(id));
        }
        Result<int> output = await Fetch(1, source.Token)
            .ZipAsync(Fetch(2, source.Token), (a, b, token) => { tokens.Add(token); return a + b; }, source.Token)
            .MapAsync((value, token) => { tokens.Add(token); return Task.FromResult(value * 2); }, source.Token);
        Assert.Equal(6, Assert.IsType<Result<int>.Ok>(output).Value);
        Assert.Equal(4, tokens.Count);
        Assert.All(tokens, token => Assert.Equal(source.Token, token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Zip_FirstFailureStillAwaitsSecondAndSkipsSelector(bool selector)
    {
        TaskCompletionSource<Result<int>> second = new TaskCompletionSource<Result<int>>();
        Task<Result<int>> first = Task.FromResult(Result.Fail<int>(Failure));
        Task pending = selector
            ? first.ZipAsync<int, int, int>(second.Task, (_, _, _) => throw new InvalidOperationException("must skip"), default)
            : first.ZipAsync(second.Task, default);
        Assert.False(pending.IsCompleted);
        second.SetResult(Result.Ok(2));
        await pending;
        Error actual = selector
            ? Assert.IsType<Result<int>.Fail>(await (Task<Result<int>>)pending).Error
            : Assert.IsType<Result<(int, int)>.Fail>(await (Task<Result<(int, int)>>)pending).Error;
        Assert.Same(Failure, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Zip_PreCancelledStillAwaitsFirstAndRetainsItsFault(bool fault)
    {
        TaskCompletionSource<Result<int>> first = new TaskCompletionSource<Result<int>>();
        Task pending = first.Task.ZipAsync(Task.FromResult(Result.Ok(2)), new CancellationToken(true));
        Assert.False(pending.IsCompleted);
        if (fault)
        {
            InvalidOperationException expected = new InvalidOperationException("first fault");
            first.SetException(expected);
            Assert.Same(expected, await Record.ExceptionAsync(() => pending));
        }
        else
        {
            first.SetResult(Result.Ok(1));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Combine_MixedFaultAndCancellationRetainsWhenAllFault(bool all)
    {
        TaskCompletionSource<Result<int>> cancelled = new TaskCompletionSource<Result<int>>();
        InvalidOperationException expected = new InvalidOperationException("group fault");
        Task pending = Run(all ? "CombineAll" : "Combine",
            new[] { Task.FromException<Result<int>>(expected), cancelled.Task }, default);
        Assert.False(pending.IsCompleted);
        cancelled.SetCanceled();
        Assert.Same(expected, await Record.ExceptionAsync(() => pending));
        Assert.True(pending.IsFaulted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sequence_GetEnumeratorCancellationDisposesWithoutMoveNext(bool all)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskSource tasks = new TaskSource(source.Cancel, () => throw new InvalidOperationException("must not advance"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Sequence(all, tasks, source.Token));
        Assert.True(tasks.Disposed);
    }

    private sealed class TaskSource : IEnumerable<Task<Result<int>>>, IEnumerator<Task<Result<int>>>
    {
        private readonly Action _get;
        private readonly Action? _move;
        private readonly Action? _dispose;
        public TaskSource(Action get, Action? move = null, Action? dispose = null) { _get = get; _move = move; _dispose = dispose; }
        public bool Disposed { get; private set; }
        public Task<Result<int>> Current => throw new InvalidOperationException();
        object IEnumerator.Current => Current;
        public IEnumerator<Task<Result<int>>> GetEnumerator() { _get(); return this; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool MoveNext() { _move?.Invoke(); return false; }
        public void Reset() => throw new NotSupportedException();
        public void Dispose() { Disposed = true; _dispose?.Invoke(); }
    }
}

#pragma warning restore xUnit1051
