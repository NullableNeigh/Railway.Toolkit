using Railway.Toolkit;

namespace Railway.Toolkit.Tests;

[Trait("Category", "Core")]
public class ErrorDetailsBuilderTests
{
    [Fact]
    public void Create_ShouldReturnNewBuilder()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();

        Assert.NotNull(builder);
    }

    [Fact]
    public void HasDetails_WhenEmpty_ShouldBeFalse()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();

        Assert.False(builder.HasDetails);
    }

    [Fact]
    public void HasDetails_AfterAddingDetail_ShouldBeTrue()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail("Email", "Invalid format");

        Assert.True(builder.HasDetails);
    }

    [Fact]
    public void AddDetail_ShouldStoreMessageUnderKey()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail("Email", "Invalid format");

        IReadOnlyDictionary<string, string[]> details = builder.Build();

        Assert.True(details.ContainsKey("Email"));
        Assert.Single(details["Email"]);
        Assert.Equal("Invalid format", details["Email"][0]);
    }

    [Fact]
    public void AddDetail_SameKeyTwice_ShouldAccumulateMessages()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail("Email", "Invalid format");
        builder.AddDetail("Email", "Already registered");

        IReadOnlyDictionary<string, string[]> details = builder.Build();

        Assert.Equal(2, details["Email"].Length);
        Assert.Contains("Invalid format", details["Email"]);
        Assert.Contains("Already registered", details["Email"]);
    }

    [Fact]
    public void AddDetail_DifferentKeys_ShouldStoreSeparately()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail("Email", "Invalid format");
        builder.AddDetail("Age", "Must be at least 18");

        IReadOnlyDictionary<string, string[]> details = builder.Build();

        Assert.Equal(2, details.Count);
        Assert.True(details.ContainsKey("Email"));
        Assert.True(details.ContainsKey("Age"));
    }

    [Fact]
    public void AddDetail_NullMessage_ShouldBeIgnored()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail("Email", null!);

        Assert.False(builder.HasDetails);
    }

    [Fact]
    public void AddDetail_WhitespaceMessage_ShouldBeIgnored()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail("Email", "   ");

        Assert.False(builder.HasDetails);
    }

    [Fact]
    public void AddDetail_NullKey_ShouldStoreUnderEmptyStringKey()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail(null!, "Some message");

        IReadOnlyDictionary<string, string[]> details = builder.Build();

        Assert.True(details.ContainsKey(""));
        Assert.Equal("Some message", details[""][0]);
    }

    [Fact]
    public void AddDetail_ShouldReturnBuilderForFluentChaining()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();

        ErrorDetailsBuilder returned = builder.AddDetail("Email", "Invalid format");

        Assert.Same(builder, returned);
    }

    [Fact]
    public void AddDetails_ShouldAddAllMessages()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetails("Email", "Invalid format", "Already registered");

        IReadOnlyDictionary<string, string[]> details = builder.Build();

        Assert.Equal(2, details["Email"].Length);
        Assert.Contains("Invalid format", details["Email"]);
        Assert.Contains("Already registered", details["Email"]);
    }

    [Fact]
    public void AddDetails_WithNullAndValidMessages_ShouldIgnoreNulls()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetails("Email", "Invalid format", null!, "   ");

        IReadOnlyDictionary<string, string[]> details = builder.Build();

        Assert.Single(details["Email"]);
        Assert.Equal("Invalid format", details["Email"][0]);
    }

    [Fact]
    public void AddDetails_ShouldReturnBuilderForFluentChaining()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();

        ErrorDetailsBuilder returned = builder.AddDetails("Email", "Invalid format");

        Assert.Same(builder, returned);
    }

    [Fact]
    public void FluentChaining_ShouldBuildCorrectDetails()
    {
        IReadOnlyDictionary<string, string[]> details = ErrorDetailsBuilder.Create()
            .AddDetail("Email", "Invalid format")
            .AddDetail("Email", "Already registered")
            .AddDetail("Age", "Must be at least 18")
            .AddDetails("Name", "Too short", "Contains invalid characters")
            .Build();

        Assert.Equal(3, details.Count);
        Assert.Equal(2, details["Email"].Length);
        Assert.Single(details["Age"]);
        Assert.Equal(2, details["Name"].Length);
    }

    [Fact]
    public void Build_ShouldReturnImmutableDictionary()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail("Email", "Invalid format");

        IReadOnlyDictionary<string, string[]> details = builder.Build();

        Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(details);
    }

    [Fact]
    public void Build_CalledTwice_ShouldReturnEquivalentResults()
    {
        ErrorDetailsBuilder builder = ErrorDetailsBuilder.Create();
        builder.AddDetail("Email", "Invalid format");

        IReadOnlyDictionary<string, string[]> first = builder.Build();
        IReadOnlyDictionary<string, string[]> second = builder.Build();

        Assert.Equal(first["Email"], second["Email"]);
    }
}
