using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Core")]
public class ErrorTests
{
    [Fact]
    public void Create_ShouldCreateErrorWithMessageAndCode()
    {
        var error = Error.Create("Something went wrong", "TEST_ERROR");

        Assert.Equal("Something went wrong", error.Message);
        Assert.Equal("TEST_ERROR", error.Code);
        Assert.Null(error.Exception);
        Assert.Null(error.InnerErrors);
    }

    [Fact]
    public void FromException_ShouldCreateErrorFromException()
    {
        var exception = new InvalidOperationException("Test exception");

        var error = Error.FromException(exception);

        Assert.Equal("Test exception", error.Message);
        Assert.Equal("InvalidOperationException", error.Code);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void FromException_WithCustomCode_ShouldUseCustomCode()
    {
        var exception = new InvalidOperationException("Test exception");

        var error = Error.FromException(exception, "CUSTOM_CODE");

        Assert.Equal("Test exception", error.Message);
        Assert.Equal("CUSTOM_CODE", error.Code);
        Assert.Same(exception, error.Exception);
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var error = Error.Create("Test message", "TEST_CODE");

        var result = error.ToString();

        Assert.Equal("[TEST_CODE] Test message", result);
    }

    [Fact]
    public void Aggregate_WithMultipleErrors_ShouldCombineErrors()
    {
        var error1 = Error.Create("Error 1", "ERR1");
        var error2 = Error.Create("Error 2", "ERR2");
        var error3 = Error.Create("Error 3", "ERR3");

        var aggregated = Error.Aggregate(new[] { error1, error2, error3 });

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
        var error = Error.Create("Single error", "ERR1");

        var result = Error.Aggregate(new[] { error });

        Assert.Same(error, result);
    }

    [Fact]
    public void Aggregate_WithCustomMessageAndCode_ShouldUseCustomValues()
    {
        var error1 = Error.Create("Error 1", "ERR1");
        var error2 = Error.Create("Error 2", "ERR2");

        var aggregated = Error.Aggregate(
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
