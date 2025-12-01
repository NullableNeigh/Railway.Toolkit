using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Validation")]
public class ResultEnsureTests
{
    [Fact]
    public void Ensure_WithPredicatePass_ShouldReturnOk()
    {
        var result = Result.Ok(10);

        var ensured = result.Ensure(
            x => x > 5,
            Error.Create("Too small", "VALIDATION")
        );

        Assert.Equal(10, ensured.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Ensure_WithPredicateFail_ShouldReturnFail()
    {
        var result = Result.Ok(3);

        var ensured = result.Ensure(
            x => x > 5,
            Error.Create("Too small", "VALIDATION")
        );

        var error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Too small", error.Message);
        Assert.Equal("VALIDATION", error.Code);
    }

    [Fact]
    public void Ensure_WithFailResult_ShouldPassThroughError()
    {
        var originalError = Error.Create("Original", "ORIG");
        var result = Result.Fail<int>(originalError);

        var ensured = result.Ensure(
            x => x > 5,
            Error.Create("Should not see this", "VALIDATION")
        );

        var error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.Equal("Original", error!.Message);
    }

    [Fact]
    public void Ensure_WithMessageAndCode_ShouldCreateError()
    {
        var result = Result.Ok(3);

        var ensured = result.Ensure(
            x => x > 5,
            "Value must be greater than 5",
            "TOO_SMALL"
        );

        var error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Value must be greater than 5", error.Message);
        Assert.Equal("TOO_SMALL", error.Code);
    }

    [Fact]
    public void Ensure_WithErrorFactory_ShouldUseFactory()
    {
        var result = Result.Ok(3);

        var ensured = result.Ensure(
            x => x > 5,
            x => Error.Create($"Value {x} is too small", "TOO_SMALL")
        );

        var error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Value 3 is too small", error.Message);
    }

    [Fact]
    public void EnsureNotNull_WithNonNullValue_ShouldReturnOk()
    {
        var result = Result.Ok("test");

        var ensured = result.EnsureNotNull();

        Assert.Equal("test", ensured.Match(ok => ok.Value, fail => ""));
    }

    [Fact]
    public void EnsureNotNull_WithNullValue_ShouldReturnFail()
    {
        var result = Result.Ok<string?>(null);

        var ensured = result.EnsureNotNull();

        var error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("NullValue", error.Code);
    }

    [Fact]
    public void EnsureAll_WithAllPredicatesPass_ShouldReturnOk()
    {
        var result = Result.Ok(10);

        var ensured = result.EnsureAll(
            (x => x > 5, Error.Create("Too small", "TOO_SMALL")),
            (x => x < 20, Error.Create("Too large", "TOO_LARGE")),
            (x => x % 2 == 0, Error.Create("Not even", "NOT_EVEN"))
        );

        Assert.Equal(10, ensured.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void EnsureAll_WithFirstPredicateFail_ShouldReturnFirstError()
    {
        var result = Result.Ok(3);

        var ensured = result.EnsureAll(
            (x => x > 5, Error.Create("Too small", "TOO_SMALL")),
            (x => x < 20, Error.Create("Too large", "TOO_LARGE"))
        );

        var error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Too small", error.Message);
    }

    [Fact]
    public async Task EnsureAsync_WithAsyncPredicate_ShouldWork()
    {
        var result = Result.Ok(10);

        var ensured = await result.EnsureAsync(
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
        var result = Railway.Start(10)
            .Ensure(x => x > 0, "Must be positive", "NEGATIVE")
            .Ensure(x => x < 100, "Must be less than 100", "TOO_LARGE")
            .Ensure(x => x % 2 == 0, "Must be even", "NOT_EVEN");

        Assert.Equal(10, result.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Ensure_FailureStopsChain_LikeRailwaySwitch()
    {
        var result = Railway.Start(150)
            .Ensure(x => x > 0, "Must be positive", "NEGATIVE")
            .Ensure(x => x < 100, "Must be less than 100", "TOO_LARGE")
            .Ensure(x => x % 2 == 0, "Must be even", "NOT_EVEN");

        var error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Must be less than 100", error.Message);
        Assert.Equal("TOO_LARGE", error.Code);
    }

    [Fact]
    public async Task EnsureAsync_WithTaskResult_ShouldValidate()
    {
        var resultTask = Task.FromResult(Result.Ok(10));
        var testError = Error.Create("Must be positive", "NEGATIVE");

        var ensured = await resultTask.EnsureAsync(x => x > 0, testError);

        var value = ensured.Match(ok => ok.Value, fail => -1);
        Assert.Equal(10, value);
    }

    [Fact]
    public async Task EnsureAsync_WithTaskResultAndAsyncPredicate_ShouldValidate()
    {
        var resultTask = Task.FromResult(Result.Ok(10));
        var testError = Error.Create("Must be positive", "NEGATIVE");

        var ensured = await resultTask.EnsureAsync(async x =>
        {
            await Task.Delay(1);
            return x > 0;
        }, testError);

        var value = ensured.Match(ok => ok.Value, fail => -1);
        Assert.Equal(10, value);
    }

    [Fact]
    public async Task EnsureAsync_WithTaskResultAndAsyncPredicate_ShouldFailValidation()
    {
        var resultTask = Task.FromResult(Result.Ok(-5));
        var testError = Error.Create("Must be positive", "NEGATIVE");

        var ensured = await resultTask.EnsureAsync(async x =>
        {
            await Task.Delay(1);
            return x > 0;
        }, testError);

        var error = ensured.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Must be positive", error.Message);
    }
}
