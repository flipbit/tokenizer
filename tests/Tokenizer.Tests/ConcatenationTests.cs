using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class ConcatenationTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    private sealed class Foo
    {
        public string Name { get; set; } = null!;
    }

    public ConcatenationTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void TestConcatTwoValues()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat }";
        const string input = @"Name: Alice, Name: Bob";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);

        Assert.Single(result.Matches);

        Assert.Equal("AliceBob", result.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void TestConcatTwoValuesWithReflectedObject()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat }";
        const string input = @"Name: Alice, Name: Bob";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Foo>(template, input);

        Assert.Single(result.Tokens.Matches);

        Assert.Equal("AliceBob", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
        Assert.Equal("AliceBob", result.Value.Name);
    }

    [Fact]
    public void TestConcatTwoValuesWithSeparator()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat(', ') }";
        const string input = @"Name: Alice, Name: Bob";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);

        Assert.Single(result.Matches);

        Assert.Equal("Alice, Bob", result.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void TestConcatTwoValuesWithReflectedObjectWithSeparator()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat(', ') }";
        const string input = @"Name: Alice, Name: Bob";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Foo>(template, input);

        Assert.Single(result.Tokens.Matches);

        Assert.Equal("Alice, Bob", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
        Assert.Equal("Alice, Bob", result.Value.Name);
    }

    [Fact]
    public void TestConcatTwoValuesWithNewLineSeparator()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat('<CR>') }";
        const string input = @"Name: Alice, Name: Bob";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize(template, input);

        Assert.Single(result.Matches);

        Assert.Equal($"Alice{Environment.NewLine}Bob", result.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
    }

    [Fact]
    public void TestConcatTwoValuesWithReflectedObjectWithNewLineSeparator()
    {
        const string pattern = @"Name: { Name }, Name: { Name : Concat('<CR>') }";
        const string input = @"Name: Alice, Name: Bob";

        var template = _tokenizer.Compile(pattern).Template;
        var result = _tokenizer.Tokenize<Foo>(template, input);

        Assert.Single(result.Tokens.Matches);

        Assert.Equal($"Alice{Environment.NewLine}Bob", result.Tokens.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
        Assert.Equal($"Alice{Environment.NewLine}Bob", result.Value.Name);
    }
}
