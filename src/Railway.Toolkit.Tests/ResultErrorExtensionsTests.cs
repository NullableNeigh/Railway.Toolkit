using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Error")]
public class ResultErrorExtensionsTests
{
    [Fact]
    public void MapError_WithFailResult_ShouldTransformError()
    {
        Result<int> result = Result.Fail<int>("Original error", "ORIG");

        Result<int> mapped = result.MapError(e => Error.Create($"Wrapped: {e.Message}", "WRAPPED"));

        Error? error = mapped.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Wrapped: Original error", error.Message);
        Assert.Equal("WRAPPED", error.Code);
    }

    [Fact]
    public void MapError_WithOkResult_ShouldPassThroughValue()
    {
        Result<int> result = Result.Ok(42);

        Result<int> mapped = result.MapError(e => Error.Create("Should not see this", "TEST"));

        Assert.Equal(42, mapped.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElse_WithFailResult_ShouldReturnDefaultValue()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");

        Result<int> recovered = result.OrElse(999);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElse_WithOkResult_ShouldReturnOriginalValue()
    {
        Result<int> result = Result.Ok(42);

        Result<int> recovered = result.OrElse(999);

        Assert.Equal(42, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElse_WithFactory_ShouldUseFactoryOnFail()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");

        Result<int> recovered = result.OrElse(error => error.Code == "TEST" ? 999 : 0);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElseWith_WithFailResult_ShouldReturnAlternativeResult()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");
        Result<int> alternative = Result.Ok(999);

        Result<int> recovered = result.OrElseWith(alternative);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElseWith_WithFactory_ShouldUseFactoryOnFail()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");

        Result<int> recovered = result.OrElseWith(error =>
            error.Code == "TEST" ? Result.Ok(999) : Result.Fail<int>("Other", "OTHER")
        );

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Recover_ShouldBeAliasForOrElse()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");

        Result<int> recovered = result.Recover(999);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void RecoverWith_ShouldBeAliasForOrElseWith()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");

        Result<int> recovered = result.RecoverWith(Result.Ok(999));

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Recover_WithFactory_ShouldWorkLikeOrElse()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");

        Result<int> recovered = result.Recover(error => 999);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void RecoverWith_WithFactory_ShouldWorkLikeOrElseWith()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");

        Result<int> recovered = result.RecoverWith(error => Result.Ok(999));

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void MapError_InPipeline_CanAddContext()
    {
        Result<int> result = Railway.Start(5)
            .Bind(x => Result.Fail<int>("Database error", "DB_ERROR"))
            .MapError(e => Error.Create($"Failed to process user: {e.Message}", "PROCESS_ERROR"));

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Failed to process user: Database error", error.Message);
        Assert.Equal("PROCESS_ERROR", error.Code);
    }

    [Fact]
    public void Recover_InPipeline_CanProvideDefaultBehavior()
    {
        Result<int> result = Railway.Start(5)
            .Bind(x => Result.Fail<int>("Not found", "NOT_FOUND"))
            .Recover(error => error.Code == "NOT_FOUND" ? 0 : -1)
            .Map(x => x * 2);

        Assert.Equal(0, result.Match(ok => ok.Value, fail => -999));
    }

    [Fact]
    public async Task MapErrorAsync_ShouldTransformErrorAsynchronously()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");

        Result<int> mapped = await result.MapErrorAsync(async e =>
        {
            await Task.Delay(1);
            return Error.Create($"Async: {e.Message}", "ASYNC");
        });

        Error? error = mapped.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Async: Error", error.Message);
    }

    [Fact]
    public async Task MapErrorAsync_WithTaskResult_ShouldTransformError()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Fail<int>("Error", "TEST"));

        Result<int> mapped = await resultTask.MapErrorAsync(e =>
            Error.Create($"Transformed: {e.Message}", "TRANSFORMED"));

        Error? error = mapped.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Transformed: Error", error.Message);
        Assert.Equal("TRANSFORMED", error.Code);
    }

    [Fact]
    public async Task MapErrorAsync_WithTaskResultAndAsyncMapper_ShouldTransformError()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Fail<int>("Error", "TEST"));

        Result<int> mapped = await resultTask.MapErrorAsync(async e =>
        {
            await Task.Delay(1);
            return Error.Create($"Async transformed: {e.Message}", "ASYNC_TRANSFORMED");
        });

        Error? error = mapped.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Async transformed: Error", error.Message);
        Assert.Equal("ASYNC_TRANSFORMED", error.Code);
    }
}
