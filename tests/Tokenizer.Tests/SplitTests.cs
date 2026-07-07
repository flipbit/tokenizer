using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class SplitTests : TokenizerTestBase
{
    private readonly ITokenizer _tokenizer;

    private sealed class Foo
    {
        public List<string> Names { get; set; } = null!;
    }

    public SplitTests(ITestOutputHelper output) : base(output)
    {
        _tokenizer = CreateTokenizer();
    }

    [Fact]
    public void GivenCommaSeparatedNames_WhenTokenizingWithSplitTransformer_ThenReturnsListWithCorrectValues()
    {
        // Arrange
        const string pattern = @"Names: { Names : Split(',') }";
        const string input = @"Names: Alice,Bob,Charles";

        // Act
        var template = _tokenizer.Compile(pattern).Template;
        var results = _tokenizer.Tokenize<Foo>(template, input);
        var foo = results.Value;

        // Assert
        Assert.Equal(3, foo.Names.Count);
        Assert.Equal("Alice", foo.Names[0]);
        Assert.Equal("Bob", foo.Names[1]);
        Assert.Equal("Charles", foo.Names[2]);
    }
}
