using Tokens.Compilation;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests.Compilation.Binders;

public class FrontMatterBinderTests : TokenizerTestBase
{
    public FrontMatterBinderTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenFrontMatterWithPartialOverrides_WhenParsed_ThenUnspecifiedOptionsRetainDefaults()
    {
        // Arrange — only override TrimPreambleBeforeNewLine, leave everything else at defaults
        const string content = "---\nTrimPreambleBeforeNewLine: true\n---\nHello { Name }";
        var parser = new TokenParser(new TokenizerOptions { TrimTrailingWhiteSpace = false });

        // Act
        var template = parser.Parse(content);

        // Assert — TrimPreambleBeforeNewLine is overridden by front matter
        Assert.True(template.Options.TrimPreambleBeforeNewLine);
        // TrimTrailingWhiteSpace should retain the value from the parser's options, not reset to default
        Assert.False(template.Options.TrimTrailingWhiteSpace);
    }

    [Fact]
    public void GivenFrontMatterOptions_WhenParsed_ThenOriginalOptionsAreUnchanged()
    {
        // Arrange
        var originalOptions = new TokenizerOptions();
        const string content = "---\nOutOfOrder: true\nTerminateOnNewLine: true\n---\nHello { Name }";
        var parser = new TokenParser(originalOptions);

        // Act
        var template = parser.Parse(content);

        // Assert — template has overridden values
        Assert.True(template.Options.OutOfOrderTokens);
        Assert.True(template.Options.TerminateOnNewLine);
        // Original options should be unchanged
        Assert.False(originalOptions.OutOfOrderTokens);
        Assert.False(originalOptions.TerminateOnNewLine);
    }
}
