using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

public class ResultCollectionTests
{
    [Fact]
    public void Traverse_WithAllSuccess_ShouldReturnList()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        var result = numbers.Traverse(x => Result.Ok(x * 2));

        var list = result.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(5, list.Count);
        Assert.Equal(new[] { 2, 4, 6, 8, 10 }, list);
    }

    [Fact]
    public void Traverse_WithOneFail_ShouldReturnFirstError()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        var result = numbers.Traverse(x =>
            x == 3 ? Result.Fail<int>("Three failed", "FAIL") : Result.Ok(x * 2)
        );

        var error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Three failed", error.Message);
    }

    [Fact]
    public void TraverseAll_WithAllSuccess_ShouldReturnList()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        var result = numbers.TraverseAll(x => Result.Ok(x * 2));

        var list = result.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(5, list.Count);
    }

    [Fact]
    public void TraverseAll_WithMultipleFails_ShouldAggregateErrors()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        var result = numbers.TraverseAll(x =>
            x % 2 == 0 ? Result.Fail<int>($"{x} is even", "EVEN") : Result.Ok(x)
        );

        var error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.NotNull(error.InnerErrors);
        Assert.Equal(2, error.InnerErrors.Count);
        Assert.Contains(error.InnerErrors, e => e.Message == "2 is even");
        Assert.Contains(error.InnerErrors, e => e.Message == "4 is even");
    }

    [Fact]
    public void Sequence_WithAllOk_ShouldFlipToOkList()
    {
        var results = new[]
        {
            Result.Ok(1),
            Result.Ok(2),
            Result.Ok(3)
        };

        var sequenced = results.Sequence();

        var list = sequenced.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void Sequence_WithOneFail_ShouldReturnFirstError()
    {
        var results = new[]
        {
            Result.Ok(1),
            Result.Fail<int>("Error", "ERR"),
            Result.Ok(3)
        };

        var sequenced = results.Sequence();

        var error = sequenced.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public void SequenceAll_WithMultipleFails_ShouldAggregateErrors()
    {
        var results = new[]
        {
            Result.Fail<int>("Error 1", "ERR1"),
            Result.Ok(2),
            Result.Fail<int>("Error 3", "ERR3")
        };

        var sequenced = results.SequenceAll();

        var error = sequenced.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
    }

    [Fact]
    public void Partition_ShouldSplitSuccessesAndFailures()
    {
        var results = new[]
        {
            Result.Ok(1),
            Result.Fail<int>("Error 2", "ERR2"),
            Result.Ok(3),
            Result.Fail<int>("Error 4", "ERR4"),
            Result.Ok(5)
        };

        var (successes, failures) = results.Partition();

        Assert.Equal(3, successes.Count);
        Assert.Equal(new[] { 1, 3, 5 }, successes);
        Assert.Equal(2, failures.Count);
        Assert.Equal("Error 2", failures[0].Message);
        Assert.Equal("Error 4", failures[1].Message);
    }

    [Fact]
    public void Choose_ShouldExtractOnlySuccesses()
    {
        var results = new[]
        {
            Result.Ok(1),
            Result.Fail<int>("Error", "ERR"),
            Result.Ok(3),
            Result.Fail<int>("Error", "ERR"),
            Result.Ok(5)
        };

        var successes = results.Choose();

        Assert.Equal(3, successes.Count);
        Assert.Equal(new[] { 1, 3, 5 }, successes);
    }

    [Fact]
    public async Task TraverseAsync_WithAsyncSelector_ShouldWork()
    {
        var numbers = new[] { 1, 2, 3 };

        var result = await numbers.TraverseAsync(async x =>
        {
            await Task.Delay(1);
            return Result.Ok(x * 2);
        });

        var list = result.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(new[] { 2, 4, 6 }, list);
    }

    [Fact]
    public async Task TraverseAllAsync_WithMultipleFails_ShouldAggregateErrors()
    {
        var numbers = new[] { 1, 2, 3, 4 };

        var result = await numbers.TraverseAllAsync(async x =>
        {
            await Task.Delay(1);
            return x % 2 == 0 ? Result.Fail<int>($"{x} even", "EVEN") : Result.Ok(x);
        });

        var error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
    }

    [Fact]
    public void TraverseAll_ValidateAllPattern_ShowsAllValidationErrors()
    {
        // Real-world example: validating user input
        var inputs = new[] { "invalid1", "valid", "invalid2" };

        var result = inputs.TraverseAll(input =>
            input.StartsWith("invalid")
                ? Result.Fail<string>($"{input} is invalid", "INVALID")
                : Result.Ok(input)
        );

        var error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
        // User sees all validation errors at once - critical for UX
    }

    [Fact]
    public void Traverse_FailFastPattern_StopsOnFirstError()
    {
        var processedCount = 0;
        var numbers = new[] { 1, 2, 3, 4, 5 };

        var result = numbers.Traverse(x =>
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
        var processedCount = 0;
        var numbers = new[] { 1, 2, 3, 4, 5 };

        var result = numbers.TraverseAll(x =>
        {
            processedCount++;
            return x == 3 ? Result.Fail<int>("Fail", "ERR") : Result.Ok(x);
        });

        // Should process all 5 to collect all errors
        Assert.Equal(5, processedCount);
    }
}
