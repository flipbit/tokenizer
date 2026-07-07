using Tokens.Enumerators;
using Xunit;

namespace Tokens.Exceptions;

public class TokenAssignmentExceptionTests
{
    [Fact]
    public void GivenTokenAndMessage_WhenConstructed_ThenTokenPropertyIsSet()
    {
        // Arrange
        var token = new Token("content", "MyToken", "preamble", new FileLocation());

        // Act
        var exception = new TokenAssignmentException(token, "assignment failed");

        // Assert
        Assert.Same(token, exception.Token);
    }

    [Fact]
    public void GivenTokenAndMessage_WhenConstructed_ThenMessageContainsText()
    {
        // Arrange
        var token = new Token("content", "MyToken", "preamble", new FileLocation());

        // Act
        var exception = new TokenAssignmentException(token, "assignment failed");

        // Assert
        Assert.Contains("assignment failed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenTokenAndInnerException_WhenConstructed_ThenTokenPropertyIsSet()
    {
        // Arrange
        var token = new Token("content", "MyToken", "preamble", new FileLocation());
        var inner = new InvalidOperationException("inner error");

        // Act
        var exception = new TokenAssignmentException(token, inner);

        // Assert
        Assert.Same(token, exception.Token);
    }

    [Fact]
    public void GivenTokenAndInnerException_WhenConstructed_ThenMessageContainsTokenName()
    {
        // Arrange
        var token = new Token("content", "MyToken", "preamble", new FileLocation());
        var inner = new InvalidOperationException("inner error");

        // Act
        var exception = new TokenAssignmentException(token, inner);

        // Assert
        Assert.Contains("MyToken", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenTokenAndInnerException_WhenConstructed_ThenInnerExceptionIsPreserved()
    {
        // Arrange
        var token = new Token("content", "MyToken", "preamble", new FileLocation());
        var inner = new InvalidOperationException("inner error");

        // Act
        var exception = new TokenAssignmentException(token, inner);

        // Assert
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void GivenTokenAssignmentException_WhenCheckedForInheritance_ThenInheritsFromTokenizerException()
    {
        // Arrange
        var token = new Token("content", "MyToken", "preamble", new FileLocation());

        // Act
        var exception = new TokenAssignmentException(token, "test");

        // Assert
        Assert.IsAssignableFrom<TokenizerException>(exception);
    }
}
