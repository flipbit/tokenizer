using Xunit;

namespace Tokens.Transformers;

public class TrimTransformerTests
{
    private readonly TrimTransformer transformer = new();

    [Fact]
    public void TestTrim()
    {
        transformer.CanTransform("  TEST  ", null, out var t);

        Assert.Equal("TEST", t);
    }

    [Fact]
    public void TestTrimWhenEmpty()
    {
        var result = transformer.CanTransform(string.Empty, null, out var t);

        Assert.Equal(string.Empty, t);
    }

    [Fact]
    public void TestTrimWhenNull()
    {
        var result = transformer.CanTransform(null, null, out var t);

        Assert.Equal(string.Empty, t);
    }
}