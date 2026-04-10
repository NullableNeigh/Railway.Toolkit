using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Unit")]
public class UnitTests
{
    [Fact]
    public void Unit_ShouldHaveSingletonValue()
    {
        Unit unit1 = Unit.Value;
        Unit unit2 = Unit.Value;

        Assert.Equal(unit1, unit2);
    }

    [Fact]
    public void Unit_ToString_ShouldReturnParentheses()
    {
        Unit unit = Unit.Value;

        Assert.Equal("()", unit.ToString());
    }

    [Fact]
    public void Unit_CanBeUsedInResult()
    {
        Result<Unit> result = Result.Ok(Unit.Value);

        Unit value = result.Match(
            ok => ok.Value,
            fail => default
        );

        Assert.Equal<Unit>(Unit.Value, value);
    }

    [Fact]
    public void Try_WithAction_ReturnsUnitOnSuccess()
    {
        bool executed = false;
        Result<Unit> result = ResultTryExtensions.Try(() => { executed = true; });

        Unit value = result.Match(
            ok => ok.Value,
            fail => default
        );

        Assert.True(executed);
        Assert.True(value == Unit.Value);
    }

    [Fact]
    public async Task TryAsync_WithAction_ReturnsUnitOnSuccess()
    {
        bool executed = false;
        Result<Unit> result = await ResultTryExtensions.TryAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        Unit value = result.Match(
            ok => ok.Value,
            fail => default
        );

        Assert.True(executed);
        Assert.True(value == Unit.Value);
    }

    [Fact]
    public void Unit_InPipeline_CanBeChained()
    {
        Result<string> result = Railway.Start(Unit.Value)
            .Map(_ => 42)
            .Map(x => x.ToString());

        string value = result.Match(
            ok => ok.Value,
            fail => ""
        );

        Assert.Equal("42", value);
    }
}
