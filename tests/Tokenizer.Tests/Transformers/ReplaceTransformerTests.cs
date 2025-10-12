using Xunit;
using Tokens.Exceptions;

namespace Tokens.Transformers;

public class ReplaceTransformerTests
{
    private readonly ReplaceTransformer transformer = new();

    [Fact]
    public void TestSubstringAfter()
    {
        var result = transformer.CanTransform("one two three", ["two", "four"], out var transformed);

        Assert.True(result);
        Assert.Equal("one four three", transformed);
    }

    [Fact]
    public void TestSubstringAfterWhenMissingArgument()
    {
        Assert.Throws<TokenizerException>(() => transformer.CanTransform("one two three", null, out var t));
    }

    [Fact]
    public void TestSubstringAfterWhenEmpty()
    {
        var result = transformer.CanTransform(string.Empty, null, out var transformed);

        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void TestSubstringAfterWhenNull()
    {
        var result = transformer.CanTransform(null, null, out var transformed);

        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }
}