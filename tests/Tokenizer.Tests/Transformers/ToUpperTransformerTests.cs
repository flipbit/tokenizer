using Xunit;

namespace Tokens.Transformers;

public class ToUpperTransformerTests
{
    private readonly ToUpperTransformer transformer = new();

    [Fact]
    public void TestToUpper()
    {
        transformer.CanTransform("test", null, out var t);

        Assert.Equal("TEST", t);
    }

    [Fact]
    public void TestToUpperWhenEmpty()
    {
        transformer.CanTransform(string.Empty, null, out var t);

        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void TestToUpperWhenNull()
    {
        transformer.CanTransform(null, null, out var t);

        Assert.Equal(string.Empty, t);
    }
}