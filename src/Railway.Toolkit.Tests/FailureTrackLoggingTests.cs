namespace Railway.Toolkit.Tests;

public class FailureTrackLoggingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureHandling_ReportsWhetherItsTrackRan(bool failedInput)
    {
        Error error = Error.Create("Original", "Original");
        Result<int> input = failedInput ? Result.Fail<int>(error) : Result.Ok(1);
        (string Name, Func<Task<Result<int>>> Call)[] operations =
        {
            ("MapError", () => Task.FromResult(input.MapError(_ => error))),
            ("MapErrorAsync", () => input.MapErrorAsync(_ => Task.FromResult(error))),
            ("MapErrorAsync", () => Task.FromResult(input).MapErrorAsync(_ => error)),
            ("MapErrorAsync", () => Task.FromResult(input).MapErrorAsync((_, token) => error, default)),
            ("OrElse", () => Task.FromResult(input.OrElse(42))),
            ("OrElse", () => Task.FromResult(input.OrElse(_ => 42))),
            ("OrElseAsync", () => input.OrElseAsync(_ => Task.FromResult(42))),
            ("OrElseAsync", () => Task.FromResult(input).OrElseAsync(_ => 42)),
            ("OrElseWith", () => Task.FromResult(input.OrElseWith(Result.Ok(42)))),
            ("OrElseWithAsync", () => input.OrElseWithAsync(_ => Task.FromResult(Result.Ok(42)))),
            ("OrElseWithAsync", () => Task.FromResult(input).OrElseWithAsync((_, token) => Result.Fail<int>(error), default)),
            ("TapError", () => Task.FromResult(input.TapError(_ => { }))),
            ("TapErrorAsync", () => input.TapErrorAsync(_ => Task.CompletedTask)),
            ("TapErrorAsync", () => Task.FromResult(input).TapErrorAsync((_, token) => { }, default))
        };

        foreach ((string name, Func<Task<Result<int>>> call) in operations)
        {
            InMemoryLogger logger = new InMemoryLogger();
            using IDisposable scope = RailwayLogging.EnableLogging(logger,
                new RailwayLoggingOptions { TimingStrategy = TimingStrategy.None });
            Result<int> output = await call();
            InMemoryLogEntry entry = Assert.Single(logger.Entries);
            Assert.Equal(name, entry.Properties["Operation"]);
            Assert.Equal(output is Result<int>.Ok, entry.Properties["Success"]);
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Debug, entry.LogLevel);
            string format = Assert.IsType<string>(entry.Properties["{OriginalFormat}"]);
            Assert.Contains(failedInput ? "executed" : "skipped", format);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Finally_ReportsExecutionOnEitherTrack(bool failedInput)
    {
        Result<int> input = failedInput ? Result.Fail<int>("Failed", "Failed") : Result.Ok(1);
        Func<Task<Result<int>>>[] operations =
        {
            () => Task.FromResult(input.Finally(() => { })),
            () => input.FinallyAsync(() => Task.CompletedTask),
            () => Task.FromResult(input).FinallyAsync(() => { }),
            () => Task.FromResult(input).FinallyAsync(() => Task.CompletedTask)
        };
        foreach (Func<Task<Result<int>>> call in operations)
        {
            InMemoryLogger logger = new InMemoryLogger();
            using IDisposable scope = RailwayLogging.EnableLogging(logger,
                new RailwayLoggingOptions { TimingStrategy = TimingStrategy.None });
            await call();
            InMemoryLogEntry entry = Assert.Single(logger.Entries);
            Assert.Contains("executed", Assert.IsType<string>(entry.Properties["{OriginalFormat}"]));
        }
    }
}
