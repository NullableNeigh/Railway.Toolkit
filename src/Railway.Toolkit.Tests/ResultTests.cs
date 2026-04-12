using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Core")]
public class ResultTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessResult()
    {
        Result<int> result = Result.Ok(42);

        int value = result.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(42, value);
    }

    [Fact]
    public void Fail_WithError_ShouldCreateFailureResult()
    {
        Error error = Error.Create("Test error", "TEST");
        Result<int> result = Result.Fail<int>(error);

        Error? errorValue = result.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
        Assert.Equal("TEST", errorValue.Code);
    }

    [Fact]
    public void Fail_WithMessageAndCode_ShouldCreateFailureResult()
    {
        Result<int> result = Result.Fail<int>("Test error", "TEST");

        Error? errorValue = result.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
        Assert.Equal("TEST", errorValue.Code);
    }

    [Fact]
    public void FromNullable_WithValue_ShouldReturnOk()
    {
        string? value = "test";
        Error error = Error.Create("Was null", "NULL");

        Result<string> result = Result.FromNullable(value, error);

        string actualValue = result.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("test", actualValue);
    }

    [Fact]
    public void FromNullable_WithNull_ShouldReturnFail()
    {
        string? value = null;
        Error error = Error.Create("Was null", "NULL");

        Result<string> result = Result.FromNullable(value, error);

        Error? errorValue = result.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Was null", errorValue.Message);
        Assert.Equal("NULL", errorValue.Code);
    }

    [Fact]
    public void FromNullable_WithMessageAndCode_WithNull_ShouldReturnFail()
    {
        string? value = null;

        Result<string> result = Result.FromNullable(value, "Was null", "NULL");

        Error? errorValue = result.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Was null", errorValue.Message);
        Assert.Equal("NULL", errorValue.Code);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateOk()
    {
        Result<int> result = 42;

        int value = result.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(42, value);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateFail()
    {
        Error error = Error.Create("Test error", "TEST");
        Result<int> result = error;

        Error? errorValue = result.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
    }

    [Fact]
    public void Match_WithOkResult_ShouldCallOkFunction()
    {
        Result<int> result = Result.Ok(42);

        string output = result.Match(
            ok => $"Success: {ok.Value}",
            fail => $"Error: {fail.Error.Message}"
        );

        Assert.Equal("Success: 42", output);
    }

    [Fact]
    public void Match_WithFailResult_ShouldCallFailFunction()
    {
        Result<int> result = Result.Fail<int>("Test error", "TEST");

        string output = result.Match(
            ok => $"Success: {ok.Value}",
            fail => $"Error: {fail.Error.Message}"
        );

        Assert.Equal("Error: Test error", output);
    }

    [Fact]
    public void Match_WithActions_WithOkResult_ShouldCallOkAction()
    {
        Result<int> result = Result.Ok(42);
        string called = "";

        result.Match(
            ok => called = "ok",
            fail => called = "fail"
        );

        Assert.Equal("ok", called);
    }

    [Fact]
    public void Match_WithActions_WithFailResult_ShouldCallFailAction()
    {
        Result<int> result = Result.Fail<int>("Test error", "TEST");
        string called = "";

        result.Match(
            ok => called = "ok",
            fail => called = "fail"
        );

        Assert.Equal("fail", called);
    }

    [Fact]
    public void RailwayStart_ShouldCreateOkResult()
    {
        Result<int> result = Railway.Start(42);

        int value = result.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(42, value);
    }
}

public class ResultOkUnitTests
{
    [Fact]
    public void Ok_WithNoParams_ShouldReturnOkUnit()
    {
        Result<Unit> result = Result.Ok();

        Assert.IsType<Result<Unit>.Ok>(result);
    }

    [Fact]
    public void Ok_WithNoParams_ShouldContainUnitValue()
    {
        Result<Unit>.Ok result = Assert.IsType<Result<Unit>.Ok>(Result.Ok());

        Assert.Equal(Unit.Value, result.Value);
    }
}
