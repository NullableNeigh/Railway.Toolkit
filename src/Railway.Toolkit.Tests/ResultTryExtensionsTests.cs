using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Try")]
public class ResultTryExtensionsTests
{
    [Fact]
    public void Try_WithSuccessfulFunc_ShouldReturnOk()
    {
        Result<int> result = ResultTryExtensions.Try(() => 42);

        Assert.Equal(42, result.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Try_WithException_ShouldReturnFail()
    {
        Result<int> result = ResultTryExtensions.Try<int>(() => throw new InvalidOperationException("Test error"));

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Test error", error.Message);
        Assert.Equal("InvalidOperationException", error.Code);
        Assert.NotNull(error.Exception);
    }

    [Fact]
    public void Try_WithCustomErrorCode_ShouldUseCustomCode()
    {
        Result<int> result = ResultTryExtensions.Try<int>(() => throw new Exception("Test"), "CUSTOM");

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("CUSTOM", error.Code);
    }

    [Fact]
    public void Try_WithAction_ShouldReturnUnit()
    {
        bool executed = false;
        Result<Unit> result = ResultTryExtensions.Try(() => { executed = true; });

        Assert.True(executed);
        Assert.True(Unit.Value == result.Match(ok => ok.Value, fail => default));
    }

    [Fact]
    public void Try_WithActionException_ShouldReturnFail()
    {
        Result<Unit> result = ResultTryExtensions.Try(() => throw new Exception("Action failed"));

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Action failed", error.Message);
    }

    [Fact]
    public async Task TryAsync_WithSuccessfulFunc_ShouldReturnOk()
    {
        Result<int> result = await ResultTryExtensions.TryAsync(async () =>
        {
            await Task.Delay(1);
            return 42;
        });

        Assert.Equal(42, result.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public async Task TryAsync_WithException_ShouldReturnFail()
    {
        Result<int> result = await ResultTryExtensions.TryAsync<int>(async () =>
        {
            await Task.Delay(1);
            throw new InvalidOperationException("Async error");
        });

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Async error", error.Message);
    }

    [Fact]
    public async Task TryAsync_WithAction_ShouldReturnUnit()
    {
        bool executed = false;
        Result<Unit> result = await ResultTryExtensions.TryAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        Assert.True(executed);
        Assert.True(Unit.Value == result.Match(ok => ok.Value, fail => default));
    }

    [Fact]
    public void TryMap_WithSuccessfulMapper_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(5);

        Result<int> mapped = result.TryMap(x => x * 2);

        Assert.Equal(10, mapped.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void TryMap_WithExceptionInMapper_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(5);

        Result<int> mapped = result.TryMap<int, int>(x => throw new Exception("Map failed"));

        Error? error = mapped.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Map failed", error.Message);
    }

    [Fact]
    public void TryMap_WithFailResult_ShouldPassThroughError()
    {
        Result<int> result = Result.Fail<int>("Original", "ORIG");

        Result<int> mapped = result.TryMap(x => x * 2);

        Error? error = mapped.Match(ok => (Error?)null, fail => fail.Error);
        Assert.Equal("Original", error!.Message);
    }

    [Fact]
    public void TryBind_WithSuccessfulBinder_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(5);

        Result<int> bound = result.TryBind(x => Result.Ok(x * 2));

        Assert.Equal(10, bound.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void TryBind_WithExceptionInBinder_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(5);

        Result<int> bound = result.TryBind<int, int>(x => throw new Exception("Bind failed"));

        Error? error = bound.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Bind failed", error.Message);
    }

    [Fact]
    public async Task TryMapAsync_WithAsyncMapper_ShouldWork()
    {
        Result<int> result = Result.Ok(5);

        Result<int> mapped = await result.TryMapAsync(async x =>
        {
            await Task.Delay(1);
            return x * 2;
        });

        Assert.Equal(10, mapped.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public async Task TryBindAsync_WithAsyncBinder_ShouldWork()
    {
        Result<int> result = Result.Ok(5);

        Result<int> bound = await result.TryBindAsync(async x =>
        {
            await Task.Delay(1);
            return Result.Ok(x * 2);
        });

        Assert.Equal(10, bound.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Try_BoundaryPattern_WrapsImperativeCode()
    {
        // Demonstrates using Try as boundary between imperative and functional
        Result<int> imperativeResult = ResultTryExtensions.Try(() =>
        {
            string data = File.ReadAllText("nonexistent.txt");
            return data.Length;
        });

        Error? error = imperativeResult.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.NotNull(error.Exception);
    }
}
