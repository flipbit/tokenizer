using System.Collections.Generic;
using System.Linq;
using Tokens.Compilation.Definitions;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Compilation.Definitions;

public class TokenDefinitionTests
{
    [Fact]
    public void GivenNewTokenDefinition_WhenCreated_ThenHasDefaultValues()
    {
        // Arrange & Act
        var token = new TokenDefinition();

        // Assert
        Assert.Equal(0, token.Id);
        Assert.Equal(0, token.DependsOnId);
        Assert.Equal(string.Empty, token.Preamble);
        Assert.Equal(string.Empty, token.Name);
        Assert.Equal(string.Empty, token.Value);
        Assert.False(token.Optional);
        Assert.False(token.TerminateOnNewline);
        Assert.False(token.Repeating);
        Assert.False(token.Required);
        Assert.False(token.IsNull);
        Assert.False(token.ConsiderOnce);
        Assert.Null(token.Content);
        Assert.Null(token.Location);
        Assert.Empty(token.Decorators);
        Assert.False(token.HasValue);
        Assert.False(token.IsFrontMatterToken);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingId_ThenIdIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.Id = 42;

        // Assert
        Assert.Equal(42, token.Id);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingDependsOnId_ThenDependsOnIdIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.DependsOnId = 10;

        // Assert
        Assert.Equal(10, token.DependsOnId);
    }

    [Fact]
    public void GivenTokenDefinition_WhenAppendingPreamble_ThenPreambleIsUpdated()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.AppendPreamble("Hello ");
        token.AppendPreamble("World");

        // Assert
        Assert.Equal("Hello World", token.Preamble);
    }

    [Fact]
    public void GivenTokenDefinition_WhenAppendingCarriageReturn_ThenPreambleIsNotUpdated()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.AppendPreamble("Hello");
        token.AppendPreamble("\r");
        token.AppendPreamble("World");

        // Assert
        Assert.Equal("HelloWorld", token.Preamble);
    }

    [Fact]
    public void GivenTokenDefinition_WhenAppendingName_ThenNameIsUpdated()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.AppendName("Token");
        token.AppendName("Name");

        // Assert
        Assert.Equal("TokenName", token.Name);
    }

    [Fact]
    public void GivenTokenDefinition_WhenAppendingValue_ThenValueIsUpdated()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.AppendValue("Value");
        token.AppendValue("123");

        // Assert
        Assert.Equal("Value123", token.Value);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingOptional_ThenOptionalIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.Optional = true;

        // Assert
        Assert.True(token.Optional);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingTerminateOnNewline_ThenTerminateOnNewlineIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.TerminateOnNewline = true;

        // Assert
        Assert.True(token.TerminateOnNewline);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingRepeating_ThenRepeatingIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.Repeating = true;

        // Assert
        Assert.True(token.Repeating);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingRequired_ThenRequiredIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.Required = true;

        // Assert
        Assert.True(token.Required);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingIsNull_ThenIsNullIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.IsNull = true;

        // Assert
        Assert.True(token.IsNull);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingConsiderOnce_ThenConsiderOnceIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.ConsiderOnce = true;

        // Assert
        Assert.True(token.ConsiderOnce);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingContent_ThenContentIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.Content = "Test Content";

        // Assert
        Assert.Equal("Test Content", token.Content);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingLocation_ThenLocationIsSet()
    {
        // Arrange
        var token = new TokenDefinition();
        var location = new FileLocation();

        // Act
        token.Location = location;

        // Assert
        Assert.Equal(location, token.Location);
    }

    [Fact]
    public void GivenTokenDefinition_WhenSettingIsFrontMatterToken_ThenIsFrontMatterTokenIsSet()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.IsFrontMatterToken = true;

        // Assert
        Assert.True(token.IsFrontMatterToken);
    }

    [Fact]
    public void GivenTokenDefinitionWithEmptyValue_WhenCheckingHasValue_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act & Assert
        Assert.False(token.HasValue);
    }

    [Fact]
    public void GivenTokenDefinitionWithValue_WhenCheckingHasValue_ThenReturnsTrue()
    {
        // Arrange
        var token = new TokenDefinition();
        token.AppendValue("test");

        // Act & Assert
        Assert.True(token.HasValue);
    }

    [Fact]
    public void GivenTokenDefinition_WhenAppendingDecorators_ThenDecoratorsAreAdded()
    {
        // Arrange
        var token = new TokenDefinition();
        var decorator1 = new DecoratorDefinition();
        decorator1.AppendName("Decorator1");
        var decorator2 = new DecoratorDefinition();
        decorator2.AppendName("Decorator2");
        var decorators = new List<DecoratorDefinition> { decorator1, decorator2 };

        // Act
        token.AppendDecorators(decorators);

        // Assert
        Assert.Equal(2, token.Decorators.Count);
        Assert.Equal("Decorator1", token.Decorators[0].Name);
        Assert.Equal("Decorator2", token.Decorators[1].Name);
    }

    [Fact]
    public void GivenTokenDefinition_WhenAppendingNullDecorators_ThenNoDecoratorsAreAdded()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.AppendDecorators(null);

        // Assert
        Assert.Empty(token.Decorators);
    }

    [Fact]
    public void GivenTokenDefinition_WhenCallingToString_ThenReturnsContent()
    {
        // Arrange
        var token = new TokenDefinition();
        token.Content = "Test Content";

        // Act
        var result = token.ToString();

        // Assert
        Assert.Equal("Test Content", result);
    }

    [Fact]
    public void GivenTokenDefinitionWithNullContent_WhenCallingToString_ThenReturnsNull()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        var result = token.ToString();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GivenTokenDefinitionWithPreambleContainingNewLine_WhenTrimmingPreambleBeforeNewLine_ThenTrimsCorrectly()
    {
        // Arrange
        var token = new TokenDefinition();
        token.AppendPreamble("Line 1\nLine 2\nLine 3");

        // Act
        token.TrimPreambleBeforeNewLine();

        // Assert
        Assert.Equal("Line 3", token.Preamble);
    }

    [Fact]
    public void GivenTokenDefinitionWithPreambleNotContainingNewLine_WhenTrimmingPreambleBeforeNewLine_ThenPreambleUnchanged()
    {
        // Arrange
        var token = new TokenDefinition();
        token.AppendPreamble("No new lines here");

        // Act
        token.TrimPreambleBeforeNewLine();

        // Assert
        Assert.Equal("No new lines here", token.Preamble);
    }

    [Fact]
    public void GivenTokenDefinitionWithEmptyPreamble_WhenTrimmingPreambleBeforeNewLine_ThenPreambleRemainsEmpty()
    {
        // Arrange
        var token = new TokenDefinition();

        // Act
        token.TrimPreambleBeforeNewLine();

        // Assert
        Assert.Equal(string.Empty, token.Preamble);
    }

    [Fact]
    public void GivenTokenDefinitionWithPreambleEndingWithNewLine_WhenTrimmingPreambleBeforeNewLine_ThenPreambleIsCleared()
    {
        // Arrange
        var token = new TokenDefinition();
        token.AppendPreamble("Line 1\nLine 2\n");

        // Act
        token.TrimPreambleBeforeNewLine();

        // Assert
        Assert.Equal(string.Empty, token.Preamble);
    }
}
