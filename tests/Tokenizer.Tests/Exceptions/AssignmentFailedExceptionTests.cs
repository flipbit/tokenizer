using Xunit;

namespace Tokens.Exceptions;

public class AssignmentFailedExceptionTests
{
    [Fact]
    public void GivenMessageAndErrors_WhenConstructed_ThenErrorsPropertyIsSet()
    {
        // Arrange
        var inner = new List<Exception>
        {
            new InvalidOperationException("first"),
            new ArgumentException("second"),
        };

        // Act
        var exception = new AssignmentFailedException("Assignment failed", inner);

        // Assert
        Assert.Equal("Assignment failed", exception.Message);
        Assert.Equal(2, exception.Errors.Count);
        Assert.IsType<InvalidOperationException>(exception.Errors[0]);
        Assert.IsType<ArgumentException>(exception.Errors[1]);
    }

    [Fact]
    public void GivenAssignmentFailedException_WhenChecked_ThenIsTokenizerException()
    {
        // Arrange & Act
        var exception = new AssignmentFailedException("test", new List<Exception>());

        // Assert
        Assert.IsAssignableFrom<TokenizerException>(exception);
    }
}
