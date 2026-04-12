using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

public class SimpleTest
{
    [Fact]
    public void CanCreateResult()
    {
        Result<int> result = Result.Ok(5);
        Assert.NotNull(result);
    }

    [Fact]
    public void CanUseMap()
    {
        Result<int> result = Result.Ok(5);
        Result<int> mapped = ResultMapExtensions.Map(result, x => x * 2);
        Assert.NotNull(mapped);
    }
}
