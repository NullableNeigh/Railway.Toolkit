using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Cancellation")]
public class CancellationSemanticsTests
{
    private static readonly Error TestError = Error.Create("Test failure", "Test.Failure");

    [Fact]
    public async Task TryAsync_WithMatchingRequestedToken_PropagatesCancellation()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Task<Result<int>> operation = ResultTryExtensions.TryAsync(async () =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, source.Token);
            return 42;
        });

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.True(operation.IsCanceled);
    }

    [Fact]
    public async Task TryAsync_WithRequestedToken_PropagatesCancellation()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Task<Result<int>> operation = ResultTryExtensions.TryAsync<int>(
            () => throw new OperationCanceledException(source.Token));

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(source.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task TryAsync_WithUnrelatedToken_PropagatesCancellation()
    {
        using CancellationTokenSource capturedSource = new CancellationTokenSource();
        using CancellationTokenSource exceptionSource = new CancellationTokenSource();
        capturedSource.Cancel();
        exceptionSource.Cancel();

        Task<Result<int>> operation = ResultTryExtensions.TryAsync<int>(() =>
        {
            _ = capturedSource.Token;
            throw new OperationCanceledException(exceptionSource.Token);
        });

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(exceptionSource.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task TryAsync_WithUnrequestedToken_PropagatesCancellation()
    {
        using CancellationTokenSource source = new CancellationTokenSource();

        Task<Result<int>> operation = ResultTryExtensions.TryAsync<int>(
            () => throw new OperationCanceledException(source.Token));

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.False(source.IsCancellationRequested);
    }

    [Fact]
    public async Task TryAsync_WithDefaultToken_PropagatesCancellation()
    {
        Task<Result<int>> operation = ResultTryExtensions.TryAsync<int>(
            () => throw new OperationCanceledException());

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.False(exception.CancellationToken.CanBeCanceled);
    }

    [Fact]
    public async Task TryAsync_ActionAndTaskCanceledException_PropagateCancellation()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Task<Result<Unit>> actionOperation = ResultTryExtensions.TryAsync(
            () => Task.FromCanceled(source.Token));
        Task<Result<int>> functionOperation = ResultTryExtensions.TryAsync(
            () => Task.FromCanceled<int>(source.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => actionOperation);
        await Assert.ThrowsAsync<TaskCanceledException>(() => functionOperation);
    }

    [Fact]
    public void SynchronousTryOverloads_PropagateCancellation()
    {
        Assert.Throws<OperationCanceledException>(() =>
            ResultTryExtensions.Try<int>(() => throw new OperationCanceledException()));
        Assert.Throws<OperationCanceledException>(() =>
            ResultTryExtensions.Try(() => throw new OperationCanceledException()));
    }

    [Fact]
    public void SynchronousTryMapAndTryBind_PropagateCancellation()
    {
        Result<int> result = Result.Ok(42);

        Assert.Throws<OperationCanceledException>(() =>
            result.TryMap<int, int>(_ => throw new OperationCanceledException()));
        Assert.Throws<OperationCanceledException>(() =>
            result.TryBind<int, int>(_ => throw new OperationCanceledException()));
    }

    [Fact]
    public async Task TryMapAsyncAndTryBindAsync_PropagateCancellation()
    {
        Result<int> result = Result.Ok(42);

        Task<Result<int>> mapOperation = result.TryMapAsync<int, int>(
            _ => Task.FromCanceled<int>(new CancellationToken(true)));
        Task<Result<int>> bindOperation = result.TryBindAsync<int, int>(
            _ => Task.FromCanceled<Result<int>>(new CancellationToken(true)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => mapOperation);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bindOperation);
    }

    [Fact]
    public async Task AsyncTryFamily_StillConvertsOrdinaryExceptionsToFailures()
    {
        InvalidOperationException exception = new InvalidOperationException("ordinary failure");

        Result<Unit> action = await ResultTryExtensions.TryAsync(() => Task.FromException(exception));
        Result<int> map = await Result.Ok(42).TryMapAsync<int, int>(_ => Task.FromException<int>(exception));
        Result<int> bind = await Result.Ok(42).TryBindAsync<int, int>(
            _ => Task.FromException<Result<int>>(exception));

        Assert.Same(exception, Assert.IsType<Result<Unit>.Fail>(action).Error.Exception);
        Assert.Same(exception, Assert.IsType<Result<int>.Fail>(map).Error.Exception);
        Assert.Same(exception, Assert.IsType<Result<int>.Fail>(bind).Error.Exception);
    }

    [Fact]
    public async Task CancelledIncomingTask_DoesNotInvokeFollowingOperations()
    {
        CancellationToken token = new CancellationToken(true);
        Task<Result<int>> cancelled = Task.FromCanceled<Result<int>>(token);
        bool mapperInvoked = false;
        bool binderInvoked = false;
        bool predicateInvoked = false;
        bool sideEffectInvoked = false;

        Task<Result<int>> map = cancelled.MapAsync(value =>
        {
            mapperInvoked = true;
            return value + 1;
        });
        Task<Result<int>> bind = cancelled.BindAsync(value =>
        {
            binderInvoked = true;
            return Result.Ok(value + 1);
        });
        Task<Result<int>> ensure = cancelled.EnsureAsync(value =>
        {
            predicateInvoked = true;
            return true;
        }, TestError);
        Task<Result<int>> tap = cancelled.TapAsync(value =>
        {
            sideEffectInvoked = true;
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => map);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bind);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ensure);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tap);

        Assert.True(map.IsCanceled);
        Assert.False(mapperInvoked);
        Assert.False(binderInvoked);
        Assert.False(predicateInvoked);
        Assert.False(sideEffectInvoked);
    }

    [Fact]
    public async Task OrdinaryAsyncOperations_PropagateCancellationFromInvokedDelegates()
    {
        CancellationToken token = new CancellationToken(true);
        Result<int> result = Result.Ok(42);

        Task<Result<int>> map = result.MapAsync(_ => Task.FromCanceled<int>(token));
        Task<Result<int>> bind = result.BindAsync(_ => Task.FromCanceled<Result<int>>(token));
        Task<Result<int>> ensure = result.EnsureAsync(_ => Task.FromCanceled<bool>(token), TestError);
        Task<Result<int>> tap = result.TapAsync(_ => Task.FromCanceled(token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => map);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bind);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ensure);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tap);
    }

    [Fact]
    public async Task FailureTrack_StillShortCircuitsTryOperations()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        Result<int> failure = Result.Fail<int>(TestError);
        int invocationCount = 0;

        Result<int> syncMap = failure.TryMap<int, int>(_ =>
        {
            invocationCount++;
            throw new OperationCanceledException(source.Token);
        });
        Result<int> syncBind = failure.TryBind<int, int>(_ =>
        {
            invocationCount++;
            throw new OperationCanceledException(source.Token);
        });
        Result<int> asyncMap = await failure.TryMapAsync<int, int>(_ =>
        {
            invocationCount++;
            return Task.FromCanceled<int>(source.Token);
        });
        Result<int> asyncBind = await failure.TryBindAsync<int, int>(_ =>
        {
            invocationCount++;
            return Task.FromCanceled<Result<int>>(source.Token);
        });

        Assert.Equal(0, invocationCount);
        Assert.Same(TestError, Assert.IsType<Result<int>.Fail>(syncMap).Error);
        Assert.Same(TestError, Assert.IsType<Result<int>.Fail>(syncBind).Error);
        Assert.Same(TestError, Assert.IsType<Result<int>.Fail>(asyncMap).Error);
        Assert.Same(TestError, Assert.IsType<Result<int>.Fail>(asyncBind).Error);
    }

    [Fact]
    public async Task SequentialCollectionOperations_StopWhenCancellationIsObserved()
    {
        int[] values = new[] { 1, 2, 3 };
        CancellationToken token = new CancellationToken(true);
        int traverseCount = 0;
        int traverseAllCount = 0;

        Task<Result<IReadOnlyList<int>>> traverse = values.TraverseAsync(value =>
        {
            traverseCount++;
            return value == 2
                ? Task.FromCanceled<Result<int>>(token)
                : Task.FromResult(Result.Ok(value));
        });
        Task<Result<IReadOnlyList<int>>> traverseAll = values.TraverseAllAsync(value =>
        {
            traverseAllCount++;
            return value == 2
                ? Task.FromCanceled<Result<int>>(token)
                : Task.FromResult(Result.Ok(value));
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => traverse);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => traverseAll);
        Assert.Equal(2, traverseCount);
        Assert.Equal(2, traverseAllCount);
    }

    [Fact]
    public async Task WhenAllCollectionOperation_WithCancellation_RemainsCancelled()
    {
        CancellationToken token = new CancellationToken(true);
        Task<Result<int>> success = Task.FromResult(Result.Ok(1));
        Task<Result<int>> cancellation = Task.FromCanceled<Result<int>>(token);

        Task<Result<IReadOnlyList<int>>> operation = ResultCombineExtensions.CombineAsync(success, cancellation);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.True(operation.IsCanceled);
    }

    [Fact]
    public async Task WhenAllCollectionOperation_WithFaultAndCancellation_RemainsFaulted()
    {
        InvalidOperationException expected = new InvalidOperationException("fault wins");
        CancellationToken token = new CancellationToken(true);
        Task<Result<int>> fault = Task.FromException<Result<int>>(expected);
        Task<Result<int>> cancellation = Task.FromCanceled<Result<int>>(token);

        Task<Result<IReadOnlyList<int>>> operation = ResultCombineExtensions.CombineAllAsync(fault, cancellation);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() => operation);
        Assert.Same(expected, actual);
        Assert.True(operation.IsFaulted);
    }

    [Fact]
    public async Task MatchAsync_PropagatesCancelledTaskAndValueTaskInputs()
    {
        CancellationToken token = new CancellationToken(true);
        Task<Result<int>> cancelledTask = Task.FromCanceled<Result<int>>(token);
        ValueTask<Result<int>> cancelledValueTask = new ValueTask<Result<int>>(
            Task.FromCanceled<Result<int>>(token));

        Task<int> taskMatch = cancelledTask.MatchAsync(ok => ok.Value, _ => 0);
        ValueTask<int> valueTaskMatch = cancelledValueTask.MatchAsync(ok => ok.Value, _ => 0);
        Task<int> valueTaskMatchAsTask = valueTaskMatch.AsTask();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskMatch);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueTaskMatchAsTask);
        Assert.True(taskMatch.IsCanceled);
        Assert.True(valueTaskMatchAsTask.IsCanceled);
    }

    [Fact]
    public async Task Cancellation_ProducesNoRailwayOperationLog()
    {
        InMemoryLogger logger = new InMemoryLogger();
        RailwayLoggingOptions options = new RailwayLoggingOptions
        {
            Enabled = true,
            TimingStrategy = TimingStrategy.None,
            SamplingRate = 1
        };
        using IDisposable scope = RailwayLogging.EnableLogging(logger, options);

        Result<int> successfulResult = ResultTryExtensions.Try(() => 42);
        Assert.IsType<Result<int>.Ok>(successfulResult);
        Assert.Single(logger.Entries);
        logger.Clear();

        Task<Result<int>> cancelledOperation = ResultTryExtensions.TryAsync<int>(
            () => throw new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledOperation);
        Assert.Empty(logger.Entries);
    }
}
