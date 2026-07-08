using Xunit;
using Xunit.Abstractions;

namespace Tokens;

public class TemplateMatchResultTests : TokenizerTestBase
{
    public TemplateMatchResultTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTemplateWithHint_WhenMatchedAgainstTemplateWithout_ThenHintMatchWins()
    {
        // Arrange
        var tokenizer = CreateTokenizer();
        var matcher = new TemplateMatcher();

        var templateWithHint = tokenizer.Compile("Name: {Name: SubstringBefore(',') }").Template;
        templateWithHint.Name = "with-hint";
        templateWithHint.AddHint(new Hint(Text: "Name"));

        var templateWithoutHint = tokenizer.Compile("Name: {Name}, Age: {Age}").Template;
        templateWithoutHint.Name = "without-hint";

        matcher.RegisterTemplate(templateWithHint);
        matcher.RegisterTemplate(templateWithoutHint);

        // Act
        var result = matcher.Tokenize("Name: Alice, Age: 30");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.BestMatch);
        Assert.Equal("with-hint", result.BestMatch!.Template.Name);
    }

    [Fact]
    public void GivenNoSuccessfulMatches_WhenBestMatchAccessed_ThenReturnsNullAndSuccessIsFalse()
    {
        // Arrange
        var matcher = new TemplateMatcher();
        matcher.RegisterTemplate("Prefix: {Value}", "test");

        // Act
        var result = matcher.Tokenize("completely unrelated input");

        // Assert
        Assert.Null(result.BestMatch);
        Assert.False(result.Success);
    }
}
