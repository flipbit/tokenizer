using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tokenization.Engine;

/// <summary>
/// Tests for TokenizationEngine behaviors that were previously tested through internal methods.
/// All tests now exercise behaviors through the public Tokenizer.Tokenize pipeline.
/// </summary>
public class TokenizationEngineTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    public TokenizationEngineTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenRepeatingToken_WhenInputDoesNotMatchRepeat_ThenBacktracks()
    {
        // Arrange
        var template = _tokenizer.Compile("test: {Name}").Template;

        // Act
        var result = _tokenizer.Tokenize(template, "test: hello");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("hello", result.Tokens.Matches.First().Value);
    }

    [Fact]
    public void GivenNewlineTerminatedToken_WhenInputHasNewline_ThenAssignsValueBeforeNewline()
    {
        // Arrange
        var template = _tokenizer.Compile("Name: {Name}\nAge: {Age}").Template;

        // Act
        var result = _tokenizer.Tokenize(template, "Name: Alice\nAge: 30");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Tokens.Matches.Count);
        Assert.Equal("Alice", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
        Assert.Equal("30", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Age", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void GivenFrontMatterToken_WhenTokenizing_ThenFrontMatterIsProcessed()
    {
        // Arrange — template with front matter and a body token
        var template = _tokenizer.Compile("---\nname: MyTemplate\n---\nName: {Name}").Template;

        // Act
        var result = _tokenizer.Tokenize(template, "Name: Bob");

        // Assert
        Assert.True(result.Success);
        Assert.Contains(result.Tokens.Matches, m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenCandidateTokens_WhenBothTokensPresent_ThenBothAreAssigned()
    {
        // Arrange — two tokens with distinct preambles and values
        var template = _tokenizer.Compile("A:{First}B:{Second}").Template;

        // Act
        var result = _tokenizer.Tokenize(template, "A:helloB:world");

        // Assert
        Assert.Contains(result.Tokens.Matches, m => string.Equals(m.Token.Name, "First", StringComparison.Ordinal) && string.Equals((string)m.Value, "hello", StringComparison.Ordinal));
        Assert.Contains(result.Tokens.Matches, m => string.Equals(m.Token.Name, "Second", StringComparison.Ordinal) && string.Equals((string)m.Value, "world", StringComparison.Ordinal));
    }

    [Fact]
    public void GivenTemplateWithOnlyFrontMatter_WhenTokenizing_ThenResultIsNotNull()
    {
        // Arrange
        var template = _tokenizer.Compile("---\nname: MyTemplate\n---\n").Template;

        // Act
        var result = _tokenizer.Tokenize(template, "anything");

        // Assert
        Assert.NotNull(result);
    }
}
