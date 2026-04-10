using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

/// <summary>
/// Tests that verify the core concepts of railway-oriented programming.
/// These tests ensure the library correctly implements the railway metaphor.
/// </summary>
[Trait("Category", "Railway")]
public class RailwayPatternTests
{
    // Railway Pattern Concepts:
    // 1. Two tracks: Success and Failure
    // 2. Operations stay on success track or switch to failure track
    // 3. Once on failure track, stay there (short-circuit)
    // 4. Match at the end handles both tracks

    [Fact]
    public void SuccessTrack_OperationsContinue()
    {
        // All operations on success track should execute
        List<string> executionLog = new List<string>();

        Result<string> result = Railway.Start(5)
            .Map(x => { executionLog.Add("Map1"); return x * 2; })
            .Bind(x => { executionLog.Add("Bind1"); return Result.Ok(x + 3); })
            .Map(x => { executionLog.Add("Map2"); return x.ToString(); });

        Assert.Equal(3, executionLog.Count);
        Assert.Equal("13", result.Match(ok => ok.Value, fail => ""));
    }

    [Fact]
    public void FailureTrack_OperationsShortCircuit()
    {
        // Once on failure track, operations should be skipped
        List<string> executionLog = new List<string>();

        Result<string> result = Railway.Start(5)
            .Map(x => { executionLog.Add("Map1"); return x * 2; })
            .Bind(x => { executionLog.Add("Bind1"); return Result.Fail<int>("Error", "TEST"); })
            .Map(x => { executionLog.Add("Map2 - should not execute"); return x.ToString(); })
            .Bind(x => { executionLog.Add("Bind2 - should not execute"); return Result.Ok(x); });

        Assert.Equal(2, executionLog.Count);
        Assert.DoesNotContain("Map2", executionLog);
        Assert.DoesNotContain("Bind2", executionLog);

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error", error.Message);
    }

    [Fact]
    public void Bind_SwitchesToFailureTrack()
    {
        // Bind can switch from success to failure track
        Result<int> result = Railway.Start(150)
            .Bind(x => x > 100
                ? Result.Fail<int>("Too large", "TOO_LARGE")
                : Result.Ok(x)
            );

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Too large", error.Message);
    }

    [Fact]
    public void Ensure_SwitchesToFailureTrack()
    {
        // Ensure can switch from success to failure track
        Result<int> result = Railway.Start(-5)
            .Ensure(x => x >= 0, "Must be non-negative", "NEGATIVE");

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Must be non-negative", error.Message);
    }

    [Fact]
    public void Recover_SwitchesBackToSuccessTrack()
    {
        // Recover can switch from failure back to success track
        Result<int> result = Railway.Start(5)
            .Bind(x => Result.Fail<int>("Error", "ERR"))
            .Recover(0)
            .Map(x => x + 10);

        Assert.Equal(10, result.Match(ok => ok.Value, fail => -1));
    }

    [Fact]
    public void Match_HandlesEndOfRailway()
    {
        // Match must handle both tracks at the end
        Result<int> successResult = Railway.Start(5).Map(x => x * 2);
        Result<int> failureResult = Railway.Start(5).Bind(x => Result.Fail<int>("Error", "ERR"));

        string successOutput = successResult.Match(
            ok => $"Success: {ok.Value}",
            fail => $"Failure: {fail.Error.Message}"
        );

        string failureOutput = failureResult.Match(
            ok => $"Success: {ok.Value}",
            fail => $"Failure: {fail.Error.Message}"
        );

        Assert.Equal("Success: 10", successOutput);
        Assert.Equal("Failure: Error", failureOutput);
    }

    [Fact]
    public void TwoTrackInput_OneTrackOutput_Pattern()
    {
        // Classic railway pattern: multiple two-track inputs combine to single output
        string result = Railway.Start(10)
            .Bind(ValidatePositive)
            .Bind(ValidateLessThan100)
            .Bind(ValidateEven)
            .Match(
                ok => $"Valid: {ok.Value}",
                fail => $"Invalid: {fail.Error.Message}"
            );

        Assert.Equal("Valid: 10", result);
    }

    [Fact]
    public void ErrorPropagation_MaintainsFirstError()
    {
        // First error should propagate through the railway
        Result<int> result = Railway.Start(5)
            .Bind(x => Result.Fail<int>("First error", "ERR1"))
            .Bind(x => Result.Fail<int>("Second error", "ERR2"))
            .Map(x => x * 2);

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.Equal("First error", error!.Message);
        Assert.Equal("ERR1", error.Code);
    }

    [Fact]
    public void Tap_PeeksWithoutSwitchingTracks()
    {
        // Tap should observe but not change tracks
        int tappedValue = 0;

        Result<int> result = Railway.Start(5)
            .Tap(x => tappedValue = x)
            .Map(x => x * 2);

        Assert.Equal(5, tappedValue);
        Assert.Equal(10, result.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void TapError_ObservesFailureTrackOnly()
    {
        // TapError should only execute on failure track
        string errorMessage = "";

        Result<int> result = Railway.Start(5)
            .TapError(e => errorMessage = "Should not execute")
            .Bind(x => Result.Fail<int>("Error", "ERR"))
            .TapError(e => errorMessage = e.Message)
            .Map(x => x * 2);

        Assert.Equal("Error", errorMessage);
    }

    [Fact]
    public void CompleteUserValidationPipeline()
    {
        // Real-world example: user validation pipeline
        User user = new User("John", "john@example.com", 25);

        Result<ValidatedUser> result = Railway.Start(user)
            .Ensure(u => !string.IsNullOrEmpty(u.Name), "Name is required", "NAME_REQUIRED")
            .Ensure(u => u.Email.Contains("@"), "Invalid email", "INVALID_EMAIL")
            .Ensure(u => u.Age >= 18, "Must be 18 or older", "UNDERAGE")
            .Map(u => new ValidatedUser(u.Name, u.Email, u.Age));

        Assert.True(result.Match(ok => true, fail => false));
    }

    [Fact]
    public void CompleteUserValidationPipeline_WithFailure()
    {
        User user = new User("", "invalid", 16);

        Result<User> result = Railway.Start(user)
            .Ensure(u => !string.IsNullOrEmpty(u.Name), "Name is required", "NAME_REQUIRED")
            .Ensure(u => u.Email.Contains("@"), "Invalid email", "INVALID_EMAIL")
            .Ensure(u => u.Age >= 18, "Must be 18 or older", "UNDERAGE");

        Error? error = result.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Name is required", error.Message); // First failure
    }

    [Fact]
    public async Task AsyncRailway_MaintainsTwoTrackSemantics()
    {
        // Async operations should maintain railway semantics
        Result<string> result = await Railway.Start(5)
            .Map(x => x * 2)
            .BindAsync(async x =>
            {
                await Task.Delay(1);
                return Result.Ok(x + 3);
            })
            .MapAsync(async x =>
            {
                await Task.Delay(1);
                return x.ToString();
            });

        Assert.Equal("13", result.Match(ok => ok.Value, fail => ""));
    }

    // Helper validation functions
    private static Result<int> ValidatePositive(int value) =>
        value > 0
            ? Result.Ok(value)
            : Result.Fail<int>("Must be positive", "NEGATIVE");

    private static Result<int> ValidateLessThan100(int value) =>
        value < 100
            ? Result.Ok(value)
            : Result.Fail<int>("Must be less than 100", "TOO_LARGE");

    private static Result<int> ValidateEven(int value) =>
        value % 2 == 0
            ? Result.Ok(value)
            : Result.Fail<int>("Must be even", "NOT_EVEN");

    // Test models
    private record User(string Name, string Email, int Age);
    private record ValidatedUser(string Name, string Email, int Age);
}
