using System.Collections.Generic;
using Xunit;

namespace Tokens;

public class SplitTests
{
    private readonly Tokenizer tokenizer;

    private class Foo
    {
        public List<string> Names { get; set; }
    }

    public SplitTests()
    {
        SerilogConfig.Init();

        tokenizer = new Tokenizer(new TokenizerOptions{ EnableLogging = true });
    }

    [Fact]
    public void GivenCommaSeparatedNames_WhenTokenizingWithSplitTransformer_ThenReturnsListWithCorrectValues()
    {
        // Arrange
        const string pattern = @"Names: { Names : Split(',') }";
        const string input = @"Names: Alice,Bob,Charles";

        // Act
        var results = tokenizer.Tokenize<Foo>(pattern, input);
        var foo = results.Value;

        // Assert
        Assert.Equal(3, foo.Names.Count);
        Assert.Equal("Alice", foo.Names[0]);
        Assert.Equal("Bob", foo.Names[1]);
        Assert.Equal("Charles", foo.Names[2]);
    }
}