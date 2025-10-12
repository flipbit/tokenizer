using System;
using Xunit;

namespace Tokens.Transformers;

public class SetTransformerTests
{
    private readonly SetTransformer transformer = new();

    [Fact]
    public void TestSet()
    {
        var result = transformer.CanTransform("input", ["output"], out var transformed);

        Assert.True(result);
        Assert.Equal("output", transformed);
    }

    [Fact]
    public void TestSetWhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => transformer.CanTransform(string.Empty, null, out var t));;
    }

    [Fact]
    public void TestSetWhenTooManyArguments()
    {
        Assert.Throws<ArgumentException>(() => transformer.CanTransform("input", ["1", "2"], out var t));
    }

    [Fact]
    public void TestInTemplate()
    {
        var pattern = @"Name: { Name : Set('Alice') }";
        var input = "Name: Bob";

        var result = new Tokenizer().Tokenize(pattern, input);

        Assert.Equal("Alice", result.First("Name"));
    }

    [Fact]
    public void TestInTemplateWithShortHand()
    {
        var pattern = @"Name: { Name = 'Alice' : ToUpper }";
        var input = "Name: Bob";

        var result = new Tokenizer().Tokenize(pattern, input);

        Assert.Equal("ALICE", result.First("Name"));
    }
}