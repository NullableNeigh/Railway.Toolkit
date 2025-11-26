using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

public class UnitTests
{
    [Fact]
    public void Unit_ShouldHaveSingletonValue()
    {
        var unit1 = Unit.Value;
        var unit2 = Unit.Value;

        Assert.Equal(unit1, unit2);
    }

    [Fact]
    public void Unit_ToString_ShouldReturnParentheses()
    {
        var unit = Unit.Value;

        Assert.Equal("()", unit.ToString());
    }

    [Fact]
    public void Unit_CanBeUsedInResult()
    {
        var result = Result.Ok(Unit.Value);

        var value = result.Match(
            ok => ok.Value,
            fail => default
        );

        Assert.Equal(Unit.Value, value);
    }

    [Fact]
    public void Try_WithAction_ReturnsUnitOnSuccess()
    {
        var executed = false;
        var result = ResultTryExtensions.Try(() => executed = true);

        var value = result.Match(
            ok => ok.Value,
            fail => default
        );

        Assert.True(executed);
        Assert.Equal(Unit.Value, value);
    }

    [Fact]
    public async Task TryAsync_WithAction_ReturnsUnitOnSuccess()
    {
        var executed = false;
        var result = await ResultTryExtensions.TryAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        var value = result.Match(
            ok => ok.Value,
            fail => default
        );

        Assert.True(executed);
        Assert.Equal(Unit.Value, value);
    }

    [Fact]
    public void Unit_InPipeline_CanBeChained()
    {
        var result = Railway.Start(Unit.Value)
            .Map(_ => 42)
            .Map(x => x.ToString());

        var value = result.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("42", value);
    }
}
