using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_ShouldCreateSuccessResult()
    {
        var result = Result.Ok(42);

        var value = result.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(42, value);
    }

    [Fact]
    public void Fail_WithError_ShouldCreateFailureResult()
    {
        var error = Error.Create("Test error", "TEST");
        var result = Result.Fail<int>(error);

        var errorValue = result.Match(
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
        var result = Result.Fail<int>("Test error", "TEST");

        var errorValue = result.Match(
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
        var error = Error.Create("Was null", "NULL");

        var result = Result.FromNullable(value, error);

        var actualValue = result.Match(
            ok => ok.Value,
            fail => null
        );

        Assert.Equal("test", actualValue);
    }

    [Fact]
    public void FromNullable_WithNull_ShouldReturnFail()
    {
        string? value = null;
        var error = Error.Create("Was null", "NULL");

        var result = Result.FromNullable(value, error);

        var errorValue = result.Match(
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

        var result = Result.FromNullable(value, "Was null", "NULL");

        var errorValue = result.Match(
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

        var value = result.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(42, value);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateFail()
    {
        var error = Error.Create("Test error", "TEST");
        Result<int> result = error;

        var errorValue = result.Match(
            ok => (Error?)null,
            fail => fail.Error
        );

        Assert.NotNull(errorValue);
        Assert.Equal("Test error", errorValue.Message);
    }

    [Fact]
    public void Match_WithOkResult_ShouldCallOkFunction()
    {
        var result = Result.Ok(42);

        var output = result.Match(
            ok => $"Success: {ok.Value}",
            fail => $"Error: {fail.Error.Message}"
        );

        Assert.Equal("Success: 42", output);
    }

    [Fact]
    public void Match_WithFailResult_ShouldCallFailFunction()
    {
        var result = Result.Fail<int>("Test error", "TEST");

        var output = result.Match(
            ok => $"Success: {ok.Value}",
            fail => $"Error: {fail.Error.Message}"
        );

        Assert.Equal("Error: Test error", output);
    }

    [Fact]
    public void Match_WithActions_WithOkResult_ShouldCallOkAction()
    {
        var result = Result.Ok(42);
        var called = "";

        result.Match(
            ok => called = "ok",
            fail => called = "fail"
        );

        Assert.Equal("ok", called);
    }

    [Fact]
    public void Match_WithActions_WithFailResult_ShouldCallFailAction()
    {
        var result = Result.Fail<int>("Test error", "TEST");
        var called = "";

        result.Match(
            ok => called = "ok",
            fail => called = "fail"
        );

        Assert.Equal("fail", called);
    }

    [Fact]
    public void RailwayStart_ShouldCreateOkResult()
    {
        var result = Railway.Start(42);

        var value = result.Match(
            ok => ok.Value,
            fail => 0
        );

        Assert.Equal(42, value);
    }
}
