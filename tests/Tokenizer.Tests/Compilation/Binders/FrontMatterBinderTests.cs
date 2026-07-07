using Tokens.Compilation.Definitions;
using Tokens.Compilation.Nodes;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace Tokens.Compilation.Binders;

public class FrontMatterBinderTests : TokenizerTestBase
{
    public FrontMatterBinderTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenBooleanOptions_WhenBinding_ThenOptionsMapped()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "trimleadingwhitespace", "yes"),
            new FrontMatterEntry(loc, 0, 5, "casesensitive", "on"),
            new FrontMatterEntry(loc, 0, 5, "ignoremissingproperties", "false"),
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.True(template.Options.TrimLeadingWhitespaceInTokenPreamble);
        Assert.Equal(System.StringComparison.InvariantCulture, template.Options.TokenStringComparison);
        Assert.False(template.Options.IgnoreMissingProperties);
    }

    [Fact]
    public void GivenNameHintTag_WhenBinding_ThenPropertiesSet()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "name", "My Template"),
            new FrontMatterEntry(loc, 0, 5, "hint", "A hint"),
            new FrontMatterEntry(loc, 0, 5, "hint?", "Optional hint"),
            new FrontMatterEntry(loc, 0, 5, "tag", "alpha"),
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.Equal("My Template", template.Name);
        Assert.Equal(2, template.Hints.Count);
        Assert.Equal("alpha", template.Tags.Single());
    }

    [Fact]
    public void GivenSetDirective_WhenBinding_ThenTokenAdded()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[] { new SetTokenDirective(loc, 0, 5, "value") };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.Single(template.Tokens);
        Assert.Equal("value", template.Tokens[0].Name);
        Assert.True(template.Tokens[0].IsFrontMatterToken);
    }

    [Fact]
    public void GivenUnknownOption_WhenBinding_ThenThrows()
    {
        // Arrange
        var loc = new FileLocation();
        var fm = new FrontMatterBlock(loc, 0, 10, new SyntaxNode[] { new FrontMatterEntry(loc, 0, 5, "unknown", "x") });
        var template = new TemplateDefinition();

        // Act & Assert
        Assert.Throws<ParsingException>(() => new FrontMatterBinder().Bind(template, fm));
    }

    [Fact]
    public void GivenDuplicateName_WhenBinding_ThenLastOneWins()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "name", "First"),
            new FrontMatterEntry(loc, 0, 5, "Name", "Second"),
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.Equal("Second", template.Name);
    }

    [Fact]
    public void GivenDuplicateBoolean_WhenBinding_ThenLastOneWins()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "trimleadingwhitespace", "off"),
            new FrontMatterEntry(loc, 0, 5, "TrimLeadingWhitespace", "on"),
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.True(template.Options.TrimLeadingWhitespaceInTokenPreamble);
    }

    [Fact]
    public void GivenMultipleHintsTagsSets_WhenBinding_ThenAccumulate()
    {
        // Arrange
        var loc = new FileLocation();
        var entries = new SyntaxNode[]
        {
            new FrontMatterEntry(loc, 0, 5, "hint", "A"),
            new FrontMatterEntry(loc, 0, 5, "hint?", "B"),
            new FrontMatterEntry(loc, 0, 5, "tag", "alpha"),
            new FrontMatterEntry(loc, 0, 5, "tag", "beta"),
            new SetTokenDirective(loc, 0, 5, "value1"),
            new SetTokenDirective(loc, 0, 5, "value2"),
        };
        var fm = new FrontMatterBlock(loc, 0, 10, entries);
        var template = new TemplateDefinition();

        // Act
        new FrontMatterBinder().Bind(template, fm);

        // Assert
        Assert.Equal(2, template.Hints.Count);
        Assert.Equal(2, template.Tags.Count);
        Assert.Equal(2, template.Tokens.Count);
    }

    [Fact]
    public void GivenFrontMatterWithPartialOverrides_WhenParsed_ThenUnspecifiedOptionsRetainDefaults()
    {
        // Arrange — only override TrimPreambleBeforeNewLine, leave everything else at defaults
        const string content = "---\nTrimPreambleBeforeNewLine: true\n---\nHello { Name }";
        var parser = new TemplateCompiler(new TokenizerOptions { TrimTrailingWhiteSpace = false });

        // Act
        var template = parser.Compile(content).Template;

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
        var template = parser.Compile(content).Template;

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
        var template = parser.Compile(content).Template;

        // Assert — front matter said false, so leading whitespace should be preserved
        Assert.False(template.Options.TrimLeadingWhitespaceInTokenPreamble);
        var token = template.Tokens.First(t => string.Equals(t.Name, "Name", StringComparison.Ordinal));
        Assert.StartsWith("   ", token.Preamble, StringComparison.Ordinal);
    }
}
