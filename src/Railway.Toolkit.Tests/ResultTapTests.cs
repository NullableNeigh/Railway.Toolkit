using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Tap")]
public class ResultTapTests
{
    [Fact]
    public void Tap_WithOkResult_ShouldExecuteAction()
    {
        Result<int> result = Result.Ok(42);
        int tapped = 0;

        Result<int> output = result.Tap(value => tapped = value);

        Assert.Equal(42, tapped);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Tap_WithFailResult_ShouldNotExecuteAction()
    {
        Result<int> result = Result.Fail<int>("Error", "TEST");
        bool executed = false;

        Result<int> output = result.Tap(value => executed = true);

        Assert.False(executed);
        Error? error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
    }

    [Fact]
    public void Tap_ShouldReturnOriginalResult()
    {
        Result<int> result = Result.Ok(42);

        Result<int> output = result.Tap(value => { });

        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void TapError_WithOkResult_ShouldNotExecuteAction()
    {
        Result<int> result = Result.Ok(42);
        bool executed = false;

        Result<int> output = result.TapError(error => executed = true);

        Assert.False(executed);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void TapError_WithFailResult_ShouldExecuteAction()
    {
        Result<int> result = Result.Fail<int>("Test error", "TEST");
        Error? tappedError = null;

        Result<int> output = result.TapError(error => tappedError = error);

        Assert.NotNull(tappedError);
        Assert.Equal("Test error", tappedError.Message);
        Error? error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.Equal("Test error", error!.Message);
    }

    [Fact]
    public async Task TapAsync_WithTaskResult_ShouldExecuteAction()
    {
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(42));
        int tapped = 0;

        Result<int> output = await resultTask.TapAsync(value => tapped = value);

        Assert.Equal(42, tapped);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public async Task TapAsync_WithAsyncAction_ShouldExecuteAction()
    {
        Result<int> result = Result.Ok(42);
        int tapped = 0;

        Result<int> output = await result.TapAsync(async value =>
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
        List<string> log = new List<string>();

        Result<int> result = Railway.Start(5)
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
        List<string> errorLog = new List<string>();

        Result<int> result = Railway.Start(5)
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
        string errorMessage = "";
        Task<Result<int>> resultTask = Task.FromResult(Result.Fail<int>("Failed", "ERR"));

        Result<int> output = await resultTask.TapErrorAsync(e => errorMessage = e.Message);

        Assert.Equal("Failed", errorMessage);
        Error? error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task TapErrorAsync_WithAsyncAction_ShouldExecuteOnError()
    {
        string errorMessage = "";
        Result<int> result = Result.Fail<int>("Failed", "ERR");

        Result<int> output = await result.TapErrorAsync(async e =>
        {
            await Task.Delay(1);
            errorMessage = e.Message;
        });

        Assert.Equal("Failed", errorMessage);
        Error? error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task TapErrorAsync_WithTaskResultAndAsyncAction_ShouldExecuteOnError()
    {
        string errorMessage = "";
        Task<Result<int>> resultTask = Task.FromResult(Result.Fail<int>("Failed", "ERR"));

        Result<int> output = await resultTask.TapErrorAsync(async e =>
        {
            await Task.Delay(1);
            errorMessage = e.Message;
        });

        Assert.Equal("Failed", errorMessage);
        Error? error = output.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task TapAsync_WithTaskResultAndAsyncAction_ShouldExecute()
    {
        int tapped = 0;
        Task<Result<int>> resultTask = Task.FromResult(Result.Ok(42));

        Result<int> output = await resultTask.TapAsync(async value =>
        {
            await Task.Delay(1);
            tapped = value;
        });

        Assert.Equal(42, tapped);
        Assert.Equal(42, output.Match(ok => ok.Value, fail => 0));
    }
}

[Trait("Category", "Tap")]
public class ResultFinallyTests
{
    [Fact]
    public void Finally_WithOkResult_ShouldExecuteAction()
    {
        bool executed = false;
        Result<int> result = Result.Ok(42).Finally(() => executed = true);

        Assert.True(executed);
        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public void Finally_WithFailResult_ShouldExecuteAction()
    {
        bool executed = false;
        Result<int> result = Result.Fail<int>(Error.Create("error", "Test.Error")).Finally(() => executed = true);

        Assert.True(executed);
        Assert.IsType<Result<int>.Fail>(result);
    }

    [Fact]
    public void Finally_ShouldPassThroughOriginalResult()
    {
        Result<int> original = Result.Ok(42);
        Result<int> output = original.Finally(() => { });

        Result<int>.Ok ok = Assert.IsType<Result<int>.Ok>(output);
        Assert.Equal(42, ok.Value);
    }

    [Fact]
    public void Finally_WithResultContext_OnOkResult_ShouldReceiveResult()
    {
        bool wasOk = false;
        Result<int> result = Result.Ok(42).Finally(r => wasOk = r is Result<int>.Ok);

        Assert.True(wasOk);
    }

    [Fact]
    public void Finally_WithResultContext_OnFailResult_ShouldReceiveResult()
    {
        bool wasFail = false;
        Result<int> result = Result.Fail<int>(Error.Create("error", "Test.Error"))
            .Finally(r => wasFail = r is Result<int>.Fail);

        Assert.True(wasFail);
    }

    [Fact]
    public async Task FinallyAsync_WithOkResult_ShouldExecuteAction()
    {
        bool executed = false;
        Result<int> result = await Result.Ok(42).FinallyAsync(async () =>
        {
            await Task.Yield();
            executed = true;
        });

        Assert.True(executed);
        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public async Task FinallyAsync_WithFailResult_ShouldExecuteAction()
    {
        bool executed = false;
        Result<int> result = await Result.Fail<int>(Error.Create("error", "Test.Error"))
            .FinallyAsync(async () =>
            {
                await Task.Yield();
                executed = true;
            });

        Assert.True(executed);
        Assert.IsType<Result<int>.Fail>(result);
    }

    [Fact]
    public async Task FinallyAsync_OnTaskResult_ShouldExecuteAction()
    {
        bool executed = false;
        Result<int> result = await Task.FromResult(Result.Ok(42))
            .FinallyAsync(() => executed = true);

        Assert.True(executed);
        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public async Task FinallyAsync_OnTaskResult_WithAsyncAction_ShouldExecuteAction()
    {
        bool executed = false;
        Result<int> result = await Task.FromResult(Result.Ok(42))
            .FinallyAsync(async () =>
            {
                await Task.Yield();
                executed = true;
            });

        Assert.True(executed);
        Assert.IsType<Result<int>.Ok>(result);
    }
}

[Trait("Category", "Tap")]
public class ResultTapActionTests
{
    [Fact]
    public void Tap_WithAction_OnOkResult_ShouldExecute()
    {
        bool executed = false;
        Result.Ok(42).Tap(() => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public void Tap_WithAction_OnFailResult_ShouldNotExecute()
    {
        bool executed = false;
        Result.Fail<int>(Error.Create("error", "Test.Error")).Tap(() => executed = true);

        Assert.False(executed);
    }

    [Fact]
    public void TapError_WithAction_OnFailResult_ShouldExecute()
    {
        bool executed = false;
        Result.Fail<int>(Error.Create("error", "Test.Error")).TapError(() => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public void TapError_WithAction_OnOkResult_ShouldNotExecute()
    {
        bool executed = false;
        Result.Ok(42).TapError(() => executed = true);

        Assert.False(executed);
    }
}
