using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Bind")]
public class ResultBindTests
{
    [Fact]
    public void Bind_WithOkResult_ShouldExecuteBinder()
    {
        Result<int> result = Result.Ok(5);

        Result<int> bound = result.Bind(x => Result.Ok(x * 2));

        int value = bound.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public void Bind_WithFailResult_ShouldPassThroughError()
    {
        Error error = Error.Create("Test error", "TEST");
        Result<int> result = Result.Fail<int>(error);

        Result<int> bound = result.Bind(x => Result.Ok(x * 2));

        Error? errorValue = bound.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
    }

    [Fact]
    public void Bind_WhenBinderReturnsFail_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(5);

        Result<int> bound = result.Bind(x =>
            x > 3
                ? Result.Fail<int>("Too large", "VALIDATION")
                : Result.Ok(x)
        );

        Error? errorValue = bound.Match(
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
        Result<int> result = Result.Ok(42);

        Result<string> bound = result.Bind(x => Result.Ok(x.ToString()));

        string value = bound.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("42", value);
    }

    [Fact]
    public void Bind_WithNullBinder_ShouldThrowArgumentNullException()
    {
        Result<int> result = Result.Ok(5);

        Assert.Throws<ArgumentNullException>(() => result.Bind<int, int>(null!));
    }

    [Fact]
    public async Task BindAsync_WithTaskResult_ShouldExecuteBinder()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(5));

        Result<int> bound = await resultTask.BindAsync(x => Result.Ok(x * 2));

        int value = bound.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public async Task BindAsync_WithAsyncBinder_ShouldExecuteBinder()
    {
        Result<int> result = Result.Ok(5);

        Result<int> bound = await result.BindAsync(async x =>
        {
            await Task.Delay(1);
            return Result.Ok(x * 2);
        });

        int value = bound.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public async Task BindAsync_WithTaskResultAndAsyncBinder_ShouldExecuteBinder()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(5));

        Result<int> bound = await resultTask.BindAsync(async x =>
        {
            await Task.Delay(1);
            return Result.Ok(x * 2);
        });

        int value = bound.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(10, value);
    }

    [Fact]
    public void Bind_CanChainMultipleTimes()
    {
        Result<string> result = Railway.Start(5)
            .Bind(x => Result.Ok(x * 2))
            .Bind(x => Result.Ok(x + 3))
            .Bind(x => Result.Ok(x.ToString()));

        string value = result.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("13", value);
    }

    [Fact]
    public void Bind_WithFailInChain_ShouldSkipRemainingBinds()
    {
        Result<int> result = Railway.Start(5)
            .Bind(x => Result.Ok(x * 2))
            .Bind(x => Result.Fail<int>("Error in middle", "TEST"))
            .Bind(x => Result.Ok(x + 100));

        Error? errorValue = result.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Error in middle", errorValue.Message);
    }

    [Fact]
    public void Bind_CanMixWithMap()
    {
        Result<string> result = Railway.Start(5)
            .Map(x => x * 2)
            .Bind(x => Result.Ok(x + 3))
            .Map(x => x.ToString());

        string value = result.Match(
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

        Result<int> successResult = Railway.Start(10)
            .Bind(ValidatePositive)
            .Bind(ValidateLessThan100)
            .Map(x => x * 2);

        Result<int> failResult = Railway.Start(-5)
            .Bind(ValidatePositive)
            .Bind(ValidateLessThan100)  // Skipped
            .Map(x => x * 2);            // Skipped

        Assert.Equal(20, successResult.Match(ok => ok.Value, fail => 0));

        Error? error = failResult.Match(ok => (Error?)null, fail => fail.Error);
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
