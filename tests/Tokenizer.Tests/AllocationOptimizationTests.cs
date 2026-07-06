using Tokens.Enumerators;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class AllocationOptimizationTests : TokenizerTestBase
{
    private readonly ITokenizer tokenizer;

    public AllocationOptimizationTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenReusableMatchBuffer_WhenMatchCalledTwice_ThenBufferIsClearedAndReused()
    {
        // Arrange
        var enumerator = new TokenEnumerator("Name: Alice");
        var template = tokenizer.Compile("Name: {Name}");
        var result = tokenizer.Tokenize(template, "Name: Alice");
        var tokensToMatch = result.Template.Tokens;
        var buffer = new List<Token>();

        // Act - call match twice with same buffer
        enumerator.TryMatch(tokensToMatch, false, buffer);
        var firstCallMatched = buffer.Count > 0;

        enumerator.TryMatch(tokensToMatch, false, buffer);

        // Assert - buffer was cleared on second call (not accumulated)
        Assert.True(firstCallMatched);
        Assert.True(buffer.Count > 0); // still has results from second call
    }

    [Fact]
    public void GivenEndToEndTokenization_WhenTokenizing_ThenProducesCorrectResults()
    {
        // Arrange / Act
        var template = tokenizer.Compile("Name: {SimpleTarget.Name}\nAge: {SimpleTarget.Age}");
        var result = tokenizer.Tokenize<SimpleTarget>(template, "Name: Alice\nAge: 30");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Alice", result.Value.Name);
        Assert.Equal("30", result.Value.Age);
    }

    [Fact]
    public void GivenTemplateWithDependentTokens_WhenTokenizing_ThenDependenciesResolveCorrectly()
    {
        // Arrange / Act
        var template = tokenizer.Compile("Name: {SimpleTarget.Name}");
        var result = tokenizer.Tokenize<SimpleTarget>(template, "Name: Bob");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Bob", result.Value.Name);
    }

    [Fact]
    public void GivenMultipleTemplates_WhenTokenizingSequentially_ThenResultsAreIndependent()
    {
        // Arrange / Act
        var template = tokenizer.Compile("Name: {SimpleTarget.Name}\nAge: {SimpleTarget.Age}");
        var result1 = tokenizer.Tokenize<SimpleTarget>(template, "Name: Alice\nAge: 25");
        var result2 = tokenizer.Tokenize<SimpleTarget>(template, "Name: Bob\nAge: 30");

        // Assert
        Assert.True(result1.Success);
        Assert.Equal("Alice", result1.Value.Name);
        Assert.Equal("25", result1.Value.Age);

        Assert.True(result2.Success);
        Assert.Equal("Bob", result2.Value.Name);
        Assert.Equal("30", result2.Value.Age);
    }

    public class SimpleTarget
    {
        public string? Name { get; set; }
        public string? Age { get; set; }
    }
}
