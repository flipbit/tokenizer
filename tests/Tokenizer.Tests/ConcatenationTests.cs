using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class ConcatenationTests : TokenizerTestBase
{
    private readonly ITokenizer tokenizer;

    private class Foo
    {
        public string Name { get; set; } = null!;
    }

    public ConcatenationTests(ITestOutputHelper output) : base(output)
    {
        tokenizer = CreateTokenizer();
    }

    [Fact]
    public void TestConcatTwoValues()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat }";
        const string input = @"Name: Alice, Name: Bob";

        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        Assert.Single(result.Matches);

        Assert.Equal("AliceBob", result.First("Name"));
    }

    [Fact]
    public void TestConcatTwoValuesWithReflectedObject()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat }";
        const string input = @"Name: Alice, Name: Bob";

        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize<Foo>(template, input);

        Assert.Single(result.Tokens.Matches);

        Assert.Equal("AliceBob", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
        Assert.Equal("AliceBob", result.Value.Name);
    }

    [Fact]
    public void TestConcatTwoValuesWithSeparator()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat(', ') }";
        const string input = @"Name: Alice, Name: Bob";

        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        Assert.Single(result.Matches);

        Assert.Equal("Alice, Bob", result.First("Name"));
    }

    [Fact]
    public void TestConcatTwoValuesWithReflectedObjectWithSeparator()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat(', ') }";
        const string input = @"Name: Alice, Name: Bob";

        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize<Foo>(template, input);

        Assert.Single(result.Tokens.Matches);

        Assert.Equal("Alice, Bob", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
        Assert.Equal("Alice, Bob", result.Value.Name);
    }

    [Fact]
    public void TestConcatTwoValuesWithNewLineSeparator()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat('<CR>') }";
        const string input = @"Name: Alice, Name: Bob";

        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize(template, input);

        Assert.Single(result.Matches);

        Assert.Equal($"Alice{Environment.NewLine}Bob", result.First("Name"));
    }

    [Fact]
    public void TestConcatTwoValuesWithReflectedObjectWithNewLineSeparator()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat('<CR>') }";
        const string input = @"Name: Alice, Name: Bob";

        var template = tokenizer.Compile(pattern).Template;
        var result = tokenizer.Tokenize<Foo>(template, input);

        Assert.Single(result.Tokens.Matches);

        Assert.Equal($"Alice{Environment.NewLine}Bob", result.Tokens.Matches.First(m => m.Token.Name == "Name").Value);
        Assert.Equal($"Alice{Environment.NewLine}Bob", result.Value.Name);
    }
}
