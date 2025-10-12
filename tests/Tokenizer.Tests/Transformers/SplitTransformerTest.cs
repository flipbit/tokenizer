using Xunit;
using Tokens.Exceptions;

namespace Tokens.Transformers;

public class SplitTransformerTest
{
    private readonly SplitTransformer transformer = new();

    [Fact]
    public void TestSplitInput()
    {
        var result = transformer.CanTransform("1,2,3,4", [","], out var transformed);

        Assert.True(result);

        var list = transformed as string[];

        Assert.Equal(4, list.Length);
        Assert.Equal("1", list[0]);
        Assert.Equal("2", list[1]);
        Assert.Equal("3", list[2]);
        Assert.Equal("4", list[3]);
    }

    [Fact]
    public void TestSplitInputWhenNoSeparator()
    {
        var result = transformer.CanTransform("1-2-3-4", [","], out var transformed);

        Assert.True(result);
        Assert.Equal("1-2-3-4", transformed);
    }

    [Fact]
    public void TestSplitWhenMissingArgument()
    {
        Assert.Throws<TokenizerException>(() => transformer.CanTransform("1,2,3,4", null, out var t));
    }

    [Fact]
    public void TestSplitWhenEmptyInput()
    {
        var result = transformer.CanTransform(string.Empty, null, out var transformed);

        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }

    [Fact]
    public void TestSplitWhenNullInput()
    {
        var result = transformer.CanTransform(null, null, out var transformed);

        Assert.True(result);
        Assert.Equal(string.Empty, transformed);
    }
}