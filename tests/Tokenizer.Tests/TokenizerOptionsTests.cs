using System.Globalization;
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

        var template = parser.Compile(content).Template;

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.Equal("Second: ", template.Tokens.ElementAt(1).Preamble);
    }

    private sealed class Person
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

        var template = tokenizer.Compile(content).Template;
        var result = tokenizer.Tokenize(template, input);
        var person = result.Assign<Person>();

        Assert.Equal(30, person.Age);
        Assert.Equal("London", person.Address);
        Assert.True(result.Template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void TestTrimBeforePreambleWhenFalse()
    {
        const string content = "Should not be trimmed\r\nPreamble: { First } Second: { Second }";

        var parser = new TemplateCompiler(new TokenizerOptions { TrimPreambleBeforeNewLine = false });

        var template = parser.Compile(content).Template;

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Should not be trimmed\nPreamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.Equal("Second: ", template.Tokens.ElementAt(1).Preamble);
    }

    [Fact]
    public void TestTrimBeforePreambleWhenSetFromFrontMatter()
    {
        const string content = "---\nTrimPreambleBeforeNewLine: true\n---\nShould be trimmed\r\nPreamble: { First } Second: { Second }";

        var parser = new TemplateCompiler(new TokenizerOptions { TrimPreambleBeforeNewLine = false });

        var template = parser.Compile(content).Template;

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

        var template = parser.Compile(content).Template;

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.True(template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void TestTerminateOnNewLineWhenNotSetFromFrontMatter()
    {
        const string content = "Preamble: { First }\n Trimmed";

        var parser = new TemplateCompiler(new TokenizerOptions { TrimPreambleBeforeNewLine = false });

        var template = parser.Compile(content).Template;

        Assert.Equal(2, template.Tokens.Count);
        Assert.Equal("Preamble: ", template.Tokens.ElementAt(0).Preamble);
        Assert.False(template.Options.TerminateOnNewLine);
    }

    [Fact]
    public void GivenNewOptions_WhenAccessingCulture_ThenDefaultsToNull()
    {
        // Arrange / Act
        var options = new TokenizerOptions();

        // Assert
        Assert.Null(options.Culture);
    }

    [Fact]
    public void GivenOptions_WhenSettingCulture_ThenCultureIsPreserved()
    {
        // Arrange / Act
        var options = new TokenizerOptions { Culture = CultureInfo.GetCultureInfo("pt-BR") };

        // Assert
        Assert.Equal("pt-BR", options.Culture!.Name);
    }

    [Fact]
    public void GivenNewOptions_WhenAccessingDefaultOffset_ThenDefaultsToNull()
    {
        // Arrange / Act
        var options = new TokenizerOptions();

        // Assert
        Assert.Null(options.DefaultOffset);
    }

    [Fact]
    public void GivenOptions_WhenSettingDefaultOffset_ThenOffsetIsPreserved()
    {
        // Arrange / Act
        var options = new TokenizerOptions { DefaultOffset = TimeSpan.FromHours(2) };

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), options.DefaultOffset);
    }

    [Fact]
    public void GivenNewOptions_WhenAccessingDefaultTimezone_ThenDefaultsToNull()
    {
        // Arrange / Act
        var options = new TokenizerOptions();

        // Assert
        Assert.Null(options.DefaultTimezone);
    }

    [Fact]
    public void GivenOptions_WhenSettingDefaultTimezone_ThenTimezoneIsPreserved()
    {
        // Arrange / Act
        var options = new TokenizerOptions { DefaultTimezone = "Europe/Berlin" };

        // Assert
        Assert.Equal("Europe/Berlin", options.DefaultTimezone);
    }

    [Fact]
    public void GivenNewOptions_WhenAccessingTimezoneAbbreviations_ThenReturnsEmptyDictionary()
    {
        // Arrange / Act
        var options = new TokenizerOptions();

        // Assert
        Assert.Empty(options.TimezoneAbbreviations);
    }

    [Fact]
    public void GivenOptions_WhenAddingTimezoneAbbreviation_ThenAbbreviationIsStored()
    {
        // Arrange / Act
        var options = new TokenizerOptions()
            .WithTimezoneAbbreviation("PST", TimeSpan.FromHours(-8));

        // Assert
        Assert.Single(options.TimezoneAbbreviations);
        Assert.Equal(TimeSpan.FromHours(-8), options.TimezoneAbbreviations["PST"]);
    }

    [Fact]
    public void GivenOptions_WhenCopiedWithWith_ThenNewPropertiesAreDeepCopied()
    {
        // Arrange
        var original = new TokenizerOptions
        {
            Culture = CultureInfo.GetCultureInfo("fr-FR"),
            DefaultOffset = TimeSpan.FromHours(1),
            DefaultTimezone = "Europe/Paris",
        };
        original = original.WithTimezoneAbbreviation("CET", TimeSpan.FromHours(1));

        // Act
        var copy = original with { DefaultOffset = TimeSpan.FromHours(2) };

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), copy.DefaultOffset);
        Assert.Equal("fr-FR", copy.Culture!.Name);
        Assert.Single(copy.TimezoneAbbreviations);
        // Verify independence
        copy = copy.WithTimezoneAbbreviation("CEST", TimeSpan.FromHours(2));
        Assert.Single(original.TimezoneAbbreviations);
        Assert.Equal(2, copy.TimezoneAbbreviations.Count);
    }

    [Fact]
    public void GivenDefaultOptions_WhenCheckingMaxRegexTimeout_ThenDefaultsToOneSecond()
    {
        // Arrange
        var options = new TokenizerOptions();

        // Act
        var timeout = options.MaxRegexTimeout;

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), timeout);
    }

    [Fact]
    public void GivenCustomTimeout_WhenCreatingOptions_ThenTimeoutIsPreserved()
    {
        // Arrange & Act
        var options = new TokenizerOptions { MaxRegexTimeout = TimeSpan.FromMilliseconds(250) };

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(250), options.MaxRegexTimeout);
    }

    [Fact]
    public void GivenOptionsWithCustomTimeout_WhenCopying_ThenCopyPreservesTimeout()
    {
        // Arrange
        var original = new TokenizerOptions { MaxRegexTimeout = TimeSpan.FromMilliseconds(500) };

        // Act
        var copy = original with { };

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(500), copy.MaxRegexTimeout);
    }
}
