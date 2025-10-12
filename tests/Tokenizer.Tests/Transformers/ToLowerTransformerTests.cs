using Xunit;

namespace Tokens.Transformers;

public class ToLowerTransformerTests
{
    private readonly ToLowerTransformer transformer = new();

    [Fact]
    public void TestToLower()
    {
        var result = transformer.CanTransform("TEST", null, out var t);

        Assert.True(result);
        Assert.Equal("test", t);
    }

    [Fact]
    public void TestToLowerWhenEmpty()
    {
        var result = transformer.CanTransform(string.Empty, null, out var t);

        Assert.True(result);
        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void TestToLowerWhenNull()
    {
        var result = transformer.CanTransform(null, null, out var t);

        Assert.True(result);
        Assert.Equal(string.Empty, t);
    }
}