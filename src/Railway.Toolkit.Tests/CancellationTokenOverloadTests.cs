using System.Collections;
using Microsoft.Extensions.Logging;

namespace Railway.Toolkit.Tests;

// These tests deliberately control the governing token independently of the test runner.
#pragma warning disable xUnit1051

[Trait("Category", "Cancellation")]
public class CancellationTokenOverloadTests
{
    private static readonly Error TestError = Error.Create("Test failure", "Test.Failure");

    public static TheoryData<string> SuccessOperations => new TheoryData<string>
    {
        "Bind", "TaskBind", "Map", "TaskMap", "Ensure", "TaskEnsure", "Tap", "TaskTap",
        "TryMap", "TryBind"
    };

    public static TheoryData<string> TryOperations => new TheoryData<string>
    {
        "Function", "Action", "TryMap", "TryBind"
    };

    public static TheoryData<string> NonTryOperations => new TheoryData<string>
    {
        "Bind", "TaskBind", "Map", "TaskMap", "Ensure", "TaskEnsure", "Tap", "TaskTap"
    };

    [Theory]
    [MemberData(nameof(SuccessOperations))]
    public async Task SuccessTrack_ForwardsExactToken(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        int calls = 0;
        Result<int> output = await RunSuccess(operation, Result.Ok(42), source.Token, token =>
        {
            Assert.Equal(source.Token, token);
            calls++;
            return Task.CompletedTask;
        });

        Assert.Equal(42, Assert.IsType<Result<int>.Ok>(output).Value);
        Assert.Equal(1, calls);
    }

    [Theory]
    [MemberData(nameof(SuccessOperations))]
    public async Task PreCancelledSuccess_DoesNotInvokeDelegate(string operation)
    {
        CancellationToken token = new CancellationToken(true);
        int calls = 0;
        Task<Result<int>> pending = RunSuccess(operation, Result.Ok(42), token, _ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(token, exception.CancellationToken);
        Assert.True(pending.IsCanceled);
        Assert.Equal(0, calls);
    }

    [Theory]
    [MemberData(nameof(SuccessOperations))]
    public async Task CancelledFailure_PreservesErrorWithoutInvokingDelegate(string operation)
    {
        int calls = 0;
        Result<int> output = await RunSuccess(operation, Result.Fail<int>(TestError), new CancellationToken(true), _ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        Assert.Same(TestError, Assert.IsType<Result<int>.Fail>(output).Error);
        Assert.Equal(0, calls);
    }

    [Theory]
    [MemberData(nameof(SuccessOperations))]
    public async Task DelegateIgnoringCancellation_CanCompleteSingleOperation(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        Result<int> output = await RunSuccess(operation, Result.Ok(42), source.Token, _ =>
        {
            source.Cancel();
            return Task.CompletedTask;
        });

        Assert.Equal(42, Assert.IsType<Result<int>.Ok>(output).Value);
    }

    [Theory]
    [InlineData("TaskBind", false)]
    [InlineData("TaskBind", true)]
    [InlineData("TaskMap", false)]
    [InlineData("TaskMap", true)]
    [InlineData("TaskEnsure", false)]
    [InlineData("TaskEnsure", true)]
    [InlineData("TaskTap", false)]
    [InlineData("TaskTap", true)]
    public async Task IncomingTask_CancellationWaitsForResultThenUsesTrack(string operation, bool failure)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        TaskCompletionSource<Result<int>> incoming = new TaskCompletionSource<Result<int>>();
        int calls = 0;
        Task<Result<int>> pending = RunIncoming(operation, incoming.Task, source.Token, _ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        source.Cancel();
        Assert.False(pending.IsCompleted);
        incoming.SetResult(failure ? Result.Fail<int>(TestError) : Result.Ok(42));

        if (failure)
        {
            Assert.Same(TestError, Assert.IsType<Result<int>.Fail>(await pending).Error);
        }
        else
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        }

        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("TaskBind")]
    [InlineData("TaskMap")]
    [InlineData("TaskEnsure")]
    [InlineData("TaskTap")]
    public async Task IncomingTask_PropagatesItsOwnCancellationAndFault(string operation)
    {
        OperationCanceledException cancellation = new OperationCanceledException();
        InvalidOperationException fault = new InvalidOperationException("incoming fault");
        foreach (Exception expected in new Exception[] { cancellation, fault })
        {
            Task<Result<int>> pending = RunIncoming(operation, Task.FromException<Result<int>>(expected), default,
                _ => throw new InvalidOperationException("delegate must not run"));
            Exception? actual = await Record.ExceptionAsync(() => pending);
            Assert.Same(expected, actual);
        }
    }

    [Theory]
    [MemberData(nameof(TryOperations))]
    public async Task Try_ForwardsTokenAndPreventsPreCancelledInvocation(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        int calls = 0;
        Result<int> output = await RunTry(operation, source.Token, token =>
        {
            Assert.Equal(source.Token, token);
            calls++;
            return Task.CompletedTask;
        });
        Assert.IsType<Result<int>.Ok>(output);
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RunTry(operation, source.Token, _ =>
        {
            calls++;
            return Task.CompletedTask;
        }));
        Assert.Equal(1, calls);
    }

    [Theory]
    [MemberData(nameof(TryOperations))]
    public async Task Try_ClassifiesAtCatchBoundaryWithoutComparingTokenIdentity(string operation)
    {
        // Exercise each boundary with governing, linked, unrelated, unrequested and default exception tokens.
        foreach (bool cancelGoverning in new[] { false, true })
        {
            foreach (string origin in new[] { "governing", "linked", "unrelated", "unrequested", "default", "timeout" })
            {
                using CancellationTokenSource governing = new CancellationTokenSource();
                using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(governing.Token);
                using CancellationTokenSource unrelated = new CancellationTokenSource();
                using CancellationTokenSource unrequested = new CancellationTokenSource();
                unrelated.Cancel();
                CancellationToken exceptionToken = origin switch
                {
                    "governing" => governing.Token,
                    "linked" => linked.Token,
                    "unrelated" => unrelated.Token,
                    "unrequested" => unrequested.Token,
                    _ => default
                };
                OperationCanceledException expected = origin == "timeout"
                    ? new TaskCanceledException("timeout-style cancellation")
                    : new OperationCanceledException(exceptionToken);
                TaskCompletionSource entered = new TaskCompletionSource();
                TaskCompletionSource release = new TaskCompletionSource();
                Task<Result<int>> pending = RunTry(operation, governing.Token, async _ =>
                {
                    entered.SetResult();
                    await release.Task;
                    throw expected;
                });
                await entered.Task;
                if (cancelGoverning)
                {
                    governing.Cancel();
                }
                release.SetResult();

                if (cancelGoverning)
                {
                    Exception? actual = await Record.ExceptionAsync(() => pending);
                    Assert.Same(expected, actual);
                    Assert.True(pending.IsCanceled);
                }
                else
                {
                    Error error = Assert.IsType<Result<int>.Fail>(await pending).Error;
                    Assert.Same(expected, error.Exception);
                    Assert.Equal("Boundary.Failure", error.Code);
                    Assert.True(pending.IsCompletedSuccessfully);
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(TryOperations))]
    public async Task Try_DefaultGoverningTokenConvertsSynchronousCancellationAndOrdinaryFaults(string operation)
    {
        foreach (Exception expected in new Exception[] { new OperationCanceledException(), new InvalidOperationException("fault") })
        {
            Result<int> output = await RunTry(operation, default, _ => throw expected);
            Error error = Assert.IsType<Result<int>.Fail>(output).Error;
            Assert.Same(expected, error.Exception);
            Assert.Equal("Boundary.Failure", error.Code);
        }
    }

    [Theory]
    [MemberData(nameof(NonTryOperations))]
    public async Task NonTry_PropagatesUnrequestedCancellation(string operation)
    {
        OperationCanceledException expected = new OperationCanceledException();
        Exception? actual = await Record.ExceptionAsync(() => RunSuccess(operation, Result.Ok(42), default,
            _ => Task.FromException(expected)));
        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Traversal_PreCancelled_DoesNotObtainEnumeratorEvenForEmptySource(bool all)
    {
        foreach (int count in new[] { 0, 2 })
        {
            ObservedSource source = new ObservedSource(count);
            Task<Result<IReadOnlyList<int>>> pending = Traverse(all, source, (_, _) => throw new InvalidOperationException(),
                new CancellationToken(true));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            Assert.Equal(0, source.GetEnumeratorCalls);
            Assert.Equal(0, source.MoveNextCalls);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Traversal_CancellationDuringMoveNext_PreventsSelectorAndDisposes(bool all)
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        ObservedSource source = new ObservedSource(2, cancellation.Cancel);
        int calls = 0;
        Task<Result<IReadOnlyList<int>>> pending = Traverse(all, source, (value, _) =>
        {
            calls++;
            return Task.FromResult(Result.Ok(value));
        }, cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, source.MoveNextCalls);
        Assert.Equal(0, calls);
        Assert.True(source.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Traversal_CancellationAfterSelector_PreventsNextLazyStep(bool all)
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        int steps = 0;
        int calls = 0;
        bool disposed = false;
        IEnumerable<int> LazySource()
        {
            try
            {
                steps++;
                yield return 1;
                steps++;
                yield return 2;
            }
            finally
            {
                disposed = true;
            }
        }
        Task<Result<IReadOnlyList<int>>> pending = Traverse(all, LazySource(), (value, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            calls++;
            cancellation.Cancel();
            return Task.FromResult(Result.Ok(value));
        }, cancellation.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.Equal(1, steps);
        Assert.Equal(1, calls);
        Assert.True(disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Traversal_CancellationDuringGetEnumerator_PreventsFirstMoveNext(bool all)
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        ObservedSource source = new ObservedSource(1, onGetEnumerator: cancellation.Cancel);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Traverse(all, source,
            (_, _) => throw new InvalidOperationException("selector must not run"), cancellation.Token));
        Assert.Equal(1, source.GetEnumeratorCalls);
        Assert.Equal(0, source.MoveNextCalls);
        Assert.True(source.Disposed);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Traversal_DisposalCancellationPropagatesWithoutOperationLog(bool all, bool failure)
    {
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, LoggingOptions());
        OperationCanceledException expected = new OperationCanceledException();
        ObservedSource source = new ObservedSource(1, onDispose: () => throw expected);
        Exception? actual = await Record.ExceptionAsync(() => Traverse(all, source,
            (value, _) => Task.FromResult(failure ? Result.Fail<int>(TestError) : Result.Ok(value)), default));
        Assert.Same(expected, actual);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task TraverseAll_CancellationDiscardsCollectedValuesAndErrorsWithoutLogging()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, LoggingOptions());
        ObservedSource source = new ObservedSource(4);
        List<int> selected = new List<int>();
        Task<Result<IReadOnlyList<int>>> pending = source.TraverseAllAsync((value, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            selected.Add(value);
            if (value == 2)
            {
                cancellation.Cancel();
            }
            return Task.FromResult(value == 1 ? Result.Fail<int>(TestError) : Result.Ok(value));
        }, cancellation.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        Assert.True(pending.IsCanceled);
        Assert.Equal(new[] { 0, 1, 2 }, selected);
        Assert.Equal(3, source.MoveNextCalls);
        Assert.True(source.Disposed);
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Traversal_ForwardsTokenAndPreservesOrdinaryFailurePolicy(bool all)
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        List<int> calls = new List<int>();
        Result<IReadOnlyList<int>> output = await Traverse(all, new[] { 1, 2, 3 }, (value, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            calls.Add(value);
            return Task.FromResult(value == 2 ? Result.Ok(value) : Result.Fail<int>(TestError));
        }, cancellation.Token);
        Error error = Assert.IsType<Result<IReadOnlyList<int>>.Fail>(output).Error;
        if (all)
        {
            Assert.Equal(new[] { 1, 2, 3 }, calls);
            Assert.Equal(2, error.InnerErrors!.Count);
        }
        else
        {
            Assert.Equal(new[] { 1 }, calls);
            Assert.Same(TestError, error);
        }

        Result<IReadOnlyList<int>> success = await Traverse(all, new[] { 3, 1 }, (value, _) => Task.FromResult(Result.Ok(value)), default);
        Assert.Equal(new[] { 3, 1 }, Assert.IsType<Result<IReadOnlyList<int>>.Ok>(success).Value);
        Result<IReadOnlyList<int>> empty = await Traverse(all, Array.Empty<int>(), (_, _) => throw new InvalidOperationException(), default);
        Assert.Empty(Assert.IsType<Result<IReadOnlyList<int>>.Ok>(empty).Value);
        OperationCanceledException expected = new OperationCanceledException();
        Exception? actual = await Record.ExceptionAsync(() => Traverse(all, new[] { 1 },
            (_, _) => Task.FromException<Result<int>>(expected), default));
        Assert.Same(expected, actual);
    }

    [Theory]
    [MemberData(nameof(SuccessOperations))]
    public async Task PropagatedCancellation_ProducesNoOperationLog(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, LoggingOptions());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RunSuccess(operation, Result.Ok(42), source.Token, _ =>
        {
            source.Cancel();
            throw new OperationCanceledException();
        }));
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [MemberData(nameof(TryOperations))]
    public async Task Try_ConvertedCancellationUsesNormalFailureLogging(string operation)
    {
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, LoggingOptions());
        Result<int> output = await RunTry(operation, default, _ => throw new TaskCanceledException());
        Assert.IsType<Result<int>.Fail>(output);
        InMemoryLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(operation is "Action" or "Function" ? "TryAsync" : operation + "Async", entry.Properties["Operation"]);
        Assert.Equal(false, entry.Properties["Success"]);
        Assert.Equal("Boundary.Failure", entry.Properties["ErrorCode"]);
        Assert.Equal(LogLevel.Warning, entry.LogLevel);
    }

    [Theory]
    [MemberData(nameof(TryOperations))]
    public async Task Try_RequestedCancellationProducesNoLogButOrdinaryFaultStillConverts(string operation)
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, LoggingOptions());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => RunTry(operation, source.Token, _ =>
        {
            source.Cancel();
            throw new OperationCanceledException();
        }));
        Assert.Empty(logger.Entries);

        using CancellationTokenSource ordinarySource = new CancellationTokenSource();
        InvalidOperationException expected = new InvalidOperationException("ordinary failure after cancellation");
        Result<int> output = await RunTry(operation, ordinarySource.Token, _ =>
        {
            ordinarySource.Cancel();
            throw expected;
        });
        Assert.Same(expected, Assert.IsType<Result<int>.Fail>(output).Error.Exception);
        Assert.Single(logger.Entries);
    }

    [Fact]
    public async Task ArgumentValidation_PrecedesCancellationAndIncomingWait()
    {
        CancellationToken token = new CancellationToken(true);
        Result<int> input = Result.Ok(42);
        Task<Result<int>> incoming = new TaskCompletionSource<Result<int>>().Task;
        Func<int, CancellationToken, Task<int>> mapper = null!;
        Func<int, CancellationToken, Task<Result<int>>> binder = null!;
        Func<int, CancellationToken, Task<bool>> predicate = null!;
        Func<int, CancellationToken, Task> action = null!;
        List<(string Parameter, Func<Task> Call)> invalid = new List<(string, Func<Task>)>
        {
            ("mapper", () => input.MapAsync(mapper, token)),
            ("mapper", () => incoming.MapAsync(mapper, token)),
            ("binder", () => input.BindAsync(binder, token)),
            ("binder", () => incoming.BindAsync(binder, token)),
            ("predicate", () => input.EnsureAsync(predicate, TestError, token)),
            ("predicate", () => incoming.EnsureAsync(predicate, TestError, token)),
            ("error", () => input.EnsureAsync((_, _) => Task.FromResult(true), null!, token)),
            ("error", () => incoming.EnsureAsync((_, _) => Task.FromResult(true), null!, token)),
            ("action", () => input.TapAsync(action, token)),
            ("action", () => incoming.TapAsync(action, token)),
            ("func", () => ResultTryExtensions.TryAsync<int>(null!, token)),
            ("action", () => ResultTryExtensions.TryAsync((Func<CancellationToken, Task>)null!, token)),
            ("mapper", () => input.TryMapAsync(mapper, token)),
            ("binder", () => input.TryBindAsync(binder, token)),
            ("source", () => ResultCollectionExtensions.TraverseAsync<int, int>(null!, binder, token)),
            ("source", () => ResultCollectionExtensions.TraverseAllAsync<int, int>(null!, binder, token)),
            ("selector", () => Array.Empty<int>().TraverseAsync(binder, token)),
            ("selector", () => Array.Empty<int>().TraverseAllAsync(binder, token))
        };
        foreach ((string parameter, Func<Task> call) in invalid)
        {
            Task pending = call();
            Assert.True(pending.IsCompleted);
            ArgumentNullException exception = await Assert.ThrowsAsync<ArgumentNullException>(() => pending);
            Assert.Equal(parameter, exception.ParamName);
        }
    }

    [Fact]
    public void TokenOverloads_HaveRequiredTokenTaskReturnAndOnlyApprovedFamilies()
    {
        Dictionary<string, int> expected = new Dictionary<string, int>
        {
            ["BindAsync"] = 2, ["MapAsync"] = 2, ["EnsureAsync"] = 2, ["TapAsync"] = 2,
            ["TryAsync"] = 2, ["TryMapAsync"] = 1, ["TryBindAsync"] = 1,
            ["TraverseAsync"] = 1, ["TraverseAllAsync"] = 1
        };
        Dictionary<string, int> actual = new Dictionary<string, int>();
        foreach (Type type in typeof(Result<>).Assembly.GetExportedTypes())
        {
            foreach (System.Reflection.MethodInfo method in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                int index = Array.FindIndex(parameters, parameter => parameter.ParameterType == typeof(CancellationToken));
                if (index < 0)
                {
                    continue;
                }
                Assert.Contains(method.Name, expected.Keys);
                Assert.False(parameters[index].IsOptional);
                Assert.Equal(typeof(Task<>), method.ReturnType.GetGenericTypeDefinition());
                if (method.Name.StartsWith("Try", StringComparison.Ordinal))
                {
                    Assert.Equal("errorCode", parameters[index + 1].Name);
                    Assert.True(parameters[index + 1].IsOptional);
                }
                else
                {
                    Assert.Equal(parameters.Length - 1, index);
                }
                actual[method.Name] = actual.GetValueOrDefault(method.Name) + 1;
            }
        }
        Assert.Equal(expected.OrderBy(pair => pair.Key), actual.OrderBy(pair => pair.Key));
    }

    [Fact]
    public async Task FluentWorkflow_ResolvesTokenAwareAndTokenlessOverloads()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        List<CancellationToken> tokens = new List<CancellationToken>();
        Task<int> Fetch(int id, CancellationToken token)
        {
            tokens.Add(token);
            return Task.FromResult(id + 1);
        }
        Task<Result<int>> Save(int value, CancellationToken token)
        {
            tokens.Add(token);
            return Task.FromResult(Result.Ok(value));
        }
        Result<int> output = await Result.Ok(1)
            .MapAsync(Fetch, source.Token)
            .BindAsync(Save, source.Token)
            .EnsureAsync((value, token) =>
            {
                tokens.Add(token);
                return Task.FromResult(value > 0);
            }, TestError, source.Token)
            .TapAsync((value, token) =>
            {
                tokens.Add(token);
                return Task.CompletedTask;
            }, source.Token)
            .MapAsync(value => value * 2);
        Assert.Equal(4, Assert.IsType<Result<int>.Ok>(output).Value);
        Assert.Equal(4, tokens.Count);
        Assert.All(tokens, token => Assert.Equal(source.Token, token));

        // Token precedes the optional code, which may be omitted or supplied by name.
        Result<int> function = await ResultTryExtensions.TryAsync(token => Fetch(1, token), default);
        Result<Unit> action = await ResultTryExtensions.TryAsync(_ => Task.CompletedTask, default, errorCode: "Save.Failure");
        Assert.IsType<Result<int>.Ok>(function);
        Assert.IsType<Result<Unit>.Ok>(action);
    }

    private static RailwayLoggingOptions LoggingOptions() => new RailwayLoggingOptions
    {
        Enabled = true,
        TimingStrategy = TimingStrategy.None,
        SamplingRate = 1
    };

    private static Task<Result<int>> RunSuccess(string operation, Result<int> input, CancellationToken token,
        Func<CancellationToken, Task> work) => operation switch
    {
        "Bind" => input.BindAsync(async (value, forwarded) => { await work(forwarded); return Result.Ok(value); }, token),
        "Map" => input.MapAsync(async (value, forwarded) => { await work(forwarded); return value; }, token),
        "Ensure" => input.EnsureAsync(async (_, forwarded) => { await work(forwarded); return true; }, TestError, token),
        "Tap" => input.TapAsync((_, forwarded) => work(forwarded), token),
        "TryMap" => input.TryMapAsync(async (value, forwarded) => { await work(forwarded); return value; }, token, "Boundary.Failure"),
        "TryBind" => input.TryBindAsync(async (value, forwarded) => { await work(forwarded); return Result.Ok(value); }, token, "Boundary.Failure"),
        _ => RunIncoming(operation, Task.FromResult(input), token, work)
    };

    private static Task<Result<int>> RunIncoming(string operation, Task<Result<int>> input, CancellationToken token,
        Func<CancellationToken, Task> work) => operation switch
    {
        "TaskBind" => input.BindAsync(async (value, forwarded) => { await work(forwarded); return Result.Ok(value); }, token),
        "TaskMap" => input.MapAsync(async (value, forwarded) => { await work(forwarded); return value; }, token),
        "TaskEnsure" => input.EnsureAsync(async (_, forwarded) => { await work(forwarded); return true; }, TestError, token),
        "TaskTap" => input.TapAsync((_, forwarded) => work(forwarded), token),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    private static async Task<Result<int>> RunTry(string operation, CancellationToken token, Func<CancellationToken, Task> work)
    {
        if (operation == "Action")
        {
            Result<Unit> action = await ResultTryExtensions.TryAsync(work, token, "Boundary.Failure");
            return action is Result<Unit>.Fail fail ? Result.Fail<int>(fail.Error) : Result.Ok(42);
        }
        if (operation == "Function")
        {
            return await ResultTryExtensions.TryAsync(async forwarded => { await work(forwarded); return 42; }, token, "Boundary.Failure");
        }
        return await RunSuccess(operation, Result.Ok(42), token, work);
    }

    private static Task<Result<IReadOnlyList<int>>> Traverse(bool all, IEnumerable<int> source,
        Func<int, CancellationToken, Task<Result<int>>> selector, CancellationToken token) => all
        ? source.TraverseAllAsync(selector, token)
        : source.TraverseAsync(selector, token);

    private sealed class ObservedSource : IEnumerable<int>, IEnumerator<int>
    {
        private readonly int _count;
        private readonly Action? _onMoveNext;
        private readonly Action? _onGetEnumerator;
        private readonly Action? _onDispose;
        private int _current = -1;

        public ObservedSource(int count, Action? onMoveNext = null, Action? onGetEnumerator = null, Action? onDispose = null)
        {
            _count = count;
            _onMoveNext = onMoveNext;
            _onGetEnumerator = onGetEnumerator;
            _onDispose = onDispose;
        }

        public int GetEnumeratorCalls { get; private set; }
        public int MoveNextCalls { get; private set; }
        public bool Disposed { get; private set; }
        public int Current => _current;
        object IEnumerator.Current => Current;

        public IEnumerator<int> GetEnumerator()
        {
            GetEnumeratorCalls++;
            _onGetEnumerator?.Invoke();
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext()
        {
            MoveNextCalls++;
            _onMoveNext?.Invoke();
            return ++_current < _count;
        }

        public void Reset() => throw new NotSupportedException();
        public void Dispose()
        {
            Disposed = true;
            _onDispose?.Invoke();
        }
    }
}

#pragma warning restore xUnit1051
