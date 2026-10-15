namespace Railway.Toolkit.Tests;

// These tests control cancellation independently of the test runner.
#pragma warning disable xUnit1051

[Trait("Category", "Cancellation")]
public class RemainingCancellationTests
{
    private static readonly Error Failure = Error.Create("Failure", "Test.Failure");
    private static readonly Error Mapped = Error.Create("Mapped", "Test.Mapped");

    public static TheoryData<string> Callbacks => new TheoryData<string>
    {
        "Bind", "Map", "Ensure", "Tap", "MapErrorSync", "TapErrorSync",
        "MapErrorDirect", "TapErrorDirect", "MapErrorTask", "TapErrorTask"
    };

    public static TheoryData<string> TaskCallbacks => new TheoryData<string>
    {
        "Bind", "Map", "Ensure", "Tap", "MapErrorSync", "TapErrorSync", "MapErrorTask", "TapErrorTask"
    };

    private static bool ErrorTrack(string operation) => operation.Contains("Error", StringComparison.Ordinal);

    private static Task<Result<int>> Run(string operation, Task<Result<int>> incoming,
        Action<CancellationToken> callback, CancellationToken token) => operation switch
    {
        "Bind" => incoming.BindAsync((int value, CancellationToken received) => { callback(received); return Result.Ok(value + 1); }, token),
        "Map" => incoming.MapAsync((int value, CancellationToken received) => { callback(received); return value + 1; }, token),
        "Ensure" => incoming.EnsureAsync((value, received) => { callback(received); return false; }, Failure, token),
        "Tap" => incoming.TapAsync((value, received) => callback(received), token),
        "MapErrorSync" => incoming.MapErrorAsync((Error error, CancellationToken received) => { callback(received); return Mapped; }, token),
        "TapErrorSync" => incoming.TapErrorAsync((error, received) => callback(received), token),
        "MapErrorDirect" => Result.Fail<int>(Failure).MapErrorAsync((error, received) => { callback(received); return Task.FromResult(Mapped); }, token),
        "TapErrorDirect" => Result.Fail<int>(Failure).TapErrorAsync((error, received) => { callback(received); return Task.CompletedTask; }, token),
        "MapErrorTask" => incoming.MapErrorAsync((error, received) => { callback(received); return Task.FromResult(Mapped); }, token),
        "TapErrorTask" => incoming.TapErrorAsync((error, received) => { callback(received); return Task.CompletedTask; }, token),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    [Theory]
    [MemberData(nameof(Callbacks))]
    public async Task SelectedCallbacks_ForwardTokenAndProduceExpectedOutput(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        int calls = 0;
        Result<int> input = ErrorTrack(operation) ? Result.Fail<int>(Failure) : Result.Ok(2);
        Result<int> output = await Run(operation, Task.FromResult(input), token =>
        {
            Assert.Equal(source.Token, token);
            calls++;
        }, source.Token);
        Assert.Equal(1, calls);
        if (operation.StartsWith("MapError", StringComparison.Ordinal))
            Assert.Same(Mapped, Assert.IsType<Result<int>.Fail>(output).Error);
        else if (operation == "Ensure")
            Assert.Same(Failure, Assert.IsType<Result<int>.Fail>(output).Error);
        else if (ErrorTrack(operation))
            Assert.Same(Failure, Assert.IsType<Result<int>.Fail>(output).Error);
        else
            Assert.Equal(operation == "Tap" ? 2 : 3, Assert.IsType<Result<int>.Ok>(output).Value);
    }

    [Theory]
    [MemberData(nameof(Callbacks))]
    public async Task SelectedCallbacks_PreCancellationSkipsWork(string operation)
    {
        bool called = false;
        Result<int> input = ErrorTrack(operation) ? Result.Fail<int>(Failure) : Result.Ok(2);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(operation, Task.FromResult(input), _ => called = true, new CancellationToken(true)));
        Assert.False(called);
    }

    [Theory]
    [MemberData(nameof(Callbacks))]
    public async Task SelectedCallbacks_CancellationDuringNormalCallbackPreventsOutputAndLogging(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger);
        Result<int> input = ErrorTrack(operation) ? Result.Fail<int>(Failure) : Result.Ok(2);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(operation, Task.FromResult(input), _ => source.Cancel(), source.Token));
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [MemberData(nameof(Callbacks))]
    public async Task SelectedCallbacks_OrdinaryCallbackFaultWinsOverCancellation(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        InvalidOperationException expected = new InvalidOperationException("callback");
        Result<int> input = ErrorTrack(operation) ? Result.Fail<int>(Failure) : Result.Ok(2);
        Exception? actual = await Record.ExceptionAsync(() => Run(operation, Task.FromResult(input), _ =>
        {
            source.Cancel();
            throw expected;
        }, source.Token));
        Assert.Same(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TaskCallbacks))]
    public async Task TaskCallbacks_AwaitNormallyThenCancellationWinsBeforeEitherTrack(string operation)
    {
        foreach (bool fail in new[] { false, true })
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            TaskCompletionSource<Result<int>> incoming = new TaskCompletionSource<Result<int>>();
            bool called = false;
            Task pending = Run(operation, incoming.Task, _ => called = true, source.Token);
            source.Cancel();
            Assert.False(pending.IsCompleted);
            incoming.SetResult(fail ? Result.Fail<int>(Failure) : Result.Ok(1));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            Assert.False(called);
        }
    }

    [Theory]
    [MemberData(nameof(TaskCallbacks))]
    public async Task TaskCallbacks_IncomingFaultIsPreserved(string operation)
    {
        InvalidOperationException expected = new InvalidOperationException("incoming");
        bool called = false;
        Exception? actual = await Record.ExceptionAsync(() => Run(operation, Task.FromException<Result<int>>(expected), _ => called = true, new CancellationToken(true)));
        Assert.Same(expected, actual);
        Assert.False(called);
    }

    [Fact]
    public async Task FailureTrack_DirectOkBypassesCancellationAndCallbacks()
    {
        Result<int> input = Result.Ok(1);
        CancellationToken token = new CancellationToken(true);
        Assert.Same(input, await input.MapErrorAsync((error, received) => throw new InvalidOperationException(), token));
        Assert.Same(input, await input.TapErrorAsync((error, received) => throw new InvalidOperationException(), token));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FailureTrack_AsyncCallbackIsAwaitedNormally(bool map, bool taskReceiver)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Error> work = new TaskCompletionSource<Error>();
        Result<int> input = Result.Fail<int>(Failure);
        Task pending = map
            ? taskReceiver ? Task.FromResult(input).MapErrorAsync((error, token) => work.Task, source.Token) : input.MapErrorAsync((error, token) => work.Task, source.Token)
            : taskReceiver ? Task.FromResult(input).TapErrorAsync((error, token) => work.Task, source.Token) : input.TapErrorAsync((error, token) => work.Task, source.Token);
        source.Cancel();
        Assert.False(pending.IsCompleted);
        work.SetResult(Mapped);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task ValidationPrecedesCancellationAndIncomingAwait()
    {
        Task<Result<int>> incoming = new TaskCompletionSource<Result<int>>().Task;
        CancellationToken token = new CancellationToken(true);
        await Assert.ThrowsAsync<ArgumentNullException>(() => incoming.BindAsync((Func<int, CancellationToken, Result<int>>)null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => incoming.MapAsync((Func<int, CancellationToken, int>)null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => incoming.EnsureAsync((value, received) => true, null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => incoming.TapAsync((Action<int, CancellationToken>)null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => incoming.MapErrorAsync((Func<Error, CancellationToken, Error>)null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => incoming.TapErrorAsync((Action<Error, CancellationToken>)null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Fail<int>(Failure).MapErrorAsync((Func<Error, CancellationToken, Task<Error>>)null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => incoming.TapErrorAsync((Func<Error, CancellationToken, Task>)null!, token));
    }

    private static Task<int> Match(string shape, Task<Result<int>> incoming, Result<int> direct,
        Func<bool, CancellationToken, Task<int>> handler, CancellationToken token) => shape switch
    {
        "Direct" => direct.MatchAsync((value, received) => handler(true, received), (error, received) => handler(false, received), token),
        "Task" => incoming.MatchAsync((value, received) => handler(true, received), (error, received) => handler(false, received), token),
        "Sync" => incoming.MatchAsync((value, received) => handler(true, received).GetAwaiter().GetResult(), (error, received) => handler(false, received).GetAwaiter().GetResult(), token),
        _ => throw new ArgumentOutOfRangeException(nameof(shape))
    };

    [Theory]
    [InlineData("Direct", false)]
    [InlineData("Direct", true)]
    [InlineData("Task", false)]
    [InlineData("Task", true)]
    [InlineData("Sync", false)]
    [InlineData("Sync", true)]
    public async Task Matching_SelectsOneHandlerAndObservesCompletion(string shape, bool fail)
    {
        Result<int> input = fail ? Result.Fail<int>(Failure) : Result.Ok(1);
        using CancellationTokenSource source = new CancellationTokenSource();
        int calls = 0;
        Task<int> Handler(bool ok, CancellationToken received)
        {
            Assert.Equal(!fail, ok);
            Assert.Equal(source.Token, received);
            calls++;
            return Task.FromResult(7);
        }
        Assert.Equal(7, await Match(shape, Task.FromResult(input), input, Handler, source.Token));
        Assert.Equal(1, calls);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Match(shape, Task.FromResult(input), input, (ok, received) =>
        {
            source.Cancel();
            return Task.FromResult(7);
        }, source.Token));
        calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Match(shape, Task.FromResult(input), input, Handler, source.Token));
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("Task")]
    [InlineData("Sync")]
    public async Task Matching_AwaitsIncomingThenSkipsEitherHandler(string shape)
    {
        foreach (bool fail in new[] { false, true })
        {
            using CancellationTokenSource source = new CancellationTokenSource();
            TaskCompletionSource<Result<int>> incoming = new TaskCompletionSource<Result<int>>();
            bool called = false;
            Task pending = Match(shape, incoming.Task, Result.Ok(1), (ok, token) => { called = true; return Task.FromResult(1); }, source.Token);
            source.Cancel();
            Assert.False(pending.IsCompleted);
            incoming.SetResult(fail ? Result.Fail<int>(Failure) : Result.Ok(1));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            Assert.False(called);
        }
    }

    [Theory]
    [InlineData("Direct")]
    [InlineData("Task")]
    public async Task Matching_AwaitsHandlerNormallyAndPreservesFaults(string shape)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<int> work = new TaskCompletionSource<int>();
        Task pending = Match(shape, Task.FromResult(Result.Ok(1)), Result.Ok(1), (ok, token) => work.Task, source.Token);
        source.Cancel();
        Assert.False(pending.IsCompleted);
        InvalidOperationException expected = new InvalidOperationException("handler");
        work.SetException(expected);
        Assert.Same(expected, await Record.ExceptionAsync(() => pending));
    }

    [Fact]
    public async Task Matching_ValidatesBothHandlersBeforeAwaitOrCancellation()
    {
        Task<Result<int>> pending = new TaskCompletionSource<Result<int>>().Task;
        CancellationToken token = new CancellationToken(true);
        await Assert.ThrowsAsync<ArgumentNullException>(() => pending.MatchAsync((Func<Result<int>.Ok, CancellationToken, int>)null!, (error, received) => 1, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => pending.MatchAsync((value, received) => Task.FromResult(1), (Func<Result<int>.Fail, CancellationToken, Task<int>>)null!, token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Result.Ok(1).MatchAsync((value, received) => Task.FromResult(1), (Func<Result<int>.Fail, CancellationToken, Task<int>>)null!, token));
    }

    private static Task<Result<int>> Finally(Task<Result<int>> incoming, bool asynchronous, Action cleanup) => asynchronous
        ? incoming.FinallyAsync(() => { cleanup(); return Task.CompletedTask; })
        : incoming.FinallyAsync(cleanup);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Finally_RunsExactlyOnceForEveryIncomingOutcome(bool asynchronous)
    {
        InvalidOperationException expected = new InvalidOperationException("incoming");
        Task<Result<int>>[] inputs = { Task.FromResult(Result.Ok(1)), Task.FromResult(Result.Fail<int>(Failure)), Task.FromException<Result<int>>(expected), Task.FromCanceled<Result<int>>(new CancellationToken(true)) };
        foreach (Task<Result<int>> input in inputs)
        {
            int calls = 0;
            Task<Result<int>> pending = Finally(input, asynchronous, () => calls++);
            if (input.IsCompletedSuccessfully) Assert.Same(await input, await pending);
            else if (input.IsFaulted) Assert.Same(expected, await Record.ExceptionAsync(() => pending));
            else await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            Assert.Equal(1, calls);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Finally_CleanupExceptionWinsOverEveryIncomingOutcome(bool asynchronous)
    {
        InvalidOperationException expected = new InvalidOperationException("cleanup");
        Task<Result<int>>[] inputs = { Task.FromResult(Result.Ok(1)), Task.FromResult(Result.Fail<int>(Failure)), Task.FromException<Result<int>>(new Exception("incoming")), Task.FromCanceled<Result<int>>(new CancellationToken(true)) };
        foreach (Task<Result<int>> input in inputs)
            Assert.Same(expected, await Record.ExceptionAsync(() => Finally(input, asynchronous, () => throw expected)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Finally_WaitsForIncomingBeforeCleanup(bool asynchronous)
    {
        TaskCompletionSource<Result<int>> incoming = new TaskCompletionSource<Result<int>>();
        int calls = 0;
        Task pending = Finally(incoming.Task, asynchronous, () => calls++);
        Assert.Equal(0, calls);
        incoming.SetCanceled();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Finally_AwaitsCleanupAfterIncomingCancellationOrFault(bool fault)
    {
        TaskCompletionSource<bool> cleanup = new TaskCompletionSource<bool>();
        Exception expected = new InvalidOperationException("incoming");
        Task<Result<int>> input = fault ? Task.FromException<Result<int>>(expected) : Task.FromCanceled<Result<int>>(new CancellationToken(true));
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger);
        Task pending = input.FinallyAsync(() => cleanup.Task);
        Assert.False(pending.IsCompleted);
        cleanup.SetResult(true);
        if (fault) Assert.Same(expected, await Record.ExceptionAsync(() => pending));
        else await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Finally_RunsAfterGovernedCancellationAndCanSignalItsOwnCancellation()
    {
        int calls = 0;
        Task<Result<int>> cancelled = Task.FromResult(Result.Ok(1)).MapAsync((int value, CancellationToken token) => value, new CancellationToken(true));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.FinallyAsync(() => calls++));
        Assert.Equal(1, calls);
        CancellationToken cleanupToken = new CancellationToken(true);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromException<Result<int>>(new Exception("incoming")).FinallyAsync(() => Task.FromCanceled(cleanupToken)));
        Assert.Equal(cleanupToken, exception.CancellationToken);
    }

    [Theory]
    [InlineData("Direct", false)]
    [InlineData("Direct", true)]
    [InlineData("Task", false)]
    [InlineData("Task", true)]
    public async Task Matching_CancellationAfterNormalAsyncHandlerCompletionWins(string shape, bool fail)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<int> work = new TaskCompletionSource<int>();
        Result<int> input = fail ? Result.Fail<int>(Failure) : Result.Ok(1);
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger);
        Task pending = Match(shape, Task.FromResult(input), input, (ok, token) => work.Task, source.Token);
        source.Cancel();
        Assert.False(pending.IsCompleted);
        work.SetResult(5);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData("Direct")]
    [InlineData("Task")]
    [InlineData("Sync")]
    public async Task Matching_OrdinaryHandlerFaultWinsOverCancellation(string shape)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        InvalidOperationException expected = new InvalidOperationException("handler");
        Exception? actual = await Record.ExceptionAsync(() => Match(shape, Task.FromResult(Result.Fail<int>(Failure)), Result.Fail<int>(Failure), (ok, received) =>
        {
            source.Cancel();
            throw expected;
        }, source.Token));
        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData("Task")]
    [InlineData("Sync")]
    public async Task Matching_IncomingFaultOrCancellationSkipsHandlers(string shape)
    {
        InvalidOperationException expected = new InvalidOperationException("incoming");
        int calls = 0;
        Task<int> Handler(bool ok, CancellationToken token) { calls++; return Task.FromResult(1); }
        Assert.Same(expected, await Record.ExceptionAsync(() => Match(shape, Task.FromException<Result<int>>(expected), Result.Ok(1), Handler, new CancellationToken(true))));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Match(shape, Task.FromCanceled<Result<int>>(new CancellationToken(true)), Result.Ok(1), Handler, default));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task TokenlessFailureCallbacksAndMatchingRetainTheirBehaviour()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        int calls = 0;
        Result<int> input = Result.Fail<int>(Failure);
        Result<int> mapped = await Task.FromResult(input).MapErrorAsync(error => { calls++; return Mapped; });
        await mapped.TapErrorAsync(error => { calls++; return Task.CompletedTask; });
        Assert.Equal(2, calls);
        Assert.Equal(7, await Task.FromResult(mapped).MatchAsync(ok => 0, fail => 7));
        OperationCanceledException expected = new OperationCanceledException();
        Assert.Same(expected, await Record.ExceptionAsync(() => input.MapErrorAsync(error => Task.FromException<Error>(expected))));
    }

    [Theory]
    [MemberData(nameof(TaskCallbacks))]
    public async Task InactiveTrackSkipsCallbacksWithoutCancellation(string operation)
    {
        Result<int> input = ErrorTrack(operation) ? Result.Ok(2) : Result.Fail<int>(Failure);
        bool called = false;
        Result<int> output = await Run(operation, Task.FromResult(input), _ => called = true, default);
        Assert.False(called);
        if (ErrorTrack(operation)) Assert.Equal(2, Assert.IsType<Result<int>.Ok>(output).Value);
        else Assert.Same(Failure, Assert.IsType<Result<int>.Fail>(output).Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Finally_InvalidCallbackDoesNotStartWaiting(bool asynchronous)
    {
        Task<Result<int>> pending = new TaskCompletionSource<Result<int>>().Task;
        if (asynchronous)
            await Assert.ThrowsAsync<ArgumentNullException>(() => pending.FinallyAsync((Func<Task>)null!));
        else
            await Assert.ThrowsAsync<ArgumentNullException>(() => pending.FinallyAsync((Action)null!));
    }


    [Theory]
    [MemberData(nameof(TaskCallbacks))]
    public async Task IncomingCancellationSkipsCallbacks(string operation)
    {
        bool called = false;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(operation, Task.FromCanceled<Result<int>>(new CancellationToken(true)), _ => called = true, default));
        Assert.False(called);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FailureTrack_AsyncFaultWinsOverCancellation(bool map, bool taskReceiver)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Error> work = new TaskCompletionSource<Error>();
        Result<int> input = Result.Fail<int>(Failure);
        Task pending = map
            ? taskReceiver ? Task.FromResult(input).MapErrorAsync((error, token) => work.Task, source.Token) : input.MapErrorAsync((error, token) => work.Task, source.Token)
            : taskReceiver ? Task.FromResult(input).TapErrorAsync((error, token) => work.Task, source.Token) : input.TapErrorAsync((error, token) => work.Task, source.Token);
        source.Cancel();
        InvalidOperationException expected = new InvalidOperationException("callback fault");
        work.SetException(expected);
        Assert.Same(expected, await Record.ExceptionAsync(() => pending));
    }

}

#pragma warning restore xUnit1051
