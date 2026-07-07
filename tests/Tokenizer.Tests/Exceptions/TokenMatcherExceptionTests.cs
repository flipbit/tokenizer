using Xunit;

namespace Tokens.Exceptions;

public class TokenMatcherExceptionTests
{
    [Fact]
    public void GivenMessageAndTemplate_WhenConstructed_ThenTemplatePropertyIsSet()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile("Hello {Name}").Template;

        // Act
        var exception = new TokenMatcherException("match failed", template);

        // Assert
        Assert.Same(template, exception.Template);
        Assert.Equal("match failed", exception.Message);
    }

    [Fact]
    public void GivenMessageTemplateAndInnerException_WhenConstructed_ThenInnerExceptionIsPreserved()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile("Hello {Name}").Template;
        var inner = new InvalidOperationException("inner");

        // Act
        var exception = new TokenMatcherException("match failed", template, inner);

        // Assert
        Assert.Same(template, exception.Template);
        Assert.Equal("match failed", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void GivenTokenMatcherException_WhenCheckedForInheritance_ThenInheritsFromTokenizerException()
    {
        // Arrange
        var tokenizer = new Tokenizer();
        var template = tokenizer.Compile("Hello {Name}").Template;

        // Act
        var exception = new TokenMatcherException("test", template);

        // Assert
        Assert.IsAssignableFrom<TokenizerException>(exception);
    }
}
