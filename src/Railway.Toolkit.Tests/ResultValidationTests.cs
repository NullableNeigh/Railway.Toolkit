using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Validation")]
public class ResultValidationTests
{
    // -------------------------------------------------------------------------
    // String validators
    // -------------------------------------------------------------------------

    [Fact]
    public void NotNullOrEmpty_WithNonEmptyString_ShouldReturnOk()
    {
        Result<string> result = Result.Ok("hello").NotNullOrEmpty("Name");

        Assert.IsType<Result<string>.Ok>(result);
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyString_ShouldReturnFail()
    {
        Result<string> result = Result.Ok("").NotNullOrEmpty("Name");

        Assert.IsType<Result<string>.Fail>(result);
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyString_ShouldHaveCorrectMessageAndCode()
    {
        Result<string> result = Result.Ok("").NotNullOrEmpty("Email");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.Equal("Email cannot be null or empty", fail.Error.Message);
        Assert.Equal("Validation.Required", fail.Error.Code);
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyString_ShouldPopulateDetails()
    {
        Result<string> result = Result.Ok("").NotNullOrEmpty("Email");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.NotNull(fail.Error.Details);
        Assert.True(fail.Error.Details!.ContainsKey("Email"));
        Assert.Contains("cannot be null or empty", fail.Error.Details["Email"]);
    }

    [Fact]
    public void NotNullOrEmpty_WithDefaultFieldName_ShouldUseValueAsKey()
    {
        Result<string> result = Result.Ok("").NotNullOrEmpty();

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.True(fail.Error.Details!.ContainsKey("Value"));
    }

    [Fact]
    public void NotNullOrEmpty_WhenAlreadyFailed_ShouldPassThroughError()
    {
        Error originalError = Error.Create("original", "Original.Error");
        Result<string> result = Result.Fail<string>(originalError).NotNullOrEmpty("Email");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.Equal("original", fail.Error.Message);
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithWhitespace_ShouldReturnFail()
    {
        Result<string> result = Result.Ok("   ").NotNullOrWhiteSpace("Name");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.Equal("Validation.Required", fail.Error.Code);
        Assert.True(fail.Error.Details!.ContainsKey("Name"));
    }

    [Fact]
    public void NotNullOrWhiteSpace_WithNonWhitespace_ShouldReturnOk()
    {
        Result<string> result = Result.Ok("hello").NotNullOrWhiteSpace("Name");

        Assert.IsType<Result<string>.Ok>(result);
    }

    [Fact]
    public void HasMinLength_AboveMinimum_ShouldReturnOk()
    {
        Result<string> result = Result.Ok("hello").HasMinLength(3, "Name");

        Assert.IsType<Result<string>.Ok>(result);
    }

    [Fact]
    public void HasMinLength_BelowMinimum_ShouldReturnFail()
    {
        Result<string> result = Result.Ok("hi").HasMinLength(5, "Name");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.Equal("Validation.Length", fail.Error.Code);
        Assert.Contains("must be at least 5 characters", fail.Error.Details!["Name"]);
    }

    [Fact]
    public void HasMaxLength_BelowMaximum_ShouldReturnOk()
    {
        Result<string> result = Result.Ok("hi").HasMaxLength(10, "Name");

        Assert.IsType<Result<string>.Ok>(result);
    }

    [Fact]
    public void HasMaxLength_AboveMaximum_ShouldReturnFail()
    {
        Result<string> result = Result.Ok("hello world").HasMaxLength(5, "Name");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.Equal("Validation.Length", fail.Error.Code);
        Assert.Contains("must not exceed 5 characters", fail.Error.Details!["Name"]);
    }

    // -------------------------------------------------------------------------
    // Numeric validators (generic)
    // -------------------------------------------------------------------------

    [Fact]
    public void GreaterThan_AboveMinimum_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(10).GreaterThan(5, "Age");

        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public void GreaterThan_EqualToMinimum_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(5).GreaterThan(5, "Age");

        Assert.IsType<Result<int>.Fail>(result);
    }

    [Fact]
    public void GreaterThan_BelowMinimum_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(3).GreaterThan(5, "Age");

        Result<int>.Fail fail = Assert.IsType<Result<int>.Fail>(result);
        Assert.Equal("Validation.Range", fail.Error.Code);
        Assert.Contains("must be greater than 5", fail.Error.Details!["Age"]);
    }

    [Fact]
    public void GreaterThan_WorksWithDecimal()
    {
        Result<decimal> result = Result.Ok(1.5m).GreaterThan(2.0m, "Price");

        Result<decimal>.Fail fail = Assert.IsType<Result<decimal>.Fail>(result);
        Assert.True(fail.Error.Details!.ContainsKey("Price"));
    }

    [Fact]
    public void LessThan_BelowMaximum_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(3).LessThan(5, "Count");

        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public void LessThan_EqualToMaximum_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(5).LessThan(5, "Count");

        Assert.IsType<Result<int>.Fail>(result);
    }

    [Fact]
    public void LessThan_AboveMaximum_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(10).LessThan(5, "Count");

        Result<int>.Fail fail = Assert.IsType<Result<int>.Fail>(result);
        Assert.Equal("Validation.Range", fail.Error.Code);
        Assert.Contains("must be less than 5", fail.Error.Details!["Count"]);
    }

    [Fact]
    public void InRange_WithinRange_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(5).InRange(1, 10, "Score");

        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public void InRange_AtLowerBound_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(1).InRange(1, 10, "Score");

        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public void InRange_AtUpperBound_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(10).InRange(1, 10, "Score");

        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public void InRange_BelowRange_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(0).InRange(1, 10, "Score");

        Result<int>.Fail fail = Assert.IsType<Result<int>.Fail>(result);
        Assert.Equal("Validation.Range", fail.Error.Code);
        Assert.Contains("must be between 1 and 10", fail.Error.Details!["Score"]);
    }

    [Fact]
    public void InRange_AboveRange_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(11).InRange(1, 10, "Score");

        Assert.IsType<Result<int>.Fail>(result);
    }

    // -------------------------------------------------------------------------
    // GreaterThanZero
    // -------------------------------------------------------------------------

    [Fact]
    public void GreaterThanZero_Int_WithPositiveValue_ShouldReturnOk()
    {
        Result<int> result = Result.Ok(1).GreaterThanZero("Count");

        Assert.IsType<Result<int>.Ok>(result);
    }

    [Fact]
    public void GreaterThanZero_Int_WithZero_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(0).GreaterThanZero("Count");

        Result<int>.Fail fail = Assert.IsType<Result<int>.Fail>(result);
        Assert.Equal("Validation.Range", fail.Error.Code);
        Assert.Contains("must be greater than zero", fail.Error.Details!["Count"]);
    }

    [Fact]
    public void GreaterThanZero_Int_WithNegativeValue_ShouldReturnFail()
    {
        Result<int> result = Result.Ok(-5).GreaterThanZero("Count");

        Assert.IsType<Result<int>.Fail>(result);
    }

    [Fact]
    public void GreaterThanZero_Decimal_WithPositiveValue_ShouldReturnOk()
    {
        Result<decimal> result = Result.Ok(0.01m).GreaterThanZero("Price");

        Assert.IsType<Result<decimal>.Ok>(result);
    }

    [Fact]
    public void GreaterThanZero_Decimal_WithZero_ShouldReturnFail()
    {
        Result<decimal> result = Result.Ok(0m).GreaterThanZero("Price");

        Result<decimal>.Fail fail = Assert.IsType<Result<decimal>.Fail>(result);
        Assert.True(fail.Error.Details!.ContainsKey("Price"));
    }

    // -------------------------------------------------------------------------
    // Collection validators
    // -------------------------------------------------------------------------

    [Fact]
    public void NotNullOrEmpty_WithNonEmptyCollection_ShouldReturnOk()
    {
        Result<IEnumerable<string>> result = Result.Ok<IEnumerable<string>>(new[] { "a", "b" }).NotNullOrEmpty("Tags");

        Assert.IsType<Result<IEnumerable<string>>.Ok>(result);
    }

    [Fact]
    public void NotNullOrEmpty_WithEmptyCollection_ShouldReturnFail()
    {
        Result<IEnumerable<string>> result = Result.Ok<IEnumerable<string>>(Array.Empty<string>()).NotNullOrEmpty("Tags");

        Result<IEnumerable<string>>.Fail fail = Assert.IsType<Result<IEnumerable<string>>.Fail>(result);
        Assert.Equal("Validation.Required", fail.Error.Code);
        Assert.True(fail.Error.Details!.ContainsKey("Tags"));
    }

    [Fact]
    public void HasMinCount_MeetsMinimum_ShouldReturnOk()
    {
        Result<IEnumerable<int>> result = Result.Ok<IEnumerable<int>>(new[] { 1, 2, 3 }).HasMinCount(2, "Items");

        Assert.IsType<Result<IEnumerable<int>>.Ok>(result);
    }

    [Fact]
    public void HasMinCount_BelowMinimum_ShouldReturnFail()
    {
        Result<IEnumerable<int>> result = Result.Ok<IEnumerable<int>>(new[] { 1 }).HasMinCount(3, "Items");

        Result<IEnumerable<int>>.Fail fail = Assert.IsType<Result<IEnumerable<int>>.Fail>(result);
        Assert.Equal("Validation.Count", fail.Error.Code);
        Assert.Contains("must contain at least 3 items", fail.Error.Details!["Items"]);
    }

    [Fact]
    public void HasMaxCount_MeetsMaximum_ShouldReturnOk()
    {
        Result<IEnumerable<int>> result = Result.Ok<IEnumerable<int>>(new[] { 1, 2 }).HasMaxCount(5, "Items");

        Assert.IsType<Result<IEnumerable<int>>.Ok>(result);
    }

    [Fact]
    public void HasMaxCount_ExceedsMaximum_ShouldReturnFail()
    {
        Result<IEnumerable<int>> result = Result.Ok<IEnumerable<int>>(new[] { 1, 2, 3, 4, 5, 6 }).HasMaxCount(5, "Items");

        Result<IEnumerable<int>>.Fail fail = Assert.IsType<Result<IEnumerable<int>>.Fail>(result);
        Assert.Equal("Validation.Count", fail.Error.Code);
        Assert.Contains("must not contain more than 5 items", fail.Error.Details!["Items"]);
    }

    // -------------------------------------------------------------------------
    // ToUnit
    // -------------------------------------------------------------------------

    [Fact]
    public void ToUnit_OnSuccess_ShouldReturnOkUnit()
    {
        Result<Unit> result = Result.Ok("hello").ToUnit();

        Assert.IsType<Result<Unit>.Ok>(result);
    }

    [Fact]
    public void ToUnit_OnFailure_ShouldPassThroughError()
    {
        Error error = Error.Create("something failed", "Some.Error");
        Result<Unit> result = Result.Fail<string>(error).ToUnit();

        Result<Unit>.Fail fail = Assert.IsType<Result<Unit>.Fail>(result);
        Assert.Equal("something failed", fail.Error.Message);
    }

    // -------------------------------------------------------------------------
    // Validation.Combine
    // -------------------------------------------------------------------------

    [Fact]
    public void Combine_AllValidationsPass_ShouldCallBuilder()
    {
        bool builderCalled = false;

        Result<string> result = Validation.Combine(
            new[]
            {
                Result.Ok("hello").NotNullOrEmpty("Name").ToUnit(),
                Result.Ok(25).GreaterThan(18, "Age").ToUnit(),
            },
            () =>
            {
                builderCalled = true;
                return "built";
            });

        Assert.IsType<Result<string>.Ok>(result);
        Assert.True(builderCalled);
    }

    [Fact]
    public void Combine_AllValidationsPass_ShouldReturnBuilderValue()
    {
        Result<string> result = Validation.Combine(
            new[]
            {
                Result.Ok("hello").NotNullOrEmpty("Name").ToUnit(),
            },
            () => "success value");

        Result<string>.Ok ok = Assert.IsType<Result<string>.Ok>(result);
        Assert.Equal("success value", ok.Value);
    }

    [Fact]
    public void Combine_OneValidationFails_ShouldReturnFail()
    {
        Result<string> result = Validation.Combine(
            new[]
            {
                Result.Ok("").NotNullOrEmpty("Email").ToUnit(),
                Result.Ok(25).GreaterThan(18, "Age").ToUnit(),
            },
            () => "built");

        Assert.IsType<Result<string>.Fail>(result);
    }

    [Fact]
    public void Combine_MultipleValidationsFail_ShouldMergeDetails()
    {
        Result<string> result = Validation.Combine(
            new[]
            {
                Result.Ok("").NotNullOrEmpty("Email").ToUnit(),
                Result.Ok(15).GreaterThan(18, "Age").ToUnit(),
            },
            () => "built");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.NotNull(fail.Error.Details);
        Assert.True(fail.Error.Details!.ContainsKey("Email"));
        Assert.True(fail.Error.Details.ContainsKey("Age"));
    }

    [Fact]
    public void Combine_ValidationFails_ShouldHaveValidationFailedCode()
    {
        Result<string> result = Validation.Combine(
            new[]
            {
                Result.Ok("").NotNullOrEmpty("Email").ToUnit(),
            },
            () => "built");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.Equal("Validation.Failed", fail.Error.Code);
        Assert.Equal("Validation failed", fail.Error.Message);
    }

    [Fact]
    public void Combine_BuilderNotCalled_WhenValidationFails()
    {
        bool builderCalled = false;

        Validation.Combine(
            new[]
            {
                Result.Ok("").NotNullOrEmpty("Email").ToUnit(),
            },
            () =>
            {
                builderCalled = true;
                return "built";
            });

        Assert.False(builderCalled);
    }

    [Fact]
    public void Combine_WithoutBuilder_AllPass_ShouldReturnOkUnit()
    {
        Result<Unit> result = Validation.Combine(
            new[]
            {
                Result.Ok("hello").NotNullOrEmpty("Name").ToUnit(),
                Result.Ok(5).GreaterThanZero("Count").ToUnit(),
            });

        Assert.IsType<Result<Unit>.Ok>(result);
    }

    [Fact]
    public void Combine_WithoutBuilder_SomeFail_ShouldReturnFail()
    {
        Result<Unit> result = Validation.Combine(
            new[]
            {
                Result.Ok("").NotNullOrEmpty("Name").ToUnit(),
            });

        Assert.IsType<Result<Unit>.Fail>(result);
    }

    [Fact]
    public void Combine_MultipleErrorsOnSameField_ShouldAggregateMessages()
    {
        // "test" (length 4) fails HasMinLength(5) and HasMaxLength(3) — both target "Password"
        Result<string> result = Validation.Combine(
            new[]
            {
                Result.Ok("test").HasMinLength(5, "Password").ToUnit(),
                Result.Ok("test").HasMaxLength(3, "Password").ToUnit(),
            },
            () => "built");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.Equal(2, fail.Error.Details!["Password"].Length);
    }

    [Fact]
    public void Combine_NonStructuredError_ShouldGoToGeneralBucket()
    {
        Result<Unit> plainFail = Result.Fail<Unit>(Error.Create("something went wrong", "General.Error")).ToUnit();

        Result<string> result = Validation.Combine(
            new[] { plainFail },
            () => "built");

        Result<string>.Fail fail = Assert.IsType<Result<string>.Fail>(result);
        Assert.True(fail.Error.Details!.ContainsKey(""));
        Assert.Contains("something went wrong", fail.Error.Details[""]);
    }
}
