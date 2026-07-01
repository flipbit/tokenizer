using Xunit;

namespace Tokens;

public class TokenPropertyImmutabilityTests
{
    [Fact]
    public void GivenTemplate_WhenCompiled_ThenTokenPropertiesAreSet()
    {
        // Arrange
        var tokenizer = Tokenizer.Create();

        // Act
        var result = tokenizer.Tokenize<TestClass>("Name: {TestClass.Name}\nAge: {TestClass.Age}", "Name: Alice\nAge: 30");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Alice", result.Value.Name);
    }

    [Fact]
    public void GivenOptionalToken_WhenCompiled_ThenOptionalIsTrue()
    {
        // Arrange
        var tokenizer = Tokenizer.Create();

        // Act
        var result = tokenizer.Tokenize("Name: {Name?}", "Name: Alice");

        // Assert
        var nameToken = Assert.Single(result.Tokens.Matches, m => m.Token.Name == "Name");
        Assert.True(nameToken.Token.IsOptional);
    }

    public class TestClass
    {
        public string? Name { get; set; }
        public string? Age { get; set; }
    }
}
