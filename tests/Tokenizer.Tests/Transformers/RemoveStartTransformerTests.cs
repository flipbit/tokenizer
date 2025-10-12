using Tokens.Exceptions;
using Xunit;

namespace Tokens.Transformers;

public class RemoveStartTransformerTests
{
    private readonly RemoveStartTransformer transformer = new();

    [Fact]
    public void GivenStringStartingWithSubstring_WhenTransforming_ThenRemovesStartingSubstring()
    {
        // Arrange
        var input = "one two three";
        var prefixToRemove = "one";

        // Act
        var result = transformer.CanTransform(input, [prefixToRemove], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(" two three", transformed);
    }

    [Fact]
    public void GivenStringNotStartingWithSubstring_WhenTransforming_ThenReturnsOriginalString()
    {
        // Arrange
        var input = "one two three";
        var prefixToRemove = "two";

        // Act
        var result = transformer.CanTransform(input, [prefixToRemove], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("one two three", transformed);
    }

    [Fact]
    public void GivenTransformerWithMissingArgument_WhenTransforming_ThenThrowsTokenizerException()
    {
        // Arrange
        var input = "one two three";

        // Act & Assert
        Assert.Throws<TokenizerException>(() => transformer.CanTransform(input, null, out var t));
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = transformer.CanTransform(input, null, out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        string input = null;

        // Act
        var result = transformer.CanTransform(input, null, out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenTemplateWithRemoveEndTransformer_WhenTokenizingInput_ThenRemovesEndCharacter()
    {
        // Arrange
        var template = ": { DomainName : RemoveEnd('.') }";
        var input = "Domain Name: domain.com.";

        // Act
        var result = new Tokenizer().Tokenize(template, input);

        // Assert
        Assert.Equal("domain.com", result.First("DomainName"));
    }
}