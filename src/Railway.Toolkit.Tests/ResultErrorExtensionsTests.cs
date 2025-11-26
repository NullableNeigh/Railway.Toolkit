using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

public class ResultErrorExtensionsTests
{
    [Fact]
    public void MapError_WithFailResult_ShouldTransformError()
    {
        var result = Result.Fail<int>("Original error", "ORIG");

        var mapped = result.MapError(e => Error.Create($"Wrapped: {e.Message}", "WRAPPED"));

        var error = mapped.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Wrapped: Original error", error.Message);
        Assert.Equal("WRAPPED", error.Code);
    }

    [Fact]
    public void MapError_WithOkResult_ShouldPassThroughValue()
    {
        var result = Result.Ok(42);

        var mapped = result.MapError(e => Error.Create("Should not see this", "TEST"));

        Assert.Equal(42, mapped.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElse_WithFailResult_ShouldReturnDefaultValue()
    {
        var result = Result.Fail<int>("Error", "TEST");

        var recovered = result.OrElse(999);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElse_WithOkResult_ShouldReturnOriginalValue()
    {
        var result = Result.Ok(42);

        var recovered = result.OrElse(999);

        Assert.Equal(42, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElse_WithFactory_ShouldUseFactoryOnFail()
    {
        var result = Result.Fail<int>("Error", "TEST");

        var recovered = result.OrElse(error => error.Code == "TEST" ? 999 : 0);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElseWith_WithFailResult_ShouldReturnAlternativeResult()
    {
        var result = Result.Fail<int>("Error", "TEST");
        var alternative = Result.Ok(999);

        var recovered = result.OrElseWith(alternative);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void OrElseWith_WithFactory_ShouldUseFactoryOnFail()
    {
        var result = Result.Fail<int>("Error", "TEST");

        var recovered = result.OrElseWith(error =>
            error.Code == "TEST" ? Result.Ok(999) : Result.Fail<int>("Other", "OTHER")
        );

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Recover_ShouldBeAliasForOrElse()
    {
        var result = Result.Fail<int>("Error", "TEST");

        var recovered = result.Recover(999);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void RecoverWith_ShouldBeAliasForOrElseWith()
    {
        var result = Result.Fail<int>("Error", "TEST");

        var recovered = result.RecoverWith(Result.Ok(999));

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Recover_WithFactory_ShouldWorkLikeOrElse()
    {
        var result = Result.Fail<int>("Error", "TEST");

        var recovered = result.Recover(error => 999);

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void RecoverWith_WithFactory_ShouldWorkLikeOrElseWith()
    {
        var result = Result.Fail<int>("Error", "TEST");

        var recovered = result.RecoverWith(error => Result.Ok(999));

        Assert.Equal(999, recovered.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void MapError_InPipeline_CanAddContext()
    {
        var result = Railway.Start(5)
            .Bind(x => Result.Fail<int>("Database error", "DB_ERROR"))
            .MapError(e => Error.Create($"Failed to process user: {e.Message}", "PROCESS_ERROR"));

        var error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Failed to process user: Database error", error.Message);
        Assert.Equal("PROCESS_ERROR", error.Code);
    }

    [Fact]
    public void Recover_InPipeline_CanProvideDefaultBehavior()
    {
        var result = Railway.Start(5)
            .Bind(x => Result.Fail<int>("Not found", "NOT_FOUND"))
            .Recover(error => error.Code == "NOT_FOUND" ? 0 : -1)
            .Map(x => x * 2);

        Assert.Equal(0, result.Match(ok => ok.Value, fail => -999));
    }

    [Fact]
    public async Task MapErrorAsync_ShouldTransformErrorAsynchronously()
    {
        var result = Result.Fail<int>("Error", "TEST");

        var mapped = await result.MapErrorAsync(async e =>
        {
            await Task.Delay(1);
            return Error.Create($"Async: {e.Message}", "ASYNC");
        });

        var error = mapped.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Async: Error", error.Message);
    }
}
