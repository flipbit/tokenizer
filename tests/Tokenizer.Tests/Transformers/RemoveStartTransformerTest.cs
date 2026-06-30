using Tokens.Exceptions;
using Xunit;

namespace Tokens.Transformers;

public class RemoveStartTransformerTest
{
    private readonly RemoveStartTransformer transformer = new();

    [Fact]
    public void TestRemoveStart()
    {
        var result = transformer.CanTransform("one two three", ["one"], out var transformed);

        Assert.True(result);
        Assert.Equal(" two three", transformed);
    }

    [Fact]
    public void TestRemoveStartWhenNotPresent()
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