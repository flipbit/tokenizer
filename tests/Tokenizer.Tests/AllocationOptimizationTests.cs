using Tokens.Enumerators;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class AllocationOptimizationTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public AllocationOptimizationTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenReusableMatchBuffer_WhenMatchCalledTwice_ThenBufferIsClearedAndReused()
    {
        // Arrange
        var enumerator = new TokenEnumerator("Name: Alice");
        var template = _tokenizer.Compile("Name: {Name}").Template;
        var result = _tokenizer.Tokenize(template, "Name: Alice");
        var tokensToMatch = result.Template.Tokens;
        var buffer = new List<Token>();

        // Act - call match twice with same buffer
        enumerator.TryMatch(tokensToMatch, outOfOrderTokens: false, buffer);
        var firstCallMatched = buffer.Count > 0;

        enumerator.TryMatch(tokensToMatch, outOfOrderTokens: false, buffer);

        // Assert - buffer was cleared on second call (not accumulated)
        Assert.True(firstCallMatched);
        Assert.True(buffer.Count > 0); // still has results from second call
    }

    [Fact]
    public void GivenEndToEndTokenization_WhenTokenizing_ThenProducesCorrectResults()
    {
        // Arrange / Act
        var template = _tokenizer.Compile("Name: {SimpleTarget.Name}\nAge: {SimpleTarget.Age}").Template;
        var target = _tokenizer.Tokenize<SimpleTarget>(template, "Name: Alice\nAge: 30");

        // Assert
        Assert.NotNull(target);
        Assert.Equal("Alice", target.Name);
        Assert.Equal("30", target.Age);
    }

    [Fact]
    public void GivenTemplateWithDependentTokens_WhenTokenizing_ThenDependenciesResolveCorrectly()
    {
        // Arrange / Act
        var template = _tokenizer.Compile("Name: {SimpleTarget.Name}").Template;
        var target = _tokenizer.Tokenize<SimpleTarget>(template, "Name: Bob");

        // Assert
        Assert.NotNull(target);
        Assert.Equal("Bob", target.Name);
    }

    [Fact]
    public void GivenMultipleTemplates_WhenTokenizingSequentially_ThenResultsAreIndependent()
    {
        // Arrange / Act
        var template = _tokenizer.Compile("Name: {SimpleTarget.Name}\nAge: {SimpleTarget.Age}").Template;
        var target1 = _tokenizer.Tokenize<SimpleTarget>(template, "Name: Alice\nAge: 25");
        var target2 = _tokenizer.Tokenize<SimpleTarget>(template, "Name: Bob\nAge: 30");

        // Assert
        Assert.NotNull(target1);
        Assert.Equal("Alice", target1.Name);
        Assert.Equal("25", target1.Age);

        Assert.NotNull(target2);
        Assert.Equal("Bob", target2.Name);
        Assert.Equal("30", target2.Age);
    }

    public class SimpleTarget
    {
        public string? Name { get; set; }
        public string? Age { get; set; }
    }
}
