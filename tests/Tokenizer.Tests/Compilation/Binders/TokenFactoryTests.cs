using Tokens.Compilation.Definitions;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TokenFactoryTests
{
    [Fact]
    public void GivenTokenDefinition_WhenCreating_ThenPropertiesAreMapped()
    {
        var definition = new TokenDefinition
        {
            Content = "{Name}",
            IsOptional = true,
            IsRepeating = true,
            TerminateOnNewLine = true,
            IsRequired = true,
            DependsOnId = 5,
            IsFrontMatterToken = true,
            IsNull = true,
            IsSingleUse = true,
        };
        definition.AppendName("Name");
        definition.AppendPreamble("Preamble: ");

        var token = TokenFactory.Create(definition, new TokenizerOptions(), NullCompilationDiagnosticCollector.Instance);

        Assert.Equal("Name", token.Name);
        Assert.Equal("Preamble: ", token.Preamble);
        Assert.True(token.IsOptional);
        Assert.True(token.IsRepeating);
        Assert.True(token.TerminateOnNewLine);
        Assert.True(token.IsRequired);
        Assert.Equal(5, token.DependsOnId);
        Assert.True(token.IsFrontMatterToken);
        Assert.True(token.IsNull);
        Assert.True(token.IsSingleUse);
    }

    [Fact]
    public void GivenTokenDefinitionWithNullName_WhenCreating_ThenNameDefaultsToEmpty()
    {
        var definition = new TokenDefinition { Content = "literal" };
        var token = TokenFactory.Create(definition, new TokenizerOptions(), NullCompilationDiagnosticCollector.Instance);
        Assert.Equal(string.Empty, token.Name);
    }

    [Fact]
    public void GivenTrimLeadingWhitespaceEnabled_WhenPreambleHasLeadingWhitespace_ThenPreambleIsTrimmed()
    {
        var options = new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("\n  Hello");

        var token = TokenFactory.Create(definition, options, NullCompilationDiagnosticCollector.Instance);

        Assert.Equal("Hello", token.Preamble);
    }

    [Fact]
    public void GivenTrimLeadingWhitespaceEnabled_WhenPreambleIsOnlySpaces_ThenPreambleIsPreserved()
    {
        var options = new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("   ");

        var token = TokenFactory.Create(definition, options, NullCompilationDiagnosticCollector.Instance);

        Assert.Equal("   ", token.Preamble);
    }

    [Fact]
    public void GivenTrimLeadingWhitespaceEnabled_WhenPreambleIsWhitespaceOnly_ThenLeadingSpacesTrimmed()
    {
        var options = new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("  \n");

        var token = TokenFactory.Create(definition, options, NullCompilationDiagnosticCollector.Instance);

        Assert.Equal("\n", token.Preamble);
    }

    [Fact]
    public void GivenTrimPreambleBeforeNewLineEnabled_WhenPreambleContainsNewline_ThenKeepsTextAfterLastNewline()
    {
        var options = new TokenizerOptions { TrimPreambleBeforeNewLine = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("First line\nSecond line");

        var token = TokenFactory.Create(definition, options, NullCompilationDiagnosticCollector.Instance);

        Assert.Equal("Second line", token.Preamble);
    }

    [Fact]
    public void GivenTrimPreambleBeforeNewLineEnabled_WhenPreambleHasNoNewline_ThenPreambleUnchanged()
    {
        var options = new TokenizerOptions { TrimPreambleBeforeNewLine = true };
        var definition = new TokenDefinition { Content = "{Token}" };
        definition.AppendPreamble("No newline here");

        var token = TokenFactory.Create(definition, options, NullCompilationDiagnosticCollector.Instance);

        Assert.Equal("No newline here", token.Preamble);
    }

    [Fact]
    public void GivenTokenDefinitionWithLocation_WhenCreating_ThenLocationIsSet()
    {
        var location = new FileLocation();
        var definition = new TokenDefinition
        {
            Content = "{Token}",
            Location = location,
        };

        var token = TokenFactory.Create(definition, new TokenizerOptions(), NullCompilationDiagnosticCollector.Instance);

        Assert.Equal(location, token.Location);
    }
}
