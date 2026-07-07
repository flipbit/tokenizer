using Tokens.Enumerators;
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

    [Fact]
    public void GivenLexerExceptionWithLocation_WhenMessageAccessed_ThenIncludesLineAndColumn()
    {
        // Arrange
        var location = MakeLocation(line: 5, column: 12);

        // Act
        var exception = new LexerException("syntax error", location);

        // Assert
        Assert.Contains("Line: 5", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Column: 12", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenLexerExceptionWithInnerException_WhenConstructed_ThenInnerExceptionPreserved()
    {
        // Arrange
        var location = MakeLocation(line: 3, column: 7);
        var inner = new InvalidOperationException("inner");

        // Act
        var exception = new LexerException("syntax error", location, inner);

        // Assert
        Assert.Same(inner, exception.InnerException);
        Assert.Contains("Line: 3", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Column: 7", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenLexerExceptionWithoutLocation_WhenMessageAccessed_ThenNoLineColumnAppended()
    {
        // Arrange & Act
        var exception = new LexerException("syntax error");

        // Assert
        Assert.DoesNotContain("Line:", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Column:", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GivenParsingExceptionWithLocation_WhenMessageAccessed_ThenIncludesLineAndColumn()
    {
        // Arrange
        var location = MakeLocation(line: 10, column: 3);

        // Act
        var exception = new ParsingException("parse error", location);

        // Assert
        Assert.Contains("Line: 10", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Column: 3", exception.Message, StringComparison.Ordinal);
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
