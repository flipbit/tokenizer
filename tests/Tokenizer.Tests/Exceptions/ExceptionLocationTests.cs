using Tokens.Enumerators;
using Tokens.Exceptions;
using Xunit;

namespace Tokens.Exceptions;

public class ExceptionLocationTests
{
    [Fact]
    public void GivenLexerException_WhenCreatedWithLocation_ThenLocationPropertiesAreSet()
    {
        // Arrange
        var location = MakeLocation(line: 5, column: 10);

        // Act
        var exception = new LexerException("test error", location);

        // Assert
        Assert.Equal(5, exception.Line);
        Assert.Equal(10, exception.Column);
    }

    [Fact]
    public void GivenParsingException_WhenCreatedWithLocation_ThenLocationPropertiesAreSet()
    {
        // Arrange
        var location = MakeLocation(line: 3, column: 7);

        // Act
        var exception = new ParsingException("test error", location);

        // Assert
        Assert.Equal(3, exception.Line);
        Assert.Equal(7, exception.Column);
    }

    private static FileLocation MakeLocation(int line, int column)
    {
        var location = new FileLocation();

        for (var l = 1; l < line; l++)
        {
            location.NewLine();
        }

        for (var c = 1; c < column; c++)
        {
            location.Increment('x');
        }

        return location;
    }
}
