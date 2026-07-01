using Tokens.Exceptions;
using Xunit;

namespace Tokens.Transformers;

public class RemoveEndTransformerTest
{
    private readonly RemoveEndTransformer transformer = new();

    [Fact]
    public void TestRemoveEnd()
    {
        var result = transformer.CanTransform("one two three", ["three"], out var transformed);

        Assert.True(result);
        Assert.Equal("one two ", transformed);
    }

    [Fact]
    public void TestRemoveEndWhenNotPresent()
    {
        var result = transformer.CanTransform("one two three", ["two"], out var transformed);

        Assert.True(result);
        Assert.Equal("one two three", transformed);
    }

    [Fact]
    public void TestSubstringAfterWhenMissingArgument()
    {
        Assert.Throws<TokenizerException>(() => transformer.CanTransform("one two three", null!, out var t));
    }

    [Fact]
    public void TestSubstringAfterWhenEmpty()
    {
        var result = transformer.CanTransform(string.Empty, null!, out var transformed);

        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void TestSubstringAfterWhenNull()
    {
        var result = transformer.CanTransform(null!, null!, out var transformed);

        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }
}
