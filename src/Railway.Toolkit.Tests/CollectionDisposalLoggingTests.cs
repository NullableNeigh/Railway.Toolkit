namespace Railway.Toolkit.Tests;

public class CollectionDisposalLoggingTests
{
    [Theory]
    [InlineData("Traverse")]
    [InlineData("TraverseAsync")]
    [InlineData("Sequence")]
    [InlineData("SequenceAsync")]
    [InlineData("Combine")]
    public async Task FailFast_LogsOnlyAfterSuccessfulDisposal(string operation)
    {
        Error error = Error.Create("Invalid item", "Collection.Invalid");
        Exception?[] disposalFaults =
        {
            null,
            new InvalidOperationException("Disposal failed"),
            new OperationCanceledException()
        };

        foreach (Exception? disposalFault in disposalFaults)
        {
            InMemoryLogger logger = new InMemoryLogger();
            bool disposed = false;
            using IDisposable scope = RailwayLogging.EnableLogging(logger,
                new RailwayLoggingOptions { TimingStrategy = TimingStrategy.None });

            void Dispose()
            {
                Assert.Empty(logger.Entries);
                disposed = true;
                if (disposalFault is not null)
                {
                    throw disposalFault;
                }
            }

            async Task<Result<IReadOnlyList<int>>> Execute()
            {
                return operation switch
                {
                    "Traverse" => FirstOnly(1, Dispose).Traverse(_ => Result.Fail<int>(error)),
                    "TraverseAsync" => await FirstOnly(1, Dispose).TraverseAsync(_ => Task.FromResult(Result.Fail<int>(error))),
                    "Sequence" => FirstOnly(Result.Fail<int>(error), Dispose).Sequence(),
                    "SequenceAsync" => await FirstOnly(Task.FromResult(Result.Fail<int>(error)), Dispose).SequenceAsync(),
                    "Combine" => FirstOnly(Result.Fail<int>(error), Dispose).Combine(),
                    _ => throw new ArgumentOutOfRangeException(nameof(operation))
                };
            }

            if (disposalFault is null)
            {
                Result<IReadOnlyList<int>> result = await Execute();
                Assert.Same(error, Assert.IsType<Result<IReadOnlyList<int>>.Fail>(result).Error);
                InMemoryLogEntry entry = Assert.Single(logger.Entries);
                Assert.Equal(operation, entry.Properties["Operation"]);
                Assert.Equal(false, entry.Properties["Success"]);
            }
            else
            {
                Exception? thrown = await Record.ExceptionAsync(async () => { await Execute(); });
                Assert.Same(disposalFault, thrown);
                Assert.Empty(logger.Entries);
            }

            Assert.True(disposed);
        }
    }

    private static IEnumerable<T> FirstOnly<T>(T item, Action dispose)
    {
        try
        {
            yield return item;
            throw new InvalidOperationException("Fail-fast must not request another item");
        }
        finally
        {
            dispose();
        }
    }
}
