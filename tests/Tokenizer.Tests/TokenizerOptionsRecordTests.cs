using Xunit;
using Xunit.Abstractions;

namespace Tokens.Tests;

public class TokenizerOptionsRecordTests : TokenizerTestBase
{
    public TokenizerOptionsRecordTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void GivenTwoDefaultOptions_WhenCompared_ThenAreEqual()
    {
        // Arrange
        var options1 = new TokenizerOptions();
        var options2 = new TokenizerOptions();

        // Act & Assert
        Assert.Equal(options1, options2);
    }

    [Fact]
    public void GivenOptions_WhenCopiedWithModification_ThenOriginalIsUnchanged()
    {
        // Arrange
        var original = new TokenizerOptions();

        // Act
        var modified = original with { TrimTrailingWhiteSpace = false };

        // Assert
        Assert.True(original.TrimTrailingWhiteSpace);
        Assert.False(modified.TrimTrailingWhiteSpace);
        Assert.NotEqual(original, modified);
    }

    [Fact]
    public void GivenDefaultOptions_ThenDefaultValuesAreCorrect()
    {
        // Arrange & Act
        var options = new TokenizerOptions();

        // Assert
        Assert.False(options.IgnoreMissingProperties);
        Assert.False(options.EnableDiagnostics);
        Assert.True(options.TrimLeadingWhitespaceInTokenPreamble);
        Assert.False(options.TrimPreambleBeforeNewLine);
        Assert.True(options.TrimTrailingWhiteSpace);
        Assert.False(options.OutOfOrderTokens);
        Assert.Equal(System.StringComparison.InvariantCulture, options.TokenStringComparison);
        Assert.False(options.TerminateOnNewLine);
        Assert.Equal(1_048_576, options.MaxInputLength);
        Assert.Equal(65_536, options.MaxTemplateLength);
        Assert.Equal(500, options.MaxTokenCount);
        Assert.Equal(0, options.MaxIterations);
    }
}
