using Xunit;

namespace Tokens.Compilation.Parsing;

public class TemplateDefinitionEnumeratorTests
{
    [Fact]
    public void GivenEmptyString_WhenCreatingEnumerator_ThenIsEmptyIsTrue()
    {
        // Arrange & Act
        var enumerator = new TemplateDefinitionEnumerator(string.Empty);

        // Assert
        Assert.True(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenNullString_WhenCreatingEnumerator_ThenIsEmptyIsTrue()
    {
        // Arrange & Act
        var enumerator = new TemplateDefinitionEnumerator(null!);

        // Assert
        Assert.True(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenNonEmptyString_WhenCreatingEnumerator_ThenIsEmptyIsFalse()
    {
        // Arrange & Act
        var enumerator = new TemplateDefinitionEnumerator("Hello");

        // Assert
        Assert.False(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenString_WhenCallingNext_ThenReturnsNextCharacter()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hello");

        // Act & Assert
        Assert.Equal("H", enumerator.Next());
        Assert.Equal("e", enumerator.Next());
        Assert.Equal("l", enumerator.Next());
        Assert.Equal("l", enumerator.Next());
        Assert.Equal("o", enumerator.Next());
    }

    [Fact]
    public void GivenString_WhenCallingNextOnEmptyEnumerator_ThenReturnsEmptyString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");
        enumerator.Next(); // H
        enumerator.Next(); // i

        // Act
        var result = enumerator.Next();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenString_WhenCallingNextWithLength_ThenReturnsCorrectSubstring()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hello World");

        // Act & Assert
        Assert.Equal("Hel", enumerator.Next(3));
        Assert.Equal("lo ", enumerator.Next(3));
        Assert.Equal("Wor", enumerator.Next(3));
        Assert.Equal("ld", enumerator.Next(2));
    }

    [Fact]
    public void GivenString_WhenCallingNextWithLengthExceedingRemaining_ThenReturnsRemainingCharacters()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");
        enumerator.Next(); // H

        // Act
        var result = enumerator.Next(5);

        // Assert
        Assert.Equal("i", result);
    }

    [Fact]
    public void GivenString_WhenCallingNextWithLengthOnEmptyEnumerator_ThenReturnsEmptyString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");
        enumerator.Next(); // H
        enumerator.Next(); // i

        // Act
        var result = enumerator.Next(3);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenString_WhenCallingPeek_ThenReturnsNextCharacterWithoutAdvancing()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hello");

        // Act & Assert
        Assert.Equal("H", enumerator.Peek());
        Assert.Equal("H", enumerator.Peek()); // Should still be H
        Assert.Equal("H", enumerator.Next()); // Now advance
        Assert.Equal("e", enumerator.Peek());
    }

    [Fact]
    public void GivenString_WhenCallingPeekOnEmptyEnumerator_ThenReturnsEmptyString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");
        enumerator.Next(); // H
        enumerator.Next(); // i

        // Act
        var result = enumerator.Peek();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenString_WhenCallingPeekWithLength_ThenReturnsCorrectSubstringWithoutAdvancing()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hello World");

        // Act & Assert
        Assert.Equal("Hel", enumerator.Peek(3));
        Assert.Equal("Hel", enumerator.Peek(3)); // Should still be the same
        Assert.Equal("H", enumerator.Next()); // Now advance by 1
        Assert.Equal("ell", enumerator.Peek(3));
    }

    [Fact]
    public void GivenString_WhenCallingPeekWithLengthExceedingRemaining_ThenReturnsRemainingCharacters()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");
        enumerator.Next(); // H

        // Act
        var result = enumerator.Peek(5);

        // Assert
        Assert.Equal("i", result);
    }

    [Fact]
    public void GivenString_WhenCallingPeekWithLengthOnEmptyEnumerator_ThenReturnsEmptyString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");
        enumerator.Next(); // H
        enumerator.Next(); // i

        // Act
        var result = enumerator.Peek(3);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenString_WhenCallingPeekWithZeroLength_ThenReturnsEmptyString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hello");

        // Act
        var result = enumerator.Peek(0);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenString_WhenCallingNextWithZeroLength_ThenReturnsEmptyString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hello");

        // Act
        var result = enumerator.Next(0);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenString_WhenCallingNextWithNegativeLength_ThenReturnsEmptyString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hello");

        // Act
        var result = enumerator.Next(-1);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenString_WhenCallingPeekWithNegativeLength_ThenReturnsEmptyString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hello");

        // Act
        var result = enumerator.Peek(-1);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GivenStringWithNewlines_WhenEnumerating_ThenLocationIsUpdatedCorrectly()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Line1\nLine2\r\nLine3");

        // Act & Assert
        Assert.Equal(1, enumerator.Location.Line);
        Assert.Equal(1, enumerator.Location.Column);

        // Read "Line1"
        for (int i = 0; i < 5; i++)
        {
            enumerator.Next();
        }
        Assert.Equal(1, enumerator.Location.Line);
        Assert.Equal(6, enumerator.Location.Column);

        // Read newline
        enumerator.Next();
        Assert.Equal(1, enumerator.Location.Line);
        Assert.Equal(6, enumerator.Location.Column);

        // Read "Line2"
        enumerator.Next(); // This should trigger the line increment
        Assert.Equal(2, enumerator.Location.Line);
        Assert.Equal(1, enumerator.Location.Column);

        for (int i = 0; i < 4; i++)
        {
            enumerator.Next();
        }
        Assert.Equal(2, enumerator.Location.Line);
        Assert.Equal(5, enumerator.Location.Column);

        // Read \r\n
        enumerator.Next(); // \r
        enumerator.Next(); // \n
        Assert.Equal(2, enumerator.Location.Line);
        Assert.Equal(5, enumerator.Location.Column);

        // Read first character of Line3 to trigger line increment
        enumerator.Next();
        Assert.Equal(3, enumerator.Location.Line);
        Assert.Equal(1, enumerator.Location.Column);
    }

    [Fact]
    public void GivenString_WhenEnumeratingAllCharacters_ThenIsEmptyBecomesTrue()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");

        // Act & Assert
        Assert.False(enumerator.IsEmpty);
        enumerator.Next(); // H
        Assert.False(enumerator.IsEmpty);
        enumerator.Next(); // i
        Assert.True(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenString_WhenCallingNextWithLengthGreaterThanString_ThenReturnsEntireString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");

        // Act
        var result = enumerator.Next(10);

        // Assert
        Assert.Equal("Hi", result);
        Assert.True(enumerator.IsEmpty);
    }

    [Fact]
    public void GivenString_WhenCallingPeekWithLengthGreaterThanString_ThenReturnsEntireString()
    {
        // Arrange
        var enumerator = new TemplateDefinitionEnumerator("Hi");

        // Act
        var result = enumerator.Peek(10);

        // Assert
        Assert.Equal("Hi", result);
        Assert.False(enumerator.IsEmpty); // Should not advance
    }
}
