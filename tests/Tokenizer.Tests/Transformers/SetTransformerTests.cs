using System;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Transformers;

public class SetTransformerTests : Tests.TokenizerTestBase
{
    public SetTransformerTests(ITestOutputHelper output) : base(output)
    {
    }

    private readonly SetTransformer transformer = new();

    [Fact]
    public void GivenInputWithSetValue_WhenTransforming_ThenReturnsSetValue()
    {
        // Arrange
        var input = "input";
        var setValue = "output";

        // Act
        var result = transformer.CanTransform(input, [setValue], out var transformed);

        // Assert
        Assert.True(result);
        Assert.Equal("output", transformed);
    }

    [Fact]
    public void GivenEmptyString_WhenTransforming_ThenThrowsArgumentException()
    {
        // Arrange
        var input = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.CanTransform(input, null!, out var t)); ;
    }

    [Fact]
    public void GivenTransformerWithTooManyArguments_WhenTransforming_ThenThrowsArgumentException()
    {
        // Arrange
        var input = "input";
        string[] tooManyArgs = ["1", "2"];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => transformer.CanTransform(input, tooManyArgs, out var t));
    }

    [Fact]
    public void GivenTemplateWithSetTransformer_WhenTokenizing_ThenSetsValueToSpecifiedConstant()
    {
        // Arrange
        var pattern = @"Name: { Name : Set('Alice') }";
        var input = "Name: Bob";

        // Act
        var result = Tokenizer.Create().Tokenize(pattern, input);

        // Assert
        Assert.Equal("Alice", result.First("Name"));
    }

    [Fact]
    public void GivenTemplateWithShorthandSetSyntax_WhenTokenizing_ThenAppliesSetAndSubsequentTransformers()
    {
        // Arrange
        var pattern = @"Name: { Name = 'Alice' : ToUpper }";
        var input = "Name: Bob";

        // Act
        var result = Tokenizer.Create().Tokenize(pattern, input);

        // Assert
        Assert.Equal("ALICE", result.First("Name"));
    }
}
