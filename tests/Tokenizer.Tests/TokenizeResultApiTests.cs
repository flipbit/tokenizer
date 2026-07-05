using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens;

public class TokenizeResultApiTests
{
    // --- First(key) ---

    [Fact]
    public void GivenMatchingToken_WhenCallingFirst_ThenReturnsValue()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Name}").WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).Build();
        var result = new TokenizeResult(template);
        result.Tokens.AddMatch(token, "Alice", new FileLocation());

        // Act
        var value = result.First("Name");

        // Assert
        Assert.Equal("Alice", value);
    }

    [Fact]
    public void GivenMissingToken_WhenCallingFirst_ThenThrowsTokenizerException()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("Test").Build();
        var result = new TokenizeResult(template);

        // Act & Assert
        Assert.Throws<TokenizerException>(() => result.First("Missing"));
    }

    // --- First<T>(key) ---

    [Fact]
    public void GivenMatchingToken_WhenCallingFirstGeneric_ThenReturnsCastValue()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Count}").WithName("Count").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).Build();
        var result = new TokenizeResult(template);
        result.Tokens.AddMatch(token, 42, new FileLocation());

        // Act
        var value = result.First<int>("Count");

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GivenMissingToken_WhenCallingFirstGeneric_ThenThrowsTokenizerException()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("Test").Build();
        var result = new TokenizeResult(template);

        // Act & Assert
        Assert.Throws<TokenizerException>(() => result.First<string>("Missing"));
    }

    // --- FirstOrDefault(key) ---

    [Fact]
    public void GivenMatchingToken_WhenCallingFirstOrDefault_ThenReturnsValue()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Name}").WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).Build();
        var result = new TokenizeResult(template);
        result.Tokens.AddMatch(token, "Bob", new FileLocation());

        // Act
        var value = result.FirstOrDefault("Name");

        // Assert
        Assert.Equal("Bob", value);
    }

    [Fact]
    public void GivenMissingToken_WhenCallingFirstOrDefault_ThenReturnsNull()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("Test").Build();
        var result = new TokenizeResult(template);

        // Act
        var value = result.FirstOrDefault("Missing");

        // Assert
        Assert.Null(value);
    }

    // --- FirstOrDefault<T>(key) ---

    [Fact]
    public void GivenMatchingToken_WhenCallingFirstOrDefaultGeneric_ThenReturnsCastValue()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Score}").WithName("Score").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).Build();
        var result = new TokenizeResult(template);
        result.Tokens.AddMatch(token, 99, new FileLocation());

        // Act
        var value = result.FirstOrDefault<int>("Score");

        // Assert
        Assert.Equal(99, value);
    }

    [Fact]
    public void GivenMissingToken_WhenCallingFirstOrDefaultGeneric_ThenReturnsDefault()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("Test").Build();
        var result = new TokenizeResult(template);

        // Act
        var value = result.FirstOrDefault<int>("Missing");

        // Assert
        Assert.Equal(default, value);
    }

    // --- All(key) ---

    [Fact]
    public void GivenMultipleMatchingTokens_WhenCallingAll_ThenReturnsAllValues()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Tag}").WithName("Tag").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).Build();
        var result = new TokenizeResult(template);
        result.Tokens.AddMatch(token, "one", new FileLocation());
        result.Tokens.AddMatch(token, "two", new FileLocation());
        result.Tokens.AddMatch(token, "three", new FileLocation());

        // Act
        var values = result.All("Tag");

        // Assert
        Assert.Equal(3, values.Count);
        Assert.Contains("one", values);
        Assert.Contains("two", values);
        Assert.Contains("three", values);
    }

    [Fact]
    public void GivenNoMatchingTokens_WhenCallingAll_ThenReturnsEmpty()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("Test").Build();
        var result = new TokenizeResult(template);

        // Act
        var values = result.All("Missing");

        // Assert
        Assert.Empty(values);
    }

    // --- Contains(key) ---

    [Fact]
    public void GivenMatchingToken_WhenCallingContains_ThenReturnsTrue()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Name}").WithName("Name").Build();
        var template = new TemplateBuilder().WithName("Test").WithTokens(token).Build();
        var result = new TokenizeResult(template);
        result.Tokens.AddMatch(token, "Carol", new FileLocation());

        // Act
        var found = result.Contains("Name");

        // Assert
        Assert.True(found);
    }

    [Fact]
    public void GivenMissingToken_WhenCallingContains_ThenReturnsFalse()
    {
        // Arrange
        var template = new TemplateBuilder().WithName("Test").Build();
        var result = new TokenizeResult(template);

        // Act
        var found = result.Contains("Missing");

        // Assert
        Assert.False(found);
    }
}
