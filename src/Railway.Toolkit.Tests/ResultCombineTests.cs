using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Combine")]
public class ResultCombineTests
{
    [Fact]
    public void Zip_WithTwoOkResults_ShouldReturnTuple()
    {
        var result1 = Result.Ok(1);
        var result2 = Result.Ok("two");

        var combined = result1.Zip(result2);

        var (num, str) = combined.Match(ok => ok.Value, fail => (0, ""));
        Assert.Equal(1, num);
        Assert.Equal("two", str);
    }

    [Fact]
    public void Zip_WithFirstFail_ShouldReturnFirstError()
    {
        var result1 = Result.Fail<int>("Error 1", "ERR1");
        var result2 = Result.Ok("two");

        var combined = result1.Zip(result2);

        var error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error 1", error.Message);
    }

    [Fact]
    public void Zip_WithSecondFail_ShouldReturnSecondError()
    {
        var result1 = Result.Ok(1);
        var result2 = Result.Fail<string>("Error 2", "ERR2");

        var combined = result1.Zip(result2);

        var error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error 2", error.Message);
    }

    [Fact]
    public void Zip_WithSelector_ShouldCombineValues()
    {
        var result1 = Result.Ok(5);
        var result2 = Result.Ok(3);

        var combined = result1.Zip(result2, (a, b) => a + b);

        Assert.Equal(8, combined.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Zip_WithThreeResults_ShouldReturnTriple()
    {
        var result1 = Result.Ok(1);
        var result2 = Result.Ok(2);
        var result3 = Result.Ok(3);

        var combined = result1.Zip(result2, result3);

        var (a, b, c) = combined.Match(ok => ok.Value, fail => (0, 0, 0));
        Assert.Equal(1, a);
        Assert.Equal(2, b);
        Assert.Equal(3, c);
    }

    [Fact]
    public void Combine_WithAllOk_ShouldReturnList()
    {
        var result1 = Result.Ok(1);
        var result2 = Result.Ok(2);
        var result3 = Result.Ok(3);

        var combined = ResultCombineExtensions.Combine(result1, result2, result3);

        var list = combined.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Combine_WithOneFail_ShouldReturnFirstError()
    {
        var result1 = Result.Ok(1);
        var result2 = Result.Fail<int>("Error 2", "ERR2");
        var result3 = Result.Ok(3);

        var combined = ResultCombineExtensions.Combine(result1, result2, result3);

        var error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error 2", error.Message);
    }

    [Fact]
    public void CombineAll_WithAllOk_ShouldReturnList()
    {
        var result1 = Result.Ok(1);
        var result2 = Result.Ok(2);
        var result3 = Result.Ok(3);

        var combined = ResultCombineExtensions.CombineAll(result1, result2, result3);

        var list = combined.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void CombineAll_WithMultipleFails_ShouldAggregateErrors()
    {
        var result1 = Result.Fail<int>("Error 1", "ERR1");
        var result2 = Result.Ok(2);
        var result3 = Result.Fail<int>("Error 3", "ERR3");

        var combined = ResultCombineExtensions.CombineAll(result1, result2, result3);

        var error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.NotNull(error.InnerErrors);
        Assert.Equal(2, error.InnerErrors.Count);
        Assert.Equal("Error 1", error.InnerErrors[0].Message);
        Assert.Equal("Error 3", error.InnerErrors[1].Message);
    }

    [Fact]
    public void CombineAll_ValidateAllPattern_ShowsAllErrors()
    {
        // This demonstrates the "validate all" pattern
        var results = new[]
        {
            Result.Fail<int>("Name required", "NAME"),
            Result.Fail<int>("Email required", "EMAIL"),
            Result.Fail<int>("Age must be positive", "AGE")
        };

        var combined = results.CombineAll();

        var error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(3, error.InnerErrors!.Count);
        // User sees all validation errors at once
    }

    [Fact]
    public async Task ZipAsync_WithAsyncResults_ShouldCombine()
    {
        var task1 = Task.FromResult(Result.Ok(1));
        var task2 = Task.FromResult(Result.Ok("two"));

        var combined = await task1.ZipAsync(task2);

        var (num, str) = combined.Match(ok => ok.Value, fail => (0, ""));
        Assert.Equal(1, num);
        Assert.Equal("two", str);
    }

    [Fact]
    public async Task CombineAsync_ShouldCombineAsyncResults()
    {
        var task1 = Task.FromResult(Result.Ok(1));
        var task2 = Task.FromResult(Result.Ok(2));
        var task3 = Task.FromResult(Result.Ok(3));

        var combined = await ResultCombineExtensions.CombineAsync(task1, task2, task3);

        var list = combined.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task CombineAllAsync_ShouldAggregateErrors()
    {
        var task1 = Task.FromResult(Result.Fail<int>("Error 1", "ERR1"));
        var task2 = Task.FromResult(Result.Ok(2));
        var task3 = Task.FromResult(Result.Fail<int>("Error 3", "ERR3"));

        var combined = await ResultCombineExtensions.CombineAllAsync(task1, task2, task3);

        var error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
    }

    [Fact]
    public async Task ZipAsync_WithSelector_ShouldCombineValues()
    {
        var task1 = Task.FromResult(Result.Ok(5));
        var task2 = Task.FromResult(Result.Ok(3));

        var combined = await task1.ZipAsync(task2, (a, b) => a + b);

        Assert.Equal(8, combined.Match(ok => ok.Value, fail => 0));
    }
}
