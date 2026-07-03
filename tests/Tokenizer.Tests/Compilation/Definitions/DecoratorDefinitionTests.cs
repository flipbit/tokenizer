using Xunit;

namespace Tokens.Compilation.Definitions;

public class DecoratorDefinitionTests
{
    [Fact]
    public void GivenNewDecoratorDefinition_WhenCreated_ThenHasDefaultValues()
    {
        // Arrange & Act
        var decorator = new DecoratorDefinition();

        // Assert
        Assert.Equal(string.Empty, decorator.Name);
        Assert.Empty(decorator.Args);
        Assert.False(decorator.IsNotDecorator);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenAppendingName_ThenNameIsUpdated()
    {
        // Arrange
        var decorator = new DecoratorDefinition();

        // Act
        decorator.AppendName("Decorator");
        decorator.AppendName("Name");

        // Assert
        Assert.Equal("DecoratorName", decorator.Name);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenSettingIsNotDecorator_ThenIsNotDecoratorIsSet()
    {
        // Arrange
        var decorator = new DecoratorDefinition();

        // Act
        decorator.IsNotDecorator = true;

        // Assert
        Assert.True(decorator.IsNotDecorator);
    }

}
