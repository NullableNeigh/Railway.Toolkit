using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Map")]
public class ResultMapTests
{
    [Fact]
    public void Map_WithOkResult_ShouldTransformValue()
    {
        Result<int> result = Result.Ok(5);

        Result<int> mapped = result.Map(x => x * 2);

        int value = mapped.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public void Map_WithFailResult_ShouldPassThroughError()
    {
        Error error = Error.Create("Test error", "TEST");
        Result<int> result = Result.Fail<int>(error);

        Result<int> mapped = result.Map(x => x * 2);

        Error? errorValue = mapped.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
        Assert.Equal("TEST", errorValue.Code);
    }

    [Fact]
    public void Map_ShouldChangeType()
    {
        Result<int> result = Result.Ok(42);

        Result<string> mapped = result.Map(x => x.ToString());

        string value = mapped.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("42", value);
    }

    [Fact]
    public void Map_WithNullMapper_ShouldThrowArgumentNullException()
    {
        Result<int> result = Result.Ok(5);

        Assert.Throws<ArgumentNullException>(() => result.Map<int, int>(null!));
    }

    [Fact]
    public async Task MapAsync_WithTaskResult_ShouldTransformValue()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(5));

        Result<int> mapped = await resultTask.MapAsync(x => x * 2);

        int value = mapped.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public async Task MapAsync_WithAsyncMapper_ShouldTransformValue()
    {
        Result<int> result = Result.Ok(5);

        Result<int> mapped = await result.MapAsync(async x =>
        {
            await Task.Delay(1);
            return x * 2;
        });

        int value = mapped.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public async Task MapAsync_WithTaskResultAndAsyncMapper_ShouldTransformValue()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(5));

        Result<int> mapped = await resultTask.MapAsync(async x =>
        {
            await Task.Delay(1);
            return x * 2;
        });

        int value = mapped.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public void Map_CanChainMultipleTimes()
    {
        Result<string> result = Railway.Start(5)
            .Map(x => x * 2)
            .Map(x => x + 3)
            .Map(x => x.ToString());

        string value = result.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("13", value);
    }

    [Fact]
    public void Map_WithFailInChain_ShouldSkipRemainingMaps()
    {
        Error error = Error.Create("Test error", "TEST");
        Result<int> result = error;

        Result<int> mapped = result
            .Map(x => x * 2)
            .Map(x => x + 3);

        Error? errorValue = mapped.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
    }
}
