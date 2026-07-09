using Xunit;

namespace Tokens.Enumerators;

public class FileLocationTests
{
    [Fact]
    public void GivenTwoLocationsWithSameValues_WhenCompared_ThenAreEqual()
    {
        // Arrange
        var a = new FileLocation();
        var b = new FileLocation();

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void GivenTwoLocationsWithSameValues_WhenComparedWithOperator_ThenAreEqual()
    {
        // Arrange
        var a = new FileLocation();
        var b = new FileLocation();

        // Act & Assert
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void GivenTwoLocationsWithDifferentValues_WhenCompared_ThenAreNotEqual()
    {
        // Arrange
        var a = new FileLocation();
        var b = new FileLocation();
        b.Increment('x'); // Column becomes 2

        // Act & Assert
        Assert.NotEqual(a, b);
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void GivenTwoEqualLocations_WhenHashed_ThenHashCodesMatch()
    {
        // Arrange
        var a = new FileLocation();
        var b = new FileLocation();

        // Act & Assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GivenLocation_WhenComparedToNull_ThenIsNotEqual()
    {
        // Arrange
        var location = new FileLocation();

        // Act & Assert
        // CodeQL cs/null-argument-to-equals: intentionally testing Equals(null) returns false
#pragma warning disable CA1508 // Avoid dead conditional code — testing Equals(null) behavior
        Assert.False(location.Equals(obj: null));
#pragma warning restore CA1508
    }

    [Fact]
    public void GivenLocation_WhenToString_ThenReturnsCompactFormat()
    {
        // Arrange
        var location = new FileLocation();

        // Act
        var result = location.ToString();

        // Assert
        Assert.Equal("Ln: 1 Col: 1 Para: 1", result);
    }
}
