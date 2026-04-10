using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Collection")]
public class ResultCollectionTests
{
    [Fact]
    public void Traverse_WithAllSuccess_ShouldReturnList()
    {
        int[] numbers = new[] { 1, 2, 3, 4, 5 };

        Result<IReadOnlyList<int>> result = numbers.Traverse(x => Result.Ok(x * 2));

        IReadOnlyList<int> list = result.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(5, list.Count);
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, list);
    }

    [Fact]
    public void Traverse_WithOneFail_ShouldReturnFirstError()
    {
        int[] numbers = new[] { 1, 2, 3, 4, 5 };

        Result<IReadOnlyList<int>> result = numbers.Traverse(x =>
            x == 3 ? Result.Fail<int>("Three failed", "FAIL") : Result.Ok(x * 2)
        );

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Three failed", error.Message);
    }

    [Fact]
    public void TraverseAll_WithAllSuccess_ShouldReturnList()
    {
        int[] numbers = new[] { 1, 2, 3, 4, 5 };

        Result<IReadOnlyList<int>> result = numbers.TraverseAll(x => Result.Ok(x * 2));

        IReadOnlyList<int> list = result.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(5, list.Count);
    }

    [Fact]
    public void TraverseAll_WithMultipleFails_ShouldAggregateErrors()
    {
        int[] numbers = new[] { 1, 2, 3, 4, 5 };

        Result<IReadOnlyList<int>> result = numbers.TraverseAll(x =>
            x % 2 == 0 ? Result.Fail<int>($"{x} is even", "EVEN") : Result.Ok(x)
        );

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.NotNull(error.InnerErrors);
        Assert.Equal(2, error.InnerErrors.Count);
        Assert.Contains(error.InnerErrors, e => e.Message == "2 is even");
        Assert.Contains(error.InnerErrors, e => e.Message == "4 is even");
    }

    [Fact]
    public void Sequence_WithAllOk_ShouldFlipToOkList()
    {
        Result<int>[] results = new[]
        {
            Result.Ok(1),
            Result.Ok(2),
            Result.Ok(3)
        };

        Result<IReadOnlyList<int>> sequenced = results.Sequence();

        IReadOnlyList<int> list = sequenced.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void Sequence_WithOneFail_ShouldReturnFirstError()
    {
        Result<int>[] results = new[]
        {
            Result.Ok(1),
            Result.Fail<int>("Error", "ERR"),
            Result.Ok(3)
        };

        Result<IReadOnlyList<int>> sequenced = results.Sequence();

        Error? error = sequenced.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public void SequenceAll_WithMultipleFails_ShouldAggregateErrors()
    {
        Result<int>[] results = new[]
        {
            Result.Fail<int>("Error 1", "ERR1"),
            Result.Ok(2),
            Result.Fail<int>("Error 3", "ERR3")
        };

        Result<IReadOnlyList<int>> sequenced = results.SequenceAll();

        Error? error = sequenced.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
    }

    [Fact]
    public void Partition_ShouldSplitSuccessesAndFailures()
    {
        Result<int>[] results = new[]
        {
            Result.Ok(1),
            Result.Fail<int>("Error 2", "ERR2"),
            Result.Ok(3),
            Result.Fail<int>("Error 4", "ERR4"),
            Result.Ok(5)
        };

        (IReadOnlyList<int>? successes, IReadOnlyList<Error>? failures) = results.Partition();

        Assert.Equal(3, successes.Count);
        Assert.Equal(new[] { 1, 3, 5 }, successes);
        Assert.Equal(2, failures.Count);
        Assert.Equal("Error 2", failures[0].Message);
        Assert.Equal("Error 4", failures[1].Message);
    }

    [Fact]
    public void Choose_ShouldExtractOnlySuccesses()
    {
        Result<int>[] results = new[]
        {
            Result.Ok(1),
            Result.Fail<int>("Error", "ERR"),
            Result.Ok(3),
            Result.Fail<int>("Error", "ERR"),
            Result.Ok(5)
        };

        IReadOnlyList<int> successes = results.Choose();

        Assert.Equal(3, successes.Count);
        Assert.Equal(new[] { 1, 3, 5 }, successes);
    }

    [Fact]
    public async Task TraverseAsync_WithAsyncSelector_ShouldWork()
    {
        int[] numbers = new[] { 1, 2, 3 };

        Result<IReadOnlyList<int>> result = await numbers.TraverseAsync(async x =>
        {
            await Task.Delay(1);
            return Result.Ok(x * 2);
        });

        IReadOnlyList<int> list = result.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(new[] { 2, 4, 6 }, list);
    }

    [Fact]
    public async Task TraverseAllAsync_WithMultipleFails_ShouldAggregateErrors()
    {
        int[] numbers = new[] { 1, 2, 3, 4 };

        Result<IReadOnlyList<int>> result = await numbers.TraverseAllAsync(async x =>
        {
            await Task.Delay(1);
            return x % 2 == 0 ? Result.Fail<int>($"{x} even", "EVEN") : Result.Ok(x);
        });

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
    }

    [Fact]
    public void TraverseAll_ValidateAllPattern_ShowsAllValidationErrors()
    {
        // Real-world example: validating user input
        string[] inputs = new[] { "invalid1", "valid", "invalid2" };

        Result<IReadOnlyList<string>> result = inputs.TraverseAll(input =>
            input.StartsWith("invalid")
                ? Result.Fail<string>($"{input} is invalid", "INVALID")
                : Result.Ok(input)
        );

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
        // User sees all validation errors at once - critical for UX
    }

    [Fact]
    public void Traverse_FailFastPattern_StopsOnFirstError()
    {
        int processedCount = 0;
        int[] numbers = new[] { 1, 2, 3, 4, 5 };

        Result<IReadOnlyList<int>> result = numbers.Traverse(x =>
        {
            processedCount++;
            return x == 3 ? Result.Fail<int>("Fail", "ERR") : Result.Ok(x);
        });

        // Should stop at 3, not process 4 and 5
        Assert.Equal(3, processedCount);
    }

    [Fact]
    public void TraverseAll_ProcessesAllEvenOnError()
    {
        int processedCount = 0;
        int[] numbers = new[] { 1, 2, 3, 4, 5 };

        Result<IReadOnlyList<int>> result = numbers.TraverseAll(x =>
        {
            processedCount++;
            return x == 3 ? Result.Fail<int>("Fail", "ERR") : Result.Ok(x);
        });

        // Should process all 5 to collect all errors
        Assert.Equal(5, processedCount);
    }

    [Fact]
    public async Task SequenceAsync_WithAllOk_ShouldFlipToOkList()
    {
        Task<Result<int>>[] resultTasks = new[]
        {
            Task.FromResult(Result.Ok(1)),
            Task.FromResult(Result.Ok(2)),
            Task.FromResult(Result.Ok(3))
        };

        Result<IReadOnlyList<int>> sequenced = await resultTasks.SequenceAsync();

        IReadOnlyList<int> list = sequenced.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public async Task SequenceAsync_WithOneFail_ShouldReturnFirstError()
    {
        Task<Result<int>>[] resultTasks = new[]
        {
            Task.FromResult(Result.Ok(1)),
            Task.FromResult(Result.Fail<int>("Error", "ERR")),
            Task.FromResult(Result.Ok(3))
        };

        Result<IReadOnlyList<int>> sequenced = await resultTasks.SequenceAsync();

        Error? error = sequenced.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public async Task SequenceAllAsync_WithMultipleFails_ShouldAggregateErrors()
    {
        Task<Result<int>>[] resultTasks = new[]
        {
            Task.FromResult(Result.Fail<int>("Error 1", "ERR1")),
            Task.FromResult(Result.Ok(2)),
            Task.FromResult(Result.Fail<int>("Error 3", "ERR3"))
        };

        Result<IReadOnlyList<int>> sequenced = await resultTasks.SequenceAllAsync();

        Error? error = sequenced.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
    }
}
