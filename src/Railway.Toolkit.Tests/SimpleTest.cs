using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

public class SimpleTest
{
    [Fact]
    public void CanCreateResult()
    {
        var result = Result.Ok(5);
        Assert.NotNull(result);
    }

    [Fact]
    public void CanUseMap()
    {
        var result = Result.Ok(5);
        var mapped = ResultMapExtensions.Map(result, x => x * 2);
        Assert.NotNull(mapped);
    }
}
