using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Core")]
public class ErrorTests
{
    [Fact]
    public void Create_ShouldCreateErrorWithMessageAndCode()
    {
        Error error = Error.Create("Something went wrong", "TEST_ERROR");

        Assert.Equal("Something went wrong", error.Message);
        Assert.Equal("TEST_ERROR", error.Code);
        Assert.Null(error.Exception);
        Assert.Null(error.InnerErrors);
    }

    [Fact]
    public void FromException_ShouldCreateErrorFromException()
    {
        InvalidOperationException exception = new InvalidOperationException("Test exception");

        Error error = Error.FromException(exception);

        Assert.Equal("Test exception", error.Message);
        Assert.Equal("InvalidOperationException", error.Code);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void FromException_WithCustomCode_ShouldUseCustomCode()
    {
        InvalidOperationException exception = new InvalidOperationException("Test exception");

        Error error = Error.FromException(exception, "CUSTOM_CODE");

        Assert.Equal("Test exception", error.Message);
        Assert.Equal("CUSTOM_CODE", error.Code);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        Error error = Error.Create("Test message", "TEST_CODE");

        string result = error.ToString();

        Assert.Equal("[TEST_CODE] Test message", result);
    }

    [Fact]
    public void Aggregate_WithMultipleErrors_ShouldCombineErrors()
    {
        Error error1 = Error.Create("Error 1", "ERR1");
        Error error2 = Error.Create("Error 2", "ERR2");
        Error error3 = Error.Create("Error 3", "ERR3");

        Error aggregated = Error.Aggregate(new[] { error1, error2, error3 });

        Assert.Equal("AggregateError", aggregated.Code);
        Assert.Contains("3 errors occurred", aggregated.Message);
        Assert.NotNull(aggregated.InnerErrors);
        Assert.Equal(3, aggregated.InnerErrors.Count);
        Assert.Contains(error1, aggregated.InnerErrors);
        Assert.Contains(error2, aggregated.InnerErrors);
        Assert.Contains(error3, aggregated.InnerErrors);
    }

    [Fact]
    public void Aggregate_WithSingleError_ShouldReturnSameError()
    {
        Error error = Error.Create("Single error", "ERR1");

        Error result = Error.Aggregate(new[] { error });

        Assert.Same(error, result);
    }

    [Fact]
    public void Aggregate_WithCustomMessageAndCode_ShouldUseCustomValues()
    {
        Error error1 = Error.Create("Error 1", "ERR1");
        Error error2 = Error.Create("Error 2", "ERR2");

        Error aggregated = Error.Aggregate(
            new[] { error1, error2 },
            "Custom aggregate message",
            "CUSTOM_AGG"
        );

        Assert.Equal("Custom aggregate message", aggregated.Message);
        Assert.Equal("CUSTOM_AGG", aggregated.Code);
        Assert.Equal(2, aggregated.InnerErrors!.Count);
    }

    [Fact]
    public void Aggregate_WithEmptyCollection_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Error.Aggregate(Array.Empty<Error>()));
    }
}
