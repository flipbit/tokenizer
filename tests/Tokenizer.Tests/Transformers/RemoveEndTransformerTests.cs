using Xunit;
using Xunit.Abstractions;

namespace Tokens.Transformers;

public class RemoveEndTransformerTests : TokenizerTestBase
{
    public RemoveEndTransformerTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly RemoveEndTransformer transformer = new();

    [Fact]
    public void GivenStringEndingWithSubstring_WhenTransforming_ThenRemovesEndingSubstring()
    {
        // Arrange
        var input = "one two three";
        var suffixToRemove = "three";

        // Act
        var result = transformer.TryTransform(input, [suffixToRemove], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("one two ", transformed);
    }

    [Fact]
    public void GivenStringNotEndingWithSubstring_WhenTransforming_ThenReturnsOriginalString()
    {
        // Arrange
        var input = "one two three";
        var suffixToRemove = "two";

        // Act
        var result = transformer.TryTransform(input, [suffixToRemove], out var transformed);

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
        Assert.Throws<ArgumentException>(() => transformer.TryTransform(input, null!, out var t));
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var result = transformer.TryTransform(input, null!, out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenNullValue_WhenTransforming_ThenReturnsEmptyString()
    {
        // Arrange
        string input = null!;

        // Act
        var result = transformer.TryTransform(input, null!, out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void GivenTemplateWithRemoveEndTransformer_WhenTokenizingInput_ThenRemovesEndCharacter()
    {
        // Arrange
        var template = "Domain Name: { DomainName : RemoveEnd('.') }";
        var input = "Domain Name: domain.com.";

        // Act
        var _tok = new Tokenizer();
        var compiled = _tok.Compile(template).Template;
        var result = _tok.Tokenize(compiled, input);

        // Assert
        Assert.Equal("domain.com", result.First("DomainName"));
    }
}
