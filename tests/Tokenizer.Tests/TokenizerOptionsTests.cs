using Tokens.Compilation;
using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TokenizerOptionsTests : TokenizerTestBase
{
    public TokenizerOptionsTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void TestTrimBeforePreambleWhenTrue()
    {
        const string content = "Should be trimmed\r\nPreamble: { First } Second: { Second }";

        var parser = new TemplateCompiler(new TokenizerOptions { TrimPreambleBeforeNewLine = true });

        var template = parser.Compile(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.Equal("Second: ", template.Tokens.ElementAt(1).Preamble);
    }

    private class Person
    {
        public int Age { get; set; }
        public string Address { get; set; } = null!;
    }

    [Fact]
    public void TestTerminateOnNewLineFromFrontMatter_AppliesToTokenValues()
    {
        const string content = "---\nTerminateOnNewLine: true\n---\nAge: { Age }\nAddress: { Address }";
        const string input = "Age: 30\nAddress: London";

        var tokenizer = new Tokenizer();

        var template = tokenizer.Compile(content);
        var result = tokenizer.Tokenize<Person>(template, input);

        Assert.Equal(30, result.Value.Age);
        Assert.Equal("London", result.Value.Address);
        Assert.True(result.Template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void TestTrimBeforePreambleWhenFalse()
    {
        const string content = "Should not be trimmed\r\nPreamble: { First } Second: { Second }";

        var parser = new TemplateCompiler(new TokenizerOptions { TrimPreambleBeforeNewLine = false });

        var template = parser.Compile(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Should not be trimmed\nPreamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.Equal("Second: ", template.Tokens.ElementAt(1).Preamble);
    }

    [Fact]
    public void TestTrimBeforePreambleWhenSetFromFrontMatter()
    {
        const string content = "---\nTrimPreambleBeforeNewLine: true\n---\nShould be trimmed\r\nPreamble: { First } Second: { Second }";

        var parser = new TemplateCompiler(new TokenizerOptions { TrimPreambleBeforeNewLine = false });

        var template = parser.Compile(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.Equal("Second: ", template.Tokens.ElementAt(1).Preamble);
        Assert.True(template.Options.TrimPreambleBeforeNewLine);
    }

    [Fact]
    public void TestTerminateOnNewLineWhenSetFromFrontMatter()
    {
        const string content = "---\nTerminateOnNewLine: true\n---\nPreamble: { First }\n Trimmed";

        var parser = new TemplateCompiler(new TokenizerOptions { TrimPreambleBeforeNewLine = false });

        var template = parser.Compile(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.True(template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void TestTerminateOnNewLineWhenNotSetFromFrontMatter()
    {
        const string content = "Preamble: { First }\n Trimmed";

        var parser = new TemplateCompiler(new TokenizerOptions { TrimPreambleBeforeNewLine = false });

        var template = parser.Compile(content);

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.False(template.Options.TerminateOnNewLine);
    }
}
