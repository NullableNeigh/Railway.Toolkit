using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Tap")]
public class ResultTapTests
{
    [Fact]
    public void Tap_WithOkResult_ShouldExecuteAction()
    {
        var result = Result.Ok(42);
        var tapped = 0;

        var output = result.Tap(value => tapped = value);

        Assert.Equal(42, tapped);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Tap_WithFailResult_ShouldNotExecuteAction()
    {
        var result = Result.Fail<int>("Error", "TEST");
        var executed = false;

        var output = result.Tap(value => executed = true);

        Assert.False(executed);
        var error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
    }

    [Fact]
    public void Tap_ShouldReturnOriginalResult()
    {
        var result = Result.Ok(42);

        var output = result.Tap(value => { });

        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void TapError_WithOkResult_ShouldNotExecuteAction()
    {
        var result = Result.Ok(42);
        var executed = false;

        var output = result.TapError(error => executed = true);

        Assert.False(executed);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void TapError_WithFailResult_ShouldExecuteAction()
    {
        var result = Result.Fail<int>("Test error", "TEST");
        Error? tappedError = null;

        var output = result.TapError(error => tappedError = error);

        Assert.NotNull(tappedError);
        Assert.Equal("Test error", tappedError.Message);
        var error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.Equal("Test error", error!.Message);
    }

    [Fact]
    public async Task TapAsync_WithTaskResult_ShouldExecuteAction()
    {
        var resultTask = Task.FromResult(Result.Ok(42));
        var tapped = 0;

        var output = await resultTask.TapAsync(value => tapped = value);

        Assert.Equal(42, tapped);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public async Task TapAsync_WithAsyncAction_ShouldExecuteAction()
    {
        var result = Result.Ok(42);
        var tapped = 0;

        var output = await result.TapAsync(async value =>
        {
            await Task.Delay(1);
            tapped = value;
        });

        Assert.Equal(42, tapped);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Tap_InPipeline_ShouldNotBreakChain()
    {
        var log = new List<string>();

        var result = Railway.Start(5)
            .Tap(x => log.Add($"Initial: {x}"))
            .Map(x => x * 2)
            .Tap(x => log.Add($"After map: {x}"))
            .Bind(x => Result.Ok(x + 3))
            .Tap(x => log.Add($"After bind: {x}"));

        Assert.Equal(13, result.Match(ok => ok.Value, fail => 0));
        Assert.Equal(3, log.Count);
        Assert.Equal("Initial: 5", log[0]);
        Assert.Equal("After map: 10", log[1]);
        Assert.Equal("After bind: 13", log[2]);
    }

    [Fact]
    public void TapError_InPipeline_OnlyExecutesOnError()
    {
        var errorLog = new List<string>();

        var result = Railway.Start(5)
            .TapError(e => errorLog.Add("Should not execute"))
            .Bind(x => Result.Fail<int>("Failure", "TEST"))
            .TapError(e => errorLog.Add($"Error: {e.Message}"))
            .Map(x => x * 2)
            .TapError(e => errorLog.Add($"Still error: {e.Code}"));

        Assert.Equal(2, errorLog.Count);
        Assert.Equal("Error: Failure", errorLog[0]);
        Assert.Equal("Still error: TEST", errorLog[1]);
    }

    [Fact]
    public async Task TapErrorAsync_WithTaskResult_ShouldExecuteOnError()
    {
        var errorMessage = "";
        var resultTask = Task.FromResult(Result.Fail<int>("Failed", "ERR"));

        var output = await resultTask.TapErrorAsync(e => errorMessage = e.Message);

        Assert.Equal("Failed", errorMessage);
        var error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task TapErrorAsync_WithAsyncAction_ShouldExecuteOnError()
    {
        var errorMessage = "";
        var result = Result.Fail<int>("Failed", "ERR");

        var output = await result.TapErrorAsync(async e =>
        {
            await Task.Delay(1);
            errorMessage = e.Message;
        });

        Assert.Equal("Failed", errorMessage);
        var error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task TapErrorAsync_WithTaskResultAndAsyncAction_ShouldExecuteOnError()
    {
        var errorMessage = "";
        var resultTask = Task.FromResult(Result.Fail<int>("Failed", "ERR"));

        var output = await resultTask.TapErrorAsync(async e =>
        {
            await Task.Delay(1);
            errorMessage = e.Message;
        });

        Assert.Equal("Failed", errorMessage);
        var error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task TapAsync_WithTaskResultAndAsyncAction_ShouldExecute()
    {
        var tapped = 0;
        var resultTask = Task.FromResult(Result.Ok(42));

        var output = await resultTask.TapAsync(async value =>
        {
            await Task.Delay(1);
            tapped = value;
        });

        Assert.Equal(42, tapped);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }
}
