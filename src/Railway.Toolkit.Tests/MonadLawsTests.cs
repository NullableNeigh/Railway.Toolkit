using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

/// <summary>
/// Tests that Result<T> satisfies the monad laws.
/// These laws ensure that the library behaves predictably and composably.
/// </summary>
[Trait("Category", "MonadLaws")]
public class MonadLawsTests
{
    // Monad Laws:
    // 1. Left Identity:  Result.Ok(a).Bind(f) == f(a)
    // 2. Right Identity: m.Bind(Result.Ok) == m
    // 3. Associativity:  m.Bind(f).Bind(g) == m.Bind(x => f(x).Bind(g))

    [Fact]
    public void Bind_LeftIdentity_ShouldHold()
    {
        // Left Identity: Result.Ok(a).Bind(f) should equal f(a)
        int value = 5;
        Func<int, Result<int>> f = x => Result.Ok(x * 2);

        Result<int> left = Result.Ok(value).Bind(f);
        Result<int> right = f(value);

        AssertResultsEqual(left, right);
    }

    [Fact]
    public void Bind_RightIdentity_ShouldHold()
    {
        // Right Identity: m.Bind(Result.Ok) should equal m
        Result<int> m = Result.Ok(42);

        Result<int> left = m.Bind(x => Result.Ok(x));
        Result<int> right = m;

        AssertResultsEqual(left, right);
    }

    [Fact]
    public void Bind_Associativity_ShouldHold()
    {
        // Associativity: m.Bind(f).Bind(g) should equal m.Bind(x => f(x).Bind(g))
        Result<int> m = Result.Ok(5);
        Func<int, Result<int>> f = x => Result.Ok(x * 2);
        Func<int, Result<int>> g = x => Result.Ok(x + 3);

        Result<int> left = m.Bind(f).Bind(g);
        Result<int> right = m.Bind(x => f(x).Bind(g));

        AssertResultsEqual(left, right);
    }

    [Fact]
    public void Bind_LeftIdentityWithFail_ShouldHold()
    {
        // Left identity should also hold when f returns Fail
        int value = 5;
        Func<int, Result<int>> f = x => Result.Fail<int>("Error", "TEST");

        Result<int> left = Result.Ok(value).Bind(f);
        Result<int> right = f(value);

        AssertResultsEqual(left, right);
    }

    [Fact]
    public void Bind_RightIdentityWithFail_ShouldHold()
    {
        // Right identity should hold even for Fail
        Result<int> m = Result.Fail<int>("Error", "TEST");

        Result<int> left = m.Bind(x => Result.Ok(x));
        Result<int> right = m;

        AssertResultsEqual(left, right);
    }

    [Fact]
    public void Bind_AssociativityWithFail_ShouldHold()
    {
        // Associativity should hold when operations fail
        Result<int> m = Result.Ok(5);
        Func<int, Result<int>> f = x => Result.Fail<int>("f failed", "F");
        Func<int, Result<int>> g = x => Result.Ok(x + 3);

        Result<int> left = m.Bind(f).Bind(g);
        Result<int> right = m.Bind(x => f(x).Bind(g));

        AssertResultsEqual(left, right);
    }

    // Functor Laws for Map:
    // 1. Identity: m.Map(x => x) == m
    // 2. Composition: m.Map(f).Map(g) == m.Map(x => g(f(x)))

    [Fact]
    public void Map_Identity_ShouldHold()
    {
        // Identity: m.Map(x => x) should equal m
        Result<int> m = Result.Ok(42);

        Result<int> left = m.Map(x => x);
        Result<int> right = m;

        AssertResultsEqual(left, right);
    }

    [Fact]
    public void Map_Composition_ShouldHold()
    {
        // Composition: m.Map(f).Map(g) should equal m.Map(x => g(f(x)))
        Result<int> m = Result.Ok(5);
        Func<int, int> f = x => x * 2;
        Func<int, int> g = x => x + 3;

        Result<int> left = m.Map(f).Map(g);
        Result<int> right = m.Map(x => g(f(x)));

        AssertResultsEqual(left, right);
    }

    [Fact]
    public void Map_IdentityWithFail_ShouldHold()
    {
        Result<int> m = Result.Fail<int>("Error", "TEST");

        Result<int> left = m.Map(x => x);
        Result<int> right = m;

        AssertResultsEqual(left, right);
    }

    [Fact]
    public void Map_CompositionWithFail_ShouldHold()
    {
        Result<int> m = Result.Fail<int>("Error", "TEST");
        Func<int, int> f = x => x * 2;
        Func<int, int> g = x => x + 3;

        Result<int> left = m.Map(f).Map(g);
        Result<int> right = m.Map(x => g(f(x)));

        AssertResultsEqual(left, right);
    }

    // Map and Bind relationship:
    // m.Map(f) == m.Bind(x => Result.Ok(f(x)))

    [Fact]
    public void Map_ShouldBeBindWithOk()
    {
        Result<int> m = Result.Ok(5);
        Func<int, int> f = x => x * 2;

        Result<int> usingMap = m.Map(f);
        Result<int> usingBind = m.Bind(x => Result.Ok(f(x)));

        AssertResultsEqual(usingMap, usingBind);
    }

    [Fact]
    public void Map_ShouldBeBindWithOk_EvenForFail()
    {
        Result<int> m = Result.Fail<int>("Error", "TEST");
        Func<int, int> f = x => x * 2;

        Result<int> usingMap = m.Map(f);
        Result<int> usingBind = m.Bind(x => Result.Ok(f(x)));

        AssertResultsEqual(usingMap, usingBind);
    }

    // Helper to assert two results are equal
    private static void AssertResultsEqual<T>(Result<T> left, Result<T> right)
    {
        (bool, T?, Error?) leftValue = left.Match(
            ok => (true, ok.Value, (Error?)null),
            fail => (false, default(T), fail.Error)
        );

        (bool, T?, Error?) rightValue = right.Match(
            ok => (true, ok.Value, (Error?)null),
            fail => (false, default(T), fail.Error)
        );

        Assert.Equal(leftValue.Item1, rightValue.Item1); // Both Ok or both Fail

        if (leftValue.Item1)
        {
            Assert.Equal(leftValue.Item2, rightValue.Item2); // Values equal
        }
        else
        {
            Assert.Equal(leftValue.Item3?.Code, rightValue.Item3?.Code); // Error codes equal
            Assert.Equal(leftValue.Item3?.Message, rightValue.Item3?.Message); // Error messages equal
        }
    }
}
