using System.Linq;
using Tokens.Parsers;
using Xunit;

namespace Tokens;

public class TokenizerOptionsTests
{
    [Fact]
    public void TestTrimBeforePreambleWhenTrue()
    {
        const string content = "Should be trimmed\r\nPreamble: { First } Second: { Second }";

        var parser = new TokenParser();

        parser.Options.TrimPreambleBeforeNewLine = true;

        var template = parser.Parse(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.Equal("Second: ", template.Tokens.ElementAt(1).Preamble);
    } 

    [Fact]
    public void TestTrimBeforePreambleWhenFalse()
    {
        const string content = "Should not be trimmed\r\nPreamble: { First } Second: { Second }";

        var parser = new TokenParser();

        parser.Options.TrimPreambleBeforeNewLine = false;

        var template = parser.Parse(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Should not be trimmed\nPreamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.Equal("Second: ", template.Tokens.ElementAt(1).Preamble);
    } 

    [Fact]
    public void TestTrimBeforePreambleWhenSetFromFrontMatter()
    {
        const string content = "---\nTrimPreambleBeforeNewLine: true\n---\nShould be trimmed\r\nPreamble: { First } Second: { Second }";

        var parser = new TokenParser();

        parser.Options.TrimPreambleBeforeNewLine = false;

        var template = parser.Parse(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.Equal("Second: ", template.Tokens.ElementAt(1).Preamble);
        Assert.True(template.Options.TrimPreambleBeforeNewLine);
    } 

    [Fact]
    public void TestTerminateOnNewLineWhenSetFromFrontMatter()
    {
        const string content = "---\nTerminateOnNewLine: true\n---\nPreamble: { First }\n Trimmed";

        var parser = new TokenParser();

        parser.Options.TrimPreambleBeforeNewLine = false;

        var template = parser.Parse(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.True(template.Options.TerminateOnNewline);
    } 

    [Fact]
    public void TestTerminateOnNewLineWhenNotSetFromFrontMatter()
    {
        const string content = "Preamble: { First }\n Trimmed";

        var parser = new TokenParser();

        parser.Options.TrimPreambleBeforeNewLine = false;

        var template = parser.Parse(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.False(template.Options.TerminateOnNewline);
    } 
}