using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Bind")]
public class ResultBindTests
{
    [Fact]
    public void Bind_WithOkResult_ShouldExecuteBinder()
    {
        var result = Result.Ok(5);

        var bound = result.Bind(x => Result.Ok(x * 2));

        var value = bound.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public void Bind_WithFailResult_ShouldPassThroughError()
    {
        var error = Error.Create("Test error", "TEST");
        var result = Result.Fail<int>(error);

        var bound = result.Bind(x => Result.Ok(x * 2));

        var errorValue = bound.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
    }

    [Fact]
    public void Bind_WhenBinderReturnsFail_ShouldReturnFail()
    {
        var result = Result.Ok(5);

        var bound = result.Bind(x =>
            x > 3
                ? Result.Fail<int>("Too large", "VALIDATION")
                : Result.Ok(x)
        );

        var errorValue = bound.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Too large", errorValue.Message);
        Assert.Equal("VALIDATION", errorValue.Code);
    }

    [Fact]
    public void Bind_ShouldChangeType()
    {
        var result = Result.Ok(42);

        var bound = result.Bind(x => Result.Ok(x.ToString()));

        var value = bound.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("42", value);
    }

    [Fact]
    public void Bind_WithNullBinder_ShouldThrowArgumentNullException()
    {
        var result = Result.Ok(5);

        Assert.Throws<ArgumentNullException>(() => result.Bind<int, int>(null!));
    }

    [Fact]
    public async Task BindAsync_WithTaskResult_ShouldExecuteBinder()
    {
        var resultTask = Task.FromResult(Result.Ok(5));

        var bound = await resultTask.BindAsync(x => Result.Ok(x * 2));

        var value = bound.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public async Task BindAsync_WithAsyncBinder_ShouldExecuteBinder()
    {
        var result = Result.Ok(5);

        var bound = await result.BindAsync(async x =>
        {
            await Task.Delay(1);
            return Result.Ok(x * 2);
        });

        var value = bound.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public async Task BindAsync_WithTaskResultAndAsyncBinder_ShouldExecuteBinder()
    {
        var resultTask = Task.FromResult(Result.Ok(5));

        var bound = await resultTask.BindAsync(async x =>
        {
            await Task.Delay(1);
            return Result.Ok(x * 2);
        });

        var value = bound.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public void Bind_CanChainMultipleTimes()
    {
        var result = Railway.Start(5)
            .Bind(x => Result.Ok(x * 2))
            .Bind(x => Result.Ok(x + 3))
            .Bind(x => Result.Ok(x.ToString()));

        var value = result.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("13", value);
    }

    [Fact]
    public void Bind_WithFailInChain_ShouldSkipRemainingBinds()
    {
        var result = Railway.Start(5)
            .Bind(x => Result.Ok(x * 2))
            .Bind(x => Result.Fail<int>("Error in middle", "TEST"))
            .Bind(x => Result.Ok(x + 100));

        var errorValue = result.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Error in middle", errorValue.Message);
    }

    [Fact]
    public void Bind_CanMixWithMap()
    {
        var result = Railway.Start(5)
            .Map(x => x * 2)
            .Bind(x => Result.Ok(x + 3))
            .Map(x => x.ToString());

        var value = result.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("13", value);
    }

    [Fact]
    public void Bind_RailwayMetaphor_SwitchesToErrorTrack()
    {
        // This demonstrates the railway pattern
        // Success track continues, error track skips operations

        var successResult = Railway.Start(10)
            .Bind(ValidatePositive)
            .Bind(ValidateLessThan100)
            .Map(x => x * 2);

        var failResult = Railway.Start(-5)
            .Bind(ValidatePositive)
            .Bind(ValidateLessThan100)  // Skipped
            .Map(x => x * 2);            // Skipped

        Assert.Equal(20, successResult.Match(ok => ok.Value, fail => 0));

        var error = failResult.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("NEGATIVE", error.Code);
    }

    private static Result<int> ValidatePositive(int value) =>
        value >= 0
            ? Result.Ok(value)
            : Result.Fail<int>("Value must be positive", "NEGATIVE");

    private static Result<int> ValidateLessThan100(int value) =>
        value < 100
            ? Result.Ok(value)
            : Result.Fail<int>("Value must be less than 100", "TOO_LARGE");
}
