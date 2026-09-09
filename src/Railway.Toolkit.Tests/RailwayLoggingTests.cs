using Microsoft.Extensions.Logging;
using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Logging")]
public class RailwayLoggingTests
{
    private static readonly RailwayLoggingOptions UntimedOptions = new RailwayLoggingOptions
    {
        Enabled = true,
        TimingStrategy = TimingStrategy.None,
        SamplingRate = 1
    };

    [Fact]
    public void EnabledLogging_EmitsStableStructuredFields()
    {
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, UntimedOptions);

        Result<string> result = Result.Ok(42).Map(value => value.ToString());

        Assert.IsType<Result<string>.Ok>(result);
        InMemoryLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.LogLevel);
        Assert.Equal("Map", entry.Properties["Operation"]);
        Assert.Equal(true, entry.Properties["Success"]);
        Assert.Equal("Int32", entry.Properties["InputType"]);
        Assert.Equal("String", entry.Properties["OutputType"]);
    }

    [Fact]
    public void DisabledLogging_EmitsNoEventAndSelectsNoActiveTimer()
    {
        InMemoryLogger logger = new InMemoryLogger();
        RailwayLoggingOptions options = new RailwayLoggingOptions
        {
            Enabled = false,
            TimingStrategy = TimingStrategy.Stopwatch
        };
        using IDisposable scope = RailwayLogging.EnableLogging(logger, options);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result<int> result = Result.Ok(21).Map(value => value * 2);

        Assert.Same(NullRailwayTimer.Instance, timer);
        Assert.IsType<Result<int>.Ok>(result);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void AbsentOrProviderDisabledLogging_SelectsNoActiveTimer()
    {
        using IRailwayTimer absentTimer = RailwayLogging.StartOperation();
        Assert.Same(NullRailwayTimer.Instance, absentTimer);

        InMemoryLogger logger = new InMemoryLogger(enabled: false);
        RailwayLoggingOptions options = new RailwayLoggingOptions
        {
            Enabled = true,
            TimingStrategy = TimingStrategy.Timestamp
        };
        using IDisposable scope = RailwayLogging.EnableLogging(logger, options);
        using IRailwayTimer disabledTimer = RailwayLogging.StartOperation();

        Result<int> result = Result.Ok(21).Map(value => value * 2);

        Assert.Same(NullRailwayTimer.Instance, disabledTimer);
        Assert.IsType<Result<int>.Ok>(result);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void AbsentOrProviderDisabledLogging_AllocatesNoTimers()
    {
        MeasureTimerSelectionAllocations();
        long absentAllocations = MeasureTimerSelectionAllocations();

        InMemoryLogger logger = new InMemoryLogger(enabled: false);
        RailwayLoggingOptions options = new RailwayLoggingOptions
        {
            TimingStrategy = TimingStrategy.Stopwatch
        };
        using IDisposable scope = RailwayLogging.EnableLogging(logger, options);
        MeasureTimerSelectionAllocations();
        long disabledAllocations = MeasureTimerSelectionAllocations();

        Assert.Equal(0, absentAllocations);
        Assert.Equal(0, disabledAllocations);
    }

    [Fact]
    public void NestedScopes_RestorePreviousLoggerOnlyOnce()
    {
        InMemoryLogger outerLogger = new InMemoryLogger();
        InMemoryLogger innerLogger = new InMemoryLogger();
        IDisposable outerScope = RailwayLogging.EnableLogging(outerLogger, UntimedOptions);
        Result.Ok(0).Map(value => value + 1);
        RailwayLoggingOptions innerOptions = new RailwayLoggingOptions
        {
            TimingStrategy = TimingStrategy.Stopwatch
        };
        IDisposable innerScope = RailwayLogging.EnableLogging(innerLogger, innerOptions);

        Result.Ok(1).Map(value => value + 1);
        innerScope.Dispose();
        innerScope.Dispose();
        Result.Ok(2).Map(value => value + 1);
        outerScope.Dispose();
        outerScope.Dispose();
        Result.Ok(3).Map(value => value + 1);

        InMemoryLogEntry innerEntry = Assert.Single(innerLogger.Entries);
        Assert.Contains("DurationMs", innerEntry.Properties.Keys);
        Assert.Collection(
            outerLogger.Entries,
            entry => Assert.DoesNotContain("DurationMs", entry.Properties.Keys),
            entry => Assert.DoesNotContain("DurationMs", entry.Properties.Keys));
    }

    [Fact]
    public async Task ConcurrentAsyncFlows_IsolateLoggingScopes()
    {
        InMemoryLogger firstLogger = new InMemoryLogger();
        InMemoryLogger secondLogger = new InMemoryLogger();
        TaskCompletionSource<bool> release = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int readyCount = 0;

        async Task RunAsync(InMemoryLogger logger, RailwayLoggingOptions options, int value)
        {
            using IDisposable scope = RailwayLogging.EnableLogging(logger, options);
            if (Interlocked.Increment(ref readyCount) == 2)
            {
                release.SetResult(true);
            }

            await release.Task;
            Result.Ok(value).Map(item => item + 1);
        }

        RailwayLoggingOptions valueOptions = new RailwayLoggingOptions
        {
            TimingStrategy = TimingStrategy.None,
            LogSuccessValues = true
        };
        await Task.WhenAll(
            RunAsync(firstLogger, UntimedOptions, 1),
            RunAsync(secondLogger, valueOptions, 2));

        Assert.DoesNotContain("OutputValue", Assert.Single(firstLogger.Entries).Properties.Keys);
        Assert.Equal("3", Assert.Single(secondLogger.Entries).Properties["OutputValue"]);
    }

    [Fact]
    public void SuccessfulFailedAndSkippedOperations_EmitExpectedSemantics()
    {
        InMemoryLogger logger = new InMemoryLogger();
        Error error = Error.Create("failed unchanged", "Logging.Failed");
        bool skippedMapperInvoked = false;
        using IDisposable scope = RailwayLogging.EnableLogging(logger, UntimedOptions);

        Result.Ok(1).Map(value => value + 1);
        Result.Ok(1).Bind<int, string>(_ => Result.Fail<string>(error));
        Result.Fail<int>(error).Map(value =>
        {
            skippedMapperInvoked = true;
            return value + 1;
        });

        Assert.False(skippedMapperInvoked);
        Assert.Collection(
            logger.Entries,
            entry =>
            {
                Assert.Equal(LogLevel.Debug, entry.LogLevel);
                Assert.Equal("Map", entry.Properties["Operation"]);
                Assert.Equal(true, entry.Properties["Success"]);
            },
            entry =>
            {
                Assert.Equal(LogLevel.Warning, entry.LogLevel);
                Assert.Equal("Bind", entry.Properties["Operation"]);
                Assert.Equal(false, entry.Properties["Success"]);
            },
            entry =>
            {
                Assert.Equal(LogLevel.Debug, entry.LogLevel);
                Assert.Equal("Map", entry.Properties["Operation"]);
                Assert.Equal(false, entry.Properties["Success"]);
            });
    }

    [Fact]
    public void FailedOperation_LogsErrorCodeAndMessageWithoutDetailedErrorState()
    {
        InMemoryLogger logger = new InMemoryLogger();
        InvalidOperationException exception = new InvalidOperationException("exception detail");
        IReadOnlyDictionary<string, string[]> details = new Dictionary<string, string[]>
        {
            ["SecretField"] = new[] { "detail value" }
        };
        IReadOnlyList<Error> innerErrors = new[] { Error.Create("inner message", "Inner.Code") };
        Error error = new Error
        {
            Message = "message passed unchanged",
            Code = "Failure.Code",
            Exception = exception,
            Details = details,
            InnerErrors = innerErrors
        };
        using IDisposable scope = RailwayLogging.EnableLogging(logger, UntimedOptions);

        Result.Ok(1).Bind<int, int>(_ => Result.Fail<int>(error));

        InMemoryLogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal("Failure.Code", entry.Properties["ErrorCode"]);
        Assert.Equal("message passed unchanged", entry.Properties["ErrorMessage"]);
        Assert.Null(entry.Exception);
        Assert.DoesNotContain("Error", entry.Properties.Keys);
        Assert.DoesNotContain("Exception", entry.Properties.Keys);
        Assert.DoesNotContain("Details", entry.Properties.Keys);
        Assert.DoesNotContain("InnerErrors", entry.Properties.Keys);
        Assert.DoesNotContain(entry.Properties.Values, value => ReferenceEquals(value, error));
        Assert.DoesNotContain(entry.Properties.Values, value => ReferenceEquals(value, exception));
        Assert.DoesNotContain(entry.Properties.Values, value => ReferenceEquals(value, details));
        Assert.DoesNotContain(entry.Properties.Values, value => ReferenceEquals(value, innerErrors));
    }

    [Fact]
    public void SuccessValue_IsLoggedOnlyWhenExplicitlyEnabled()
    {
        InMemoryLogger defaultLogger = new InMemoryLogger();
        using (RailwayLogging.EnableLogging(defaultLogger, UntimedOptions))
        {
            Result.Ok(20).Map(value => value + 1);
        }

        InMemoryLogger optedInLogger = new InMemoryLogger();
        RailwayLoggingOptions optedInOptions = new RailwayLoggingOptions
        {
            Enabled = true,
            TimingStrategy = TimingStrategy.None,
            LogSuccessValues = true
        };
        using (RailwayLogging.EnableLogging(optedInLogger, optedInOptions))
        {
            Result.Ok(20).Map(value => value + 1);
        }

        Assert.DoesNotContain("OutputValue", Assert.Single(defaultLogger.Entries).Properties.Keys);
        Assert.Equal("21", Assert.Single(optedInLogger.Entries).Properties["OutputValue"]);
    }

    [Fact]
    public void SlowOperationFiltering_ExcludesFastOperationsAndWarnsForSlowOperations()
    {
        InMemoryLogger filteredLogger = new InMemoryLogger();
        RailwayLoggingOptions filteredOptions = new RailwayLoggingOptions
        {
            TimingStrategy = TimingStrategy.Stopwatch,
            LogSlowOperationsOnly = true,
            SlowOperationThreshold = TimeSpan.FromDays(1)
        };
        using (RailwayLogging.EnableLogging(filteredLogger, filteredOptions))
        {
            Result.Ok(1).Map(value => value + 1);
        }

        InMemoryLogger slowLogger = new InMemoryLogger();
        RailwayLoggingOptions slowOptions = new RailwayLoggingOptions
        {
            TimingStrategy = TimingStrategy.Stopwatch,
            LogSlowOperationsOnly = true,
            SlowOperationThreshold = TimeSpan.FromMilliseconds(1)
        };
        using (RailwayLogging.EnableLogging(slowLogger, slowOptions))
        {
            Result.Ok(1).Map(value =>
            {
                Thread.Sleep(10);
                return value + 1;
            });
        }

        Assert.Empty(filteredLogger.Entries);
        Assert.Equal(LogLevel.Warning, Assert.Single(slowLogger.Entries).LogLevel);
    }

    [Fact]
    public void SamplingState_IsIsolatedPerLoggingScope()
    {
        InMemoryLogger logger = new InMemoryLogger();
        RailwayLoggingOptions options = new RailwayLoggingOptions
        {
            TimingStrategy = TimingStrategy.None,
            SamplingRate = 2
        };

        using (RailwayLogging.EnableLogging(logger, options))
        {
            Result.Ok(1).Map(value => value + 1);
        }

        using (RailwayLogging.EnableLogging(logger, options))
        {
            Result.Ok(2).Map(value => value + 1);
        }

        Assert.Empty(logger.Entries);

        using (RailwayLogging.EnableLogging(logger, options))
        {
            Result.Ok(3).Map(value => value + 1);
            Result.Ok(4).Map(value => value + 1);
        }

        Assert.Single(logger.Entries);
    }

    [Theory]
    [InlineData(TimingStrategy.Stopwatch)]
    [InlineData(TimingStrategy.Timestamp)]
    public void EnabledTiming_EmitsNumericDuration(TimingStrategy timingStrategy)
    {
        InMemoryLogger logger = new InMemoryLogger();
        RailwayLoggingOptions options = new RailwayLoggingOptions
        {
            TimingStrategy = timingStrategy
        };
        using IDisposable scope = RailwayLogging.EnableLogging(logger, options);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result.Ok(1).Map(value => value + 1);

        if (timingStrategy == TimingStrategy.Stopwatch)
        {
            Assert.IsType<StopwatchRailwayTimer>(timer);
        }
        else
        {
            Assert.IsType<TimestampRailwayTimer>(timer);
        }

        object? duration = Assert.Single(logger.Entries).Properties["DurationMs"];
        Assert.IsType<double>(duration);
    }

    [Fact]
    public void DisabledTiming_OmitsDuration()
    {
        InMemoryLogger logger = new InMemoryLogger();
        using IDisposable scope = RailwayLogging.EnableLogging(logger, UntimedOptions);

        using IRailwayTimer timer = RailwayLogging.StartOperation();
        Result.Ok(1).Map(value => value + 1);

        Assert.Same(NullRailwayTimer.Instance, timer);
        Assert.DoesNotContain("DurationMs", Assert.Single(logger.Entries).Properties.Keys);
    }

    private static long MeasureTimerSelectionAllocations()
    {
        const int Iterations = 1_000;
        long allocationsBefore = GC.GetAllocatedBytesForCurrentThread();

        for (int index = 0; index < Iterations; index++)
        {
            using IRailwayTimer timer = RailwayLogging.StartOperation();
        }

        return GC.GetAllocatedBytesForCurrentThread() - allocationsBefore;
    }
}
