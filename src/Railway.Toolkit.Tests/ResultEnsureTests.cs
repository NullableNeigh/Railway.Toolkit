using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Validation")]
public class ResultEnsureTests
{
    [Fact]
    public void Ensure_WithPredicatePass_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(10);

        Result<int> ensured = result.Ensure(
            x => x > 5,
            Error.Create("Too small", "VALIDATION")
        );

        Assert.Equal(10, ensured.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Ensure_WithPredicateFail_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(3);

        Result<int> ensured = result.Ensure(
            x => x > 5,
            Error.Create("Too small", "VALIDATION")
        );

        Error? error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Too small", error.Message);
        Assert.Equal("VALIDATION", error.Code);
    }

    [Fact]
    public void Ensure_WithFailResult_ShouldPassThroughError()
    {
        Error originalError = Error.Create("Original", "ORIG");
        Result<int> result = Result.Fail<int>(originalError);

        Result<int> ensured = result.Ensure(
            x => x > 5,
            Error.Create("Should not see this", "VALIDATION")
        );

        Error? error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.Equal("Original", error!.Message);
    }

    [Fact]
    public void Ensure_WithMessageAndCode_ShouldCreateError()
    {
        Result<int> result = Result.Ok(3);

        Result<int> ensured = result.Ensure(
            x => x > 5,
            "Value must be greater than 5",
            "TOO_SMALL"
        );

        Error? error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Value must be greater than 5", error.Message);
        Assert.Equal("TOO_SMALL", error.Code);
    }

    [Fact]
    public void Ensure_WithErrorFactory_ShouldUseFactory()
    {
        Result<int> result = Result.Ok(3);

        Result<int> ensured = result.Ensure(
            x => x > 5,
            x => Error.Create($"Value {x} is too small", "TOO_SMALL")
        );

        Error? error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Value 3 is too small", error.Message);
    }

    [Fact]
    public void EnsureNotNull_WithNonNullValue_ShouldReturnOk()
    {
        Result<string> result = Result.Ok("test");

        Result<string> ensured = result.EnsureNotNull();

        Assert.Equal("test", ensured.Match(ok => ok.Value, fail => ""));
    }

    [Fact]
    public void EnsureNotNull_WithNullValue_ShouldReturnFail()
    {
        Result<string?> result = Result.Ok<string?>(null);

        Result<string?> ensured = result.EnsureNotNull();

        Error? error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("NullValue", error.Code);
    }

    [Fact]
    public void EnsureAll_WithAllPredicatesPass_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(10);

        Result<int> ensured = result.EnsureAll(
            (x => x > 5, Error.Create("Too small", "TOO_SMALL")),
            (x => x < 20, Error.Create("Too large", "TOO_LARGE")),
            (x => x % 2 == 0, Error.Create("Not even", "NOT_EVEN"))
        );

        Assert.Equal(10, ensured.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void EnsureAll_WithFirstPredicateFail_ShouldReturnFirstError()
    {
        Result<int> result = Result.Ok(3);

        Result<int> ensured = result.EnsureAll(
            (x => x > 5, Error.Create("Too small", "TOO_SMALL")),
            (x => x < 20, Error.Create("Too large", "TOO_LARGE"))
        );

        Error? error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Too small", error.Message);
    }

    [Fact]
    public async Task EnsureAsync_WithAsyncPredicate_ShouldWork()
    {
        Result<int> result = Result.Ok(10);

        Result<int> ensured = await result.EnsureAsync(
            async x =>
            {
                await Task.Delay(1);
                return x > 5;
            },
            Error.Create("Too small", "VALIDATION")
        );

        Assert.Equal(10, ensured.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Ensure_MultipleInChain_CanValidateMultipleConditions()
    {
        Result<int> result = Railway.Start(10)
            .Ensure(x => x > 0, "Must be positive", "NEGATIVE")
            .Ensure(x => x < 100, "Must be less than 100", "TOO_LARGE")
            .Ensure(x => x % 2 == 0, "Must be even", "NOT_EVEN");

        Assert.Equal(10, result.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Ensure_FailureStopsChain_LikeRailwaySwitch()
    {
        Result<int> result = Railway.Start(150)
            .Ensure(x => x > 0, "Must be positive", "NEGATIVE")
            .Ensure(x => x < 100, "Must be less than 100", "TOO_LARGE")
            .Ensure(x => x % 2 == 0, "Must be even", "NOT_EVEN");

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Must be less than 100", error.Message);
        Assert.Equal("TOO_LARGE", error.Code);
    }

    [Fact]
    public async Task EnsureAsync_WithTaskResult_ShouldValidate()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(10));
        Error testError = Error.Create("Must be positive", "NEGATIVE");

        Result<int> ensured = await resultTask.EnsureAsync(x => x > 0, testError);

        int value = ensured.Match(ok => ok.Value, fail => -1);
        Assert.Equal(10, value);
    }

    [Fact]
    public async Task EnsureAsync_WithTaskResultAndAsyncPredicate_ShouldValidate()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(10));
        Error testError = Error.Create("Must be positive", "NEGATIVE");

        Result<int> ensured = await resultTask.EnsureAsync(async x =>
        {
            await Task.Delay(1);
            return x > 0;
        }, testError);

        int value = ensured.Match(ok => ok.Value, fail => -1);
        Assert.Equal(10, value);
    }

    [Fact]
    public async Task EnsureAsync_WithTaskResultAndAsyncPredicate_ShouldFailValidation()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(-5));
        Error testError = Error.Create("Must be positive", "NEGATIVE");

        Result<int> ensured = await resultTask.EnsureAsync(async x =>
        {
            await Task.Delay(1);
            return x > 0;
        }, testError);

        Error? error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Must be positive", error.Message);
    }
}
