using Xunit;
using Xunit.Abstractions;

namespace Tokens.Compilation;

public class TemplateOptionsCascadeTests : TokenizerTestBase
{
    public TemplateOptionsCascadeTests(ITestOutputHelper output) : base(output)
    {
    }

    // --- TerminateOnNewLine ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenTerminateOnNewLineIsFalse()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.False(template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void GivenInstanceOptionTerminateOnNewLine_WhenCompiled_ThenTemplateInheritsTrue()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { TerminateOnNewLine = true });

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.True(template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void GivenFrontMatterTerminateOnNewLine_WhenInstanceIsFalse_ThenFrontMatterOverrides()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var content = "---\nTerminateOnNewLine: true\n---\nName: {Name}";

        // Act
        var template = tokenizer.Compile(content).Template;

        // Assert
        Assert.True(template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void GivenInstanceTerminateOnNewLineTrue_WhenTokenized_ThenValueTruncatedAtNewline()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { TerminateOnNewLine = true });
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Act
        var result = tokenizer.Tokenize(template, "Name: Alice\nExtra data");

        // Assert
        Assert.Equal("Alice", result.Matches.First(m => string.Equals(m.Token.Name, "Name", StringComparison.Ordinal)).Value);
    }

    // --- TrimPreambleBeforeNewLine ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenTrimPreambleBeforeNewLineIsFalse()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.False(template.Options.TrimPreambleBeforeNewLine);
    }

    [Fact]
    public void GivenInstanceOptionTrimPreambleBeforeNewLine_WhenCompiled_ThenTemplateInheritsTrue()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { TrimPreambleBeforeNewLine = true });

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.True(template.Options.TrimPreambleBeforeNewLine);
    }

    [Fact]
    public void GivenFrontMatterTrimPreambleBeforeNewLine_WhenInstanceIsFalse_ThenFrontMatterOverrides()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var content = "---\nTrimPreambleBeforeNewLine: true\n---\nIgnored\nName: {Name}";

        // Act
        var template = tokenizer.Compile(content).Template;

        // Assert
        Assert.True(template.Options.TrimPreambleBeforeNewLine);
    }

    // --- OutOfOrderTokens ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenOutOfOrderTokensIsFalse()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.False(template.Options.OutOfOrderTokens);
    }

    [Fact]
    public void GivenInstanceOptionOutOfOrderTokens_WhenCompiled_ThenTemplateInheritsTrue()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { OutOfOrderTokens = true });

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.True(template.Options.OutOfOrderTokens);
    }

    // --- TrimLeadingWhitespaceInTokenPreamble ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenTrimLeadingWhitespaceInTokenPreambleIsTrue()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.True(template.Options.TrimLeadingWhitespaceInTokenPreamble);
    }

    [Fact]
    public void GivenInstanceOptionTrimLeadingWhitespaceFalse_WhenCompiled_ThenTemplateInheritsFalse()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { TrimLeadingWhitespaceInTokenPreamble = false });

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.False(template.Options.TrimLeadingWhitespaceInTokenPreamble);
    }

    // --- EnableDiagnostics ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenEnableDiagnosticsIsFalse()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.False(template.Options.EnableDiagnostics);
    }

    [Fact]
    public void GivenInstanceOptionEnableDiagnosticsTrue_WhenTokenized_ThenDiagnosticsPopulated()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { EnableDiagnostics = true });
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Act
        var result = tokenizer.Tokenize(template, "Name: Alice");

        // Assert
        Assert.NotNull(result.Diagnostics);
        Assert.True(result.Diagnostics!.Events.Count > 0);
    }

    // --- IgnoreMissingProperties ---

    [Fact]
    public void GivenDefaultOptions_WhenCompiled_ThenIgnoreMissingPropertiesIsFalse()
    {
        // Arrange
        var tokenizer = CreateTokenizer();

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.False(template.Options.IgnoreMissingProperties);
    }

    [Fact]
    public void GivenInstanceOptionIgnoreMissingPropertiesTrue_WhenCompiled_ThenTemplateInheritsTrue()
    {
        // Arrange
        var tokenizer = CreateTokenizer(new TokenizerOptions { IgnoreMissingProperties = true });

        // Act
        var template = tokenizer.Compile("Name: {Name}").Template;

        // Assert
        Assert.True(template.Options.IgnoreMissingProperties);
    }
}
