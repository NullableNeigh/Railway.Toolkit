using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Map")]
public class ResultMapTests
{
    [Fact]
    public void Map_WithOkResult_ShouldTransformValue()
    {
        var result = Result.Ok(5);

        var mapped = result.Map(x => x * 2);

        var value = mapped.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public void Map_WithFailResult_ShouldPassThroughError()
    {
        var error = Error.Create("Test error", "TEST");
        var result = Result.Fail<int>(error);

        var mapped = result.Map(x => x * 2);

        var errorValue = mapped.Match(
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
        var result = Result.Ok(42);

        var mapped = result.Map(x => x.ToString());

        var value = mapped.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("42", value);
    }

    [Fact]
    public void Map_WithNullMapper_ShouldThrowArgumentNullException()
    {
        var result = Result.Ok(5);

        Assert.Throws<ArgumentNullException>(() => result.Map<int, int>(null!));
    }

    [Fact]
    public async Task MapAsync_WithTaskResult_ShouldTransformValue()
    {
        var resultTask = Task.FromResult(Result.Ok(5));

        var mapped = await resultTask.MapAsync(x => x * 2);

        var value = mapped.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public async Task MapAsync_WithAsyncMapper_ShouldTransformValue()
    {
        var result = Result.Ok(5);

        var mapped = await result.MapAsync(async x =>
        {
            await Task.Delay(1);
            return x * 2;
        });

        var value = mapped.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public async Task MapAsync_WithTaskResultAndAsyncMapper_ShouldTransformValue()
    {
        var resultTask = Task.FromResult(Result.Ok(5));

        var mapped = await resultTask.MapAsync(async x =>
        {
            await Task.Delay(1);
            return x * 2;
        });

        var value = mapped.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public void Map_CanChainMultipleTimes()
    {
        var result = Railway.Start(5)
            .Map(x => x * 2)
            .Map(x => x + 3)
            .Map(x => x.ToString());

        var value = result.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("13", value);
    }

    [Fact]
    public void Map_WithFailInChain_ShouldSkipRemainingMaps()
    {
        var error = Error.Create("Test error", "TEST");
        Result<int> result = error;

        var mapped = result
            .Map(x => x * 2)
            .Map(x => x + 3);

        var errorValue = mapped.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
    }
}
