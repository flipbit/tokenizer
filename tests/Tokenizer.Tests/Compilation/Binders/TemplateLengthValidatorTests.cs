using Tokens.Exceptions;
using Xunit;

namespace Tokens.Compilation.Binders;

public class TemplateLengthValidatorTests
{
    [Fact]
    public void GivenContentExceedingMaxLength_WhenValidating_ThenThrowsParsingException()
    {
        // Arrange
        var options = new TokenizerOptions { MaxTemplateLength = 10 };
        var content = new string('x', 11);

        // Act & Assert
        var ex = Assert.Throws<ParsingException>(() => TemplateLengthValidator.Validate(content, options));
        Assert.Contains("exceeds maximum allowed length", ex.Message);
    }

    [Fact]
    public void GivenContentAtMaxLength_WhenValidating_ThenDoesNotThrow()
    {
        var options = new TokenizerOptions { MaxTemplateLength = 10 };
        var content = new string('x', 10);
        TemplateLengthValidator.Validate(content, options);
    }

    [Fact]
    public void GivenMaxLengthDisabled_WhenValidating_ThenDoesNotThrow()
    {
        var options = new TokenizerOptions { MaxTemplateLength = 0 };
        var content = new string('x', 10000);
        TemplateLengthValidator.Validate(content, options);
    }
}
