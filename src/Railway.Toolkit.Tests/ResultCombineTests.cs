using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Combine")]
public class ResultCombineTests
{
    [Fact]
    public void Zip_WithTwoOkResults_ShouldReturnTuple()
    {
        Result<int> result1 = Result.Ok(1);
        Result<string> result2 = Result.Ok("two");

        Result<(int, string)> combined = result1.Zip(result2);

        (int num, string? str) = combined.Match(ok => ok.Value, fail => (0, ""));
        Assert.Equal(1, num);
        Assert.Equal("two", str);
    }

    [Fact]
    public void Zip_WithFirstFail_ShouldReturnFirstError()
    {
        Result<int> result1 = Result.Fail<int>("Error 1", "ERR1");
        Result<string> result2 = Result.Ok("two");

        Result<(int, string)> combined = result1.Zip(result2);

        Error? error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error 1", error.Message);
    }

    [Fact]
    public void Zip_WithSecondFail_ShouldReturnSecondError()
    {
        Result<int> result1 = Result.Ok(1);
        Result<string> result2 = Result.Fail<string>("Error 2", "ERR2");

        Result<(int, string)> combined = result1.Zip(result2);

        Error? error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error 2", error.Message);
    }

    [Fact]
    public void Zip_WithSelector_ShouldCombineValues()
    {
        Result<int> result1 = Result.Ok(5);
        Result<int> result2 = Result.Ok(3);

        Result<int> combined = result1.Zip(result2, (a, b) => a + b);

        Assert.Equal(8, combined.Match(ok => ok.Value, fail => 0));
    }

    [Fact]
    public void Zip_WithThreeResults_ShouldReturnTriple()
    {
        Result<int> result1 = Result.Ok(1);
        Result<int> result2 = Result.Ok(2);
        Result<int> result3 = Result.Ok(3);

        Result<(int, int, int)> combined = result1.Zip(result2, result3);

        (int a, int b, int c) = combined.Match(ok => ok.Value, fail => (0, 0, 0));
        Assert.Equal(1, a);
        Assert.Equal(2, b);
        Assert.Equal(3, c);
    }

    [Fact]
    public void Combine_WithAllOk_ShouldReturnList()
    {
        Result<int> result1 = Result.Ok(1);
        Result<int> result2 = Result.Ok(2);
        Result<int> result3 = Result.Ok(3);

        Result<IReadOnlyList<int>> combined = ResultCombineExtensions.Combine(result1, result2, result3);

        IReadOnlyList<int> list = combined.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
        Assert.Equal(1, list[0]);
        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);
    }

    [Fact]
    public void Combine_WithOneFail_ShouldReturnFirstError()
    {
        Result<int> result1 = Result.Ok(1);
        Result<int> result2 = Result.Fail<int>("Error 2", "ERR2");
        Result<int> result3 = Result.Ok(3);

        Result<IReadOnlyList<int>> combined = ResultCombineExtensions.Combine(result1, result2, result3);

        Error? error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal("Error 2", error.Message);
    }

    [Fact]
    public void CombineAll_WithAllOk_ShouldReturnList()
    {
        Result<int> result1 = Result.Ok(1);
        Result<int> result2 = Result.Ok(2);
        Result<int> result3 = Result.Ok(3);

        Result<IReadOnlyList<int>> combined = ResultCombineExtensions.CombineAll(result1, result2, result3);

        IReadOnlyList<int> list = combined.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void CombineAll_WithMultipleFails_ShouldAggregateErrors()
    {
        Result<int> result1 = Result.Fail<int>("Error 1", "ERR1");
        Result<int> result2 = Result.Ok(2);
        Result<int> result3 = Result.Fail<int>("Error 3", "ERR3");

        Result<IReadOnlyList<int>> combined = ResultCombineExtensions.CombineAll(result1, result2, result3);

        Error? error = combined.Match(ok => (Error?)null, fail => fail.Error);
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
        Result<int>[] results = new[]
        {
            Result.Fail<int>("Name required", "NAME"),
            Result.Fail<int>("Email required", "EMAIL"),
            Result.Fail<int>("Age must be positive", "AGE")
        };

        Result<IReadOnlyList<int>> combined = results.CombineAll();

        Error? error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(3, error.InnerErrors!.Count);
        // User sees all validation errors at once
    }

    [Fact]
    public async Task ZipAsync_WithAsyncResults_ShouldCombine()
    {
        Task<Result<int>> task1 = Task.FromResult(Result.Ok(1));
        Task<Result<string>> task2 = Task.FromResult(Result.Ok("two"));

        Result<(int, string)> combined = await task1.ZipAsync(task2);

        (int num, string? str) = combined.Match(ok => ok.Value, fail => (0, ""));
        Assert.Equal(1, num);
        Assert.Equal("two", str);
    }

    [Fact]
    public async Task CombineAsync_ShouldCombineAsyncResults()
    {
        Task<Result<int>> task1 = Task.FromResult(Result.Ok(1));
        Task<Result<int>> task2 = Task.FromResult(Result.Ok(2));
        Task<Result<int>> task3 = Task.FromResult(Result.Ok(3));

        Result<IReadOnlyList<int>> combined = await ResultCombineExtensions.CombineAsync(task1, task2, task3);

        IReadOnlyList<int> list = combined.Match(ok => ok.Value, fail => Array.Empty<int>());
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public async Task CombineAllAsync_ShouldAggregateErrors()
    {
        Task<Result<int>> task1 = Task.FromResult(Result.Fail<int>("Error 1", "ERR1"));
        Task<Result<int>> task2 = Task.FromResult(Result.Ok(2));
        Task<Result<int>> task3 = Task.FromResult(Result.Fail<int>("Error 3", "ERR3"));

        Result<IReadOnlyList<int>> combined = await ResultCombineExtensions.CombineAllAsync(task1, task2, task3);

        Error? error = combined.Match(ok => (Error?)null, fail => fail.Error);
        Assert.NotNull(error);
        Assert.Equal(2, error.InnerErrors!.Count);
    }

    [Fact]
    public async Task ZipAsync_WithSelector_ShouldCombineValues()
    {
        Task<Result<int>> task1 = Task.FromResult(Result.Ok(5));
        Task<Result<int>> task2 = Task.FromResult(Result.Ok(3));

        Result<int> combined = await task1.ZipAsync(task2, (a, b) => a + b);

        Assert.Equal(8, combined.Match(ok => ok.Value, fail => 0));
    }
}
