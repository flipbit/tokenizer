using Xunit;

namespace Tokens.Extensions;

public class ValueConcatenationTests
{
    [Fact]
    public void GivenTwoStrings_WhenCanConcatenate_ThenReturnsTrue()
    {
        // Arrange / Act
        var result = ValueConcatenation.CanConcatenate("hello", "world");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GivenNullExistingValue_WhenCanConcatenate_ThenReturnsFalse()
    {
        // Arrange / Act
        var result = ValueConcatenation.CanConcatenate(existingValue: null, newValue: "world");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenNonStringValues_WhenCanConcatenate_ThenReturnsFalse()
    {
        // Arrange / Act
        var result = ValueConcatenation.CanConcatenate(existingValue: 42, newValue: "world");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GivenTwoStrings_WhenConcatenate_ThenReturnsCombinedString()
    {
        // Arrange / Act
        var result = ValueConcatenation.Concatenate("hello", "world", concatenationString: null);

        // Assert
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void GivenTwoStringsWithSeparator_WhenConcatenate_ThenReturnsSeparatedString()
    {
        // Arrange / Act
        var result = ValueConcatenation.Concatenate("hello", "world", ", ");

        // Assert
        Assert.Equal("hello, world", result);
    }

    [Fact]
    public void GivenTwoStringsWithCrSeparator_WhenConcatenate_ThenReplacesWithNewLine()
    {
        // Arrange / Act
        var result = ValueConcatenation.Concatenate("hello", "world", "<CR>");

        // Assert
        Assert.Equal($"hello{Environment.NewLine}world", result);
    }

    [Fact]
    public void GivenNonStringExistingValue_WhenConcatenate_ThenReturnsExistingValue()
    {
        // Arrange / Act
        var result = ValueConcatenation.Concatenate(existingValue: 42, newValue: "world", concatenationString: null);

        // Assert
        Assert.Equal(42, result);
    }
}
