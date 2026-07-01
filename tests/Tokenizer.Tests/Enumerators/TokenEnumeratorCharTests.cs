using Tokens.Enumerators;
using Xunit;

namespace Tokens;

public class TokenEnumeratorCharTests
{
    [Fact]
    public void GivenNonEmptyInput_WhenPeek_ThenReturnsFirstChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("hello");

        // Act
        var result = enumerator.Peek();

        // Assert
        Assert.Equal('h', result);
    }

    [Fact]
    public void GivenNonEmptyInput_WhenNext_ThenReturnsFirstCharAndAdvances()
    {
        // Arrange
        var enumerator = new TokenEnumerator("hello");

        // Act
        var first = enumerator.Next();
        var second = enumerator.Peek();

        // Assert
        Assert.Equal('h', first);
        Assert.Equal('e', second);
    }

    [Fact]
    public void GivenEmptyInput_WhenPeek_ThenReturnsNullChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("");

        // Act
        var result = enumerator.Peek();

        // Assert
        Assert.Equal('\0', result);
    }

    [Fact]
    public void GivenEmptyInput_WhenNext_ThenReturnsNullChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("");

        // Act
        var result = enumerator.Next();

        // Assert
        Assert.Equal('\0', result);
    }

    [Fact]
    public void GivenExhaustedInput_WhenPeek_ThenReturnsNullChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("a");
        enumerator.Next();

        // Act
        var result = enumerator.Peek();

        // Assert
        Assert.Equal('\0', result);
    }

    [Fact]
    public void GivenExhaustedInput_WhenNext_ThenReturnsNullChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("a");
        enumerator.Next();

        // Act
        var result = enumerator.Next();

        // Assert
        Assert.Equal('\0', result);
    }

    [Fact]
    public void GivenNonEmptyInput_WhenPeekWithOffset_ThenReturnsCorrectChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("hello");

        // Act / Assert
        Assert.Equal('h', enumerator.Peek(0));
        Assert.Equal('e', enumerator.Peek(1));
        Assert.Equal('l', enumerator.Peek(2));
        Assert.Equal('l', enumerator.Peek(3));
        Assert.Equal('o', enumerator.Peek(4));
    }

    [Fact]
    public void GivenNonEmptyInput_WhenPeekBeyondEnd_ThenReturnsNullChar()
    {
        // Arrange
        var enumerator = new TokenEnumerator("hi");

        // Act
        var result = enumerator.Peek(5);

        // Assert
        Assert.Equal('\0', result);
    }

    [Fact]
    public void GivenInputWithNewlines_WhenAdvancingPastNewline_ThenLocationTracksCorrectly()
    {
        // Arrange
        var enumerator = new TokenEnumerator("ab\ncd");

        // Act
        enumerator.Next(); // a - Line 1 Col 2
        enumerator.Next(); // b - Line 1 Col 3
        enumerator.Next(); // \n - triggers newline on next call
        enumerator.Next(); // c - NewLine() fires, resets to Line 2 Col 1

        // Assert - after newline, the deferred NewLine() resets column
        Assert.Equal(2, enumerator.Location.Line);
        Assert.Equal(1, enumerator.Location.Column);

        // Next char increments column normally
        enumerator.Next(); // d - Line 2 Col 2
        Assert.Equal(2, enumerator.Location.Line);
        Assert.Equal(2, enumerator.Location.Column);
    }

    [Fact]
    public void GivenNonEmptyInput_WhenMatchWithString_ThenStillWorks()
    {
        // Arrange
        var enumerator = new TokenEnumerator("hello world");

        // Act / Assert
        Assert.True(enumerator.Match("hello"));
        Assert.False(enumerator.Match("world"));
    }

    [Fact]
    public void GivenMultipleChars_WhenNextCalledRepeatedly_ThenReturnsAllCharsInOrder()
    {
        // Arrange
        var enumerator = new TokenEnumerator("abc");

        // Act / Assert
        Assert.Equal('a', enumerator.Next());
        Assert.Equal('b', enumerator.Next());
        Assert.Equal('c', enumerator.Next());
        Assert.Equal('\0', enumerator.Next());
    }
}
