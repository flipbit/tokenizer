using Xunit;

namespace Tokens.Exceptions;

public class TypeConversionExceptionTests
{
    [Fact]
    public void GivenMessageValueAndType_WhenConstructed_ThenAllPropertiesAreSet()
    {
        // Arrange & Act
        var exception = new TypeConversionException("conversion failed", "hello", typeof(int));

        // Assert
        Assert.Equal("conversion failed", exception.Message);
        Assert.Equal("hello", exception.Value);
        Assert.Equal(typeof(int), exception.TargetType);
    }

    [Fact]
    public void GivenMessageValueTypeAndInnerException_WhenConstructed_ThenInnerExceptionIsPreserved()
    {
        // Arrange
        var inner = new FormatException("bad format");

        // Act
        var exception = new TypeConversionException("conversion failed", "hello", typeof(int), inner);

        // Assert
        Assert.Equal("conversion failed", exception.Message);
        Assert.Equal("hello", exception.Value);
        Assert.Equal(typeof(int), exception.TargetType);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void GivenTypeConversionException_WhenCheckedForInheritance_ThenInheritsFromTokenizerException()
    {
        // Arrange & Act
        var exception = new TypeConversionException("test", 42, typeof(string));

        // Assert
        Assert.IsAssignableFrom<TokenizerException>(exception);
    }
}
