using Xunit;
using Xunit.Abstractions;

namespace Tokens.Compilation.Binders;

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
        var parser = new TemplateCompiler(new TokenizerOptions { TrimTrailingWhiteSpace = false });

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
        var parser = new TemplateCompiler(originalOptions);

        // Act
        var template = parser.Parse(content);

        // Assert — template has overridden values
        Assert.True(template.Options.OutOfOrderTokens);
        Assert.True(template.Options.TerminateOnNewLine);
        // Original options should be unchanged
        Assert.False(originalOptions.OutOfOrderTokens);
        Assert.False(originalOptions.TerminateOnNewLine);
    }

    [Fact]
    public void GivenFrontMatterDisablesTrimLeadingWhitespace_WhenParsed_ThenPreambleRetainsLeadingWhitespace()
    {
        // Arrange — parser defaults have TrimLeadingWhitespace=true,
        // but front matter overrides it to false
        const string content = "---\nTrimLeadingWhitespace: false\n---\n   Preamble: { Name }";
        var parser = new TemplateCompiler(new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = true });

        // Act
        var template = parser.Parse(content);

        // Assert — front matter said false, so leading whitespace should be preserved
        Assert.False(template.Options.TrimLeadingWhitespaceInTokenPreamble);
        var token = template.Tokens.First(t => t.Name == "Name");
        Assert.StartsWith("   ", token.Preamble);
    }
}
