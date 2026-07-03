using System.IO;
using Xunit;

namespace Tokens.Enumerators;

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
        Assert.True(enumerator.TryMatch("hello"));
        Assert.False(enumerator.TryMatch("world"));
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

    [Fact]
    public void GivenThreeChars_WhenConsumedViaNext_ThenIsEmptyAfterThirdNext()
    {
        // Arrange
        var enumerator = new TokenEnumerator("abc");

        // Act / Assert
        Assert.False(enumerator.IsEmpty);
        enumerator.Next(); // a
        Assert.False(enumerator.IsEmpty);
        enumerator.Next(); // b
        Assert.False(enumerator.IsEmpty);
        enumerator.Next(); // c
        Assert.True(enumerator.IsEmpty); // must be true immediately after last char
    }

    [Fact]
    public void GivenTextReader_WhenNext_ThenReturnsCharsInOrder()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("abc"));

        // Act / Assert
        Assert.Equal('a', enumerator.Next());
        Assert.Equal('b', enumerator.Next());
        Assert.Equal('c', enumerator.Next());
        Assert.Equal('\0', enumerator.Next());
    }

    [Fact]
    public void GivenInputWithCRLF_WhenNext_ThenNormalizesToLF()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("a\r\nb"));

        // Act / Assert
        Assert.Equal('a', enumerator.Next());
        Assert.Equal('\n', enumerator.Next());
        Assert.Equal('b', enumerator.Next());
    }

    [Fact]
    public void GivenInputWithLoneCR_WhenNext_ThenNormalizesToLF()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("a\rb"));

        // Act / Assert
        Assert.Equal('a', enumerator.Next());
        Assert.Equal('\n', enumerator.Next());
        Assert.Equal('b', enumerator.Next());
    }

    [Fact]
    public void GivenInputWithLF_WhenNext_ThenReturnsLF()
    {
        // Arrange
        var enumerator = new TokenEnumerator(new StringReader("a\nb"));

        // Act / Assert
        Assert.Equal('a', enumerator.Next());
        Assert.Equal('\n', enumerator.Next());
        Assert.Equal('b', enumerator.Next());
    }
}
