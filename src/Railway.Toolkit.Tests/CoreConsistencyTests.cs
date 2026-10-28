namespace Railway.Toolkit.Tests;

public class CoreConsistencyTests
{
    private static readonly Error Failure = Error.Create("Missing", "Test.Missing");
    private static readonly RailwayLoggingOptions Logging = new RailwayLoggingOptions
    {
        Enabled = true, TimingStrategy = TimingStrategy.None, SamplingRate = 1
    };

    [Fact]
    public async Task FallbackForms_PreserveSuccessAndInvokeOnlyTheFailureTrack()
    {
        int calls = 0;
        Func<Error, int> value = error => { Assert.Same(Failure, error); calls++; return 42; };
        Func<Error, Result<int>> result = error => { Assert.Same(Failure, error); calls++; return Result.Ok(42); };
        Func<Error, Task<int>> asyncValue = error => Task.FromResult(value(error));
        Func<Error, Task<Result<int>>> asyncResult = error => Task.FromResult(result(error));
        Func<Error, CancellationToken, Task<int>> tokenValue = (error, token) => { Assert.Equal(CancellationToken.None, token); return asyncValue(error); };
        Func<Error, CancellationToken, Task<Result<int>>> tokenResult = (error, token) => { Assert.Equal(CancellationToken.None, token); return asyncResult(error); };
        Func<Result<int>, Task<Result<int>>>[] operations =
        {
            input => Task.FromResult(input.OrElse(value)),
            input => Task.FromResult(input.OrElseWith(result)),
            input => Task.FromResult(input).OrElseAsync(value),
            input => Task.FromResult(input).OrElseWithAsync(result),
            input => input.OrElseAsync(asyncValue),
            input => input.OrElseWithAsync(asyncResult),
            input => Task.FromResult(input).OrElseAsync(asyncValue),
            input => Task.FromResult(input).OrElseWithAsync(asyncResult),
            input => input.OrElseAsync(tokenValue, default),
            input => input.OrElseWithAsync(tokenResult, default),
            input => Task.FromResult(input).OrElseAsync(tokenValue, default),
            input => Task.FromResult(input).OrElseWithAsync(tokenResult, default),
            input => Task.FromResult(input).OrElseAsync((error, token) => value(error), default),
            input => Task.FromResult(input).OrElseWithAsync((error, token) => result(error), default)
        };
        foreach (Func<Result<int>, Task<Result<int>>> operation in operations)
        {
            calls = 0;
            Assert.Equal(7, Assert.IsType<Result<int>.Ok>(await operation(Result.Ok(7))).Value);
            Assert.Equal(0, calls);
            Assert.Equal(42, Assert.IsType<Result<int>.Ok>(await operation(Result.Fail<int>(Failure))).Value);
            Assert.Equal(1, calls);
        }
        Assert.Same(Failure, Assert.IsType<Result<int>.Fail>(await Result.Fail<int>(Failure)
            .OrElseWithAsync(_ => Task.FromResult(Result.Fail<int>(Failure)))).Error);
        Assert.Equal(42, Assert.IsType<Result<int>.Ok>(await Task.FromResult(Result.Fail<int>(Failure)).OrElseAsync(42)).Value);
        Assert.Equal(42, Assert.IsType<Result<int>.Ok>(await Task.FromResult(Result.Fail<int>(Failure)).OrElseWithAsync(Result.Ok(42))).Value);
        Assert.DoesNotContain(typeof(ResultErrorExtensions).GetMethods(), method => method.Name.StartsWith("Recover", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FallbackCancellation_FollowsTrackAndTaskBoundaries()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        bool called = false;
        Func<Error, CancellationToken, Task<int>> callback = (_, token) => { called = true; return Task.FromResult(2); };
        Result<int> success = Result.Ok(1);
        Assert.Same(success, await success.OrElseAsync(callback, source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Result.Fail<int>(Failure).OrElseAsync(callback, source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(success).OrElseAsync(callback, source.Token));
        Assert.False(called);

        using CancellationTokenSource during = new CancellationTokenSource();
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, Logging);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Result.Fail<int>(Failure).OrElseWithAsync((_, token) =>
        {
            Assert.Equal(during.Token, token);
            during.Cancel();
            return Task.FromResult(Result.Ok(5));
        }, during.Token));
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task Fallback_AwaitsIncomingWorkAndPropagatesFaults()
    {
        TaskCompletionSource<Result<int>> pending = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        Task<Result<int>> operation = pending.Task.OrElseAsync((_, token) => Task.FromResult(4), source.Token);
        Assert.False(operation.IsCompleted);
        InvalidOperationException fault = new InvalidOperationException("Incoming");
        pending.SetException(fault);
        Assert.Same(fault, await Assert.ThrowsAsync<InvalidOperationException>(() => operation));
    }

    [Fact]
    public async Task FluentTryForms_ConvertOnlyCallbackFaultsAndSkipFailures()
    {
        Exception fault = new FormatException("Callback");
        Func<Task<Result<int>>, Task<Result<int>>>[] operations =
        {
            task => task.TryMapAsync<int, int>((Func<int, int>)(_ => throw fault)),
            task => task.TryMapAsync<int, int>(_ => Task.FromException<int>(fault)),
            task => task.TryBindAsync<int, int>((Func<int, Result<int>>)(_ => throw fault)),
            task => task.TryBindAsync<int, int>(_ => Task.FromException<Result<int>>(fault)),
            task => task.TryMapAsync<int, int>((Func<int, CancellationToken, int>)((_, token) => throw fault), default),
            task => task.TryMapAsync<int, int>((_, token) => Task.FromException<int>(fault), default),
            task => task.TryBindAsync<int, int>((Func<int, CancellationToken, Result<int>>)((_, token) => throw fault), default),
            task => task.TryBindAsync<int, int>((_, token) => Task.FromException<Result<int>>(fault), default)
        };
        foreach (Func<Task<Result<int>>, Task<Result<int>>> operation in operations)
        {
            Assert.Same(fault, Assert.IsType<Result<int>.Fail>(await operation(Task.FromResult(Result.Ok(1)))).Error.Exception);
            Assert.Same(Failure, Assert.IsType<Result<int>.Fail>(await operation(Task.FromResult(Result.Fail<int>(Failure)))).Error);
            Assert.Same(fault, await Assert.ThrowsAsync<FormatException>(() => operation(Task.FromException<Result<int>>(fault))));
        }
        Result<int> success = await Task.FromResult(Result.Ok(2))
            .TryMapAsync(value => value * 2)
            .TryBindAsync(value => Task.FromResult(Result.Ok(value + 1)));
        Assert.Equal(5, Assert.IsType<Result<int>.Ok>(success).Value);
    }

    [Fact]
    public async Task ExceptionMappers_OwnTheErrorAndCannotBeRecaught()
    {
        FormatException fault = new FormatException("Private details");
        Error mapped = Error.Create("Stable message", "App.Convert");
        int calls = 0;
        Func<Exception, Error> mapper = exception => { Assert.Same(fault, exception); calls++; return mapped; };
        Func<Task<Result<int>>>[] operations =
        {
            () => Task.FromResult(ResultTryExtensions.Try<int>((Func<int>)(() => throw fault), exceptionMapper: mapper)),
            () => ResultTryExtensions.TryAsync<int>(() => Task.FromException<int>(fault), exceptionMapper: mapper),
            () => ResultTryExtensions.TryAsync<int>(token => Task.FromException<int>(fault), default, exceptionMapper: mapper),
            () => Task.FromResult(Result.Ok(1).TryMap<int, int>(_ => throw fault, exceptionMapper: mapper)),
            () => Task.FromResult(Result.Ok(1).TryBind<int, int>(_ => throw fault, exceptionMapper: mapper)),
            () => Result.Ok(1).TryBindAsync<int, int>(_ => Task.FromException<Result<int>>(fault), exceptionMapper: mapper),
            () => Task.FromResult(Result.Ok(1)).TryMapAsync<int, int>((Func<int, int>)(_ => throw fault), exceptionMapper: mapper)
        };
        foreach (Func<Task<Result<int>>> operation in operations)
        {
            calls = 0;
            Assert.Same(mapped, Assert.IsType<Result<int>.Fail>(await operation()).Error);
            Assert.Equal(1, calls);
        }
        Result<Unit> action = ResultTryExtensions.Try((Action)(() => throw fault), exceptionMapper: mapper);
        Assert.Same(mapped, Assert.IsType<Result<Unit>.Fail>(action).Error);
        Assert.Same(mapped, Assert.IsType<Result<Unit>.Fail>(await ResultTryExtensions.TryAsync((Func<Task>)(() => Task.FromException(fault)), exceptionMapper: mapper)).Error);
        Assert.Same(mapped, Assert.IsType<Result<Unit>.Fail>(await ResultTryExtensions.TryAsync(token => Task.FromException(fault), TestContext.Current.CancellationToken, exceptionMapper: mapper)).Error);
        ArgumentException mapperFault = new ArgumentException("Mapper");
        Assert.Same(mapperFault, Assert.Throws<ArgumentException>(() => ResultTryExtensions.Try<int>(() => throw fault, exceptionMapper: _ => throw mapperFault)));
        Assert.Throws<InvalidOperationException>(() => ResultTryExtensions.Try<int>(() => throw fault, exceptionMapper: _ => null!));
        Assert.Null(mapped.Exception);
    }

    [Fact]
    public async Task TryCancellation_KeepsExistingClassificationWithCustomMapping()
    {
        int mapped = 0;
        Func<Exception, Error> mapper = _ => { mapped++; return Failure; };
        OperationCanceledException cancellation = new OperationCanceledException();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Result.Ok(1))
            .TryMapAsync<int, int>(_ => Task.FromException<int>(cancellation), exceptionMapper: mapper));
        Assert.Equal(0, mapped);
        Result<int> converted = await Task.FromResult(Result.Ok(1))
            .TryMapAsync<int, int>((_, token) => Task.FromException<int>(cancellation), TestContext.Current.CancellationToken, exceptionMapper: mapper);
        Assert.Same(Failure, Assert.IsType<Result<int>.Fail>(converted).Error);
        Assert.Equal(1, mapped);
        using CancellationTokenSource source = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult(Result.Ok(1)).TryBindAsync<int, int>((_, token) =>
        {
            Assert.Equal(source.Token, token);
            source.Cancel();
            return Task.FromResult(Result.Ok(2));
        }, source.Token, exceptionMapper: mapper));
        Assert.Equal(1, mapped);
    }

    [Fact]
    public async Task NullContracts_AllowPayloadsButRejectMissingStructures()
    {
        Assert.Null(Assert.IsType<Result<string?>.Ok>(Result.Ok<string?>(null)).Value);
        Assert.Null(Assert.IsType<Result<string?>.Ok>(await Result.Fail<string?>(Failure).OrElseAsync(_ => Task.FromResult<string?>(null))).Value);
        Assert.Throws<ArgumentNullException>(() => new Result<int>.Fail(null!));
        Result<int>.Fail failure = new Result<int>.Fail(Failure);
        Assert.Throws<ArgumentNullException>(() => failure with { Error = null! });
        Assert.Throws<ArgumentNullException>(() => { Result<int> result = (Error)null!; });
        Assert.Throws<ArgumentNullException>(() => ((Result<int>)null!).Map(value => value));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((Task<Result<int>>)null!).TryMapAsync(value => value));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult<Result<int>>(null!).TryMapAsync(value => value));
        Assert.Throws<InvalidOperationException>(() => Result.Ok(1).Bind<int, int>(_ => null!));
        Assert.IsType<Result<int>.Ok>(Result.Ok(1).MapError(_ => null!)); // success skips the mapper
    }

    [Fact]
    public async Task NullCallbackOutputs_AreNeverConvertedByTry()
    {
        int mapped = 0;
        Func<Exception, Error> mapper = _ => { mapped++; return Failure; };
        Assert.Throws<InvalidOperationException>(() => Result.Ok(1).TryBind<int, int>(_ => null!, exceptionMapper: mapper));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Result.Ok(1).TryBindAsync<int, int>(_ => Task.FromResult<Result<int>>(null!), exceptionMapper: mapper));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Result.Ok(1).TryBindAsync<int, int>((_, token) => Task.FromResult<Result<int>>(null!), TestContext.Current.CancellationToken, exceptionMapper: mapper));
        await Assert.ThrowsAsync<InvalidOperationException>(() => ResultTryExtensions.TryAsync<int>((Func<Task<int>>)(() => null!), exceptionMapper: mapper));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult(Result.Ok(1)).TryBindAsync<int, int>(_ => Task.FromResult<Result<int>>(null!), exceptionMapper: mapper));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Result.Fail<int>(Failure).OrElseWithAsync(_ => Task.FromResult<Result<int>>(null!)));
        Assert.Throws<InvalidOperationException>(() => Result.Fail<int>(Failure).MapError(_ => null!));
        Assert.Throws<InvalidOperationException>(() => Result.Ok(1).Ensure(_ => false, _ => null!));
        Assert.Equal(0, mapped);
    }

    [Fact]
    public async Task Logging_EmitsOnePublicOperationEventAcrossDelegatingOverloads()
    {
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, Logging);
        (string Name, Func<Task> Call)[] operations =
        {
            ("Ensure", () => { Result.Ok(1).Ensure(_ => false, _ => Failure); return Task.CompletedTask; }),
            ("EnsureAll", () => { Result.Ok(1).EnsureAll((_ => true, Failure), (_ => true, Failure)); return Task.CompletedTask; }),
            ("Zip", () => { Result.Ok(1).Zip(Result.Ok(2), (a, b) => a + b); return Task.CompletedTask; }),
            ("Zip", () => { Result.Ok(1).Zip(Result.Ok(2), Result.Ok(3), (a, b, c) => a + b + c); return Task.CompletedTask; }),
            ("ZipAsync", async () => { await Task.FromResult(Result.Ok(1)).ZipAsync(Task.FromResult(Result.Ok(2))); }),
            ("MapAsync", async () => { await Task.FromResult(Result.Ok(1)).MapAsync(value => value); }),
            ("BindAsync", async () => { await Task.FromResult(Result.Ok(1)).BindAsync(value => Result.Ok(value)); }),
            ("EnsureAsync", async () => { await Task.FromResult(Result.Ok(1)).EnsureAsync(_ => true, Failure); }),
            ("OrElseAsync", async () => { await Task.FromResult(Result.Fail<int>(Failure)).OrElseAsync(_ => 2); }),
            ("OrElseWithAsync", async () => { await Task.FromResult(Result.Fail<int>(Failure)).OrElseWithAsync(_ => Result.Ok(2)); }),
            ("CombineAsync", async () => { await ResultCombineExtensions.CombineAsync(Task.FromResult(Result.Ok(1))); }),
            ("Sequence", () => { new[] { Result.Ok(1) }.Sequence(); return Task.CompletedTask; }),
            ("SequenceAll", () => { new[] { Result.Fail<int>(Failure) }.SequenceAll(); return Task.CompletedTask; }),
            ("SequenceAsync", async () => { await new[] { Task.FromResult(Result.Ok(1)) }.SequenceAsync(); }),
            ("SequenceAllAsync", async () => { await new[] { Task.FromResult(Result.Fail<int>(Failure)) }.SequenceAllAsync(default); })
        };
        foreach ((string name, Func<Task> call) in operations)
        {
            logger.Clear();
            await call();
            Assert.Equal(name, Assert.Single(logger.Entries).Properties["Operation"]);
        }
        logger.Clear();
        Assert.Throws<FormatException>(() => Result.Ok(1).Zip<int, int, int>(Result.Ok(2), (a, b) => throw new FormatException()));
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task CancellationPrecedesNullOutputInspectionAndCleanupStillRuns()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Result.Ok(1).BindAsync<int, int>((_, token) =>
        {
            source.Cancel();
            return Task.FromResult<Result<int>>(null!);
        }, source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult<Result<int>>(null!)
            .TryMapAsync((value, token) => Task.FromResult(value), source.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Task.FromResult<Result<int>>(null!)
            .OrElseAsync((error, token) => Task.FromResult(0), source.Token));
        bool cleaned = false;
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, Logging);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.FromResult<Result<int>>(null!).FinallyAsync(() => cleaned = true));
        Assert.True(cleaned);
        Assert.Empty(logger.Entries);
        Assert.Throws<InvalidOperationException>(() => new[] { 1 }.Traverse<int, int>(_ => null!));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new[] { 1 }.TraverseAsync<int, int>(_ => Task.FromResult<Result<int>>(null!)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => ResultCombineExtensions.CombineAsync(Task.FromResult<Result<int>>(null!)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ResultCombineExtensions.CombineAsync<int>(new Task<Result<int>>[] { null! }));
        Assert.Throws<ArgumentNullException>(() => Result.Ok(1).Match<int>(ok => ok.Value, null!));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Result.Ok(1).MatchAsync<int, int>(
            (ok, token) => null!, (fail, token) => Task.FromResult(0), TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() => Error.Aggregate(new Error[] { null! }));
    }

    [Fact]
    public async Task NewCallbacks_AreValidatedBeforePendingIncomingTasks()
    {
        TaskCompletionSource<Result<int>> pending = new TaskCompletionSource<Result<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<Task>[] calls =
        {
            () => pending.Task.OrElseAsync((Func<Error, int>)null!),
            () => pending.Task.OrElseWithAsync((Func<Error, Task<Result<int>>>)null!),
            () => pending.Task.TryMapAsync<int, int>((Func<int, int>)null!),
            () => pending.Task.TryBindAsync<int, int>((Func<int, CancellationToken, Task<Result<int>>>)null!, default)
        };
        foreach (Func<Task> call in calls)
        {
            Task operation = call();
            Assert.True(operation.IsCompleted);
            await Assert.ThrowsAsync<ArgumentNullException>(() => operation);
        }
    }
}
