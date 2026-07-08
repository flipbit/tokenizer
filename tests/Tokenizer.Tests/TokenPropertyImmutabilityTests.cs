using Xunit;

namespace Tokens;

public class TokenPropertyImmutabilityTests
{
    [Fact]
    public void GivenTemplate_WhenCompiled_ThenTokenPropertiesAreSet()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var template = tokenizer.Compile("Name: {TestClass.Name}\nAge: {TestClass.Age}").Template;
        var target = tokenizer.Tokenize<TestClass>(template, "Name: Alice\nAge: 30");

        // Assert
        Assert.NotNull(target);
        Assert.Equal("Alice", target.Name);
    }

    [Fact]
    public void GivenOptionalToken_WhenCompiled_ThenOptionalIsTrue()
    {
        // Arrange
        var tokenizer = new Tokenizer();

        // Act
        var template = tokenizer.Compile("Name: {Name?}").Template;
        var result = tokenizer.Tokenize(template, "Name: Alice");

        // Assert
        var nameToken = Assert.Single(result.Tokens.Matches, m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal));
        Assert.True(nameToken.Token.IsOptional);
    }

    public class TestClass
    {
        public string? Name { get; set; }
        public string? Age { get; set; }
    }
}
