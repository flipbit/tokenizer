using System.Collections.Generic;
using System.Linq;
using Tokens.Compilation.Definitions;
using Xunit;

namespace Tokens.Tests.Compilation.Definitions;

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

    [Fact]
    public void GivenDecoratorDefinition_WhenAddingArgs_ThenArgsAreAdded()
    {
        // Arrange
        var decorator = new DecoratorDefinition();

        // Act
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");

        // Assert
        Assert.Equal(3, decorator.Args.Count);
        Assert.Equal("arg1", decorator.Args[0]);
        Assert.Equal("arg2", decorator.Args[1]);
        Assert.Equal("arg3", decorator.Args[2]);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenClearingArgs_ThenArgsAreCleared()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");

        // Act
        decorator.Args.Clear();

        // Assert
        Assert.Empty(decorator.Args);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenRemovingArg_ThenArgIsRemoved()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");

        // Act
        decorator.Args.Remove("arg2");

        // Assert
        Assert.Equal(2, decorator.Args.Count);
        Assert.Equal("arg1", decorator.Args[0]);
        Assert.Equal("arg3", decorator.Args[1]);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenCheckingContainsArg_ThenReturnsCorrectResult()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");

        // Act & Assert
        Assert.True(decorator.Args.Contains("arg1"));
        Assert.True(decorator.Args.Contains("arg2"));
        Assert.False(decorator.Args.Contains("arg3"));
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenGettingArgIndex_ThenReturnsCorrectIndex()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");

        // Act & Assert
        Assert.Equal(0, decorator.Args.IndexOf("arg1"));
        Assert.Equal(1, decorator.Args.IndexOf("arg2"));
        Assert.Equal(2, decorator.Args.IndexOf("arg3"));
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenInsertingArg_ThenArgIsInserted()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg3");

        // Act
        decorator.Args.Insert(1, "arg2");

        // Assert
        Assert.Equal(3, decorator.Args.Count);
        Assert.Equal("arg1", decorator.Args[0]);
        Assert.Equal("arg2", decorator.Args[1]);
        Assert.Equal("arg3", decorator.Args[2]);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenAccessingArgByIndex_ThenReturnsCorrectArg()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");

        // Act & Assert
        Assert.Equal("arg1", decorator.Args[0]);
        Assert.Equal("arg2", decorator.Args[1]);
        Assert.Equal("arg3", decorator.Args[2]);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenSettingArgByIndex_ThenArgIsSet()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");

        // Act
        decorator.Args[1] = "modified_arg2";

        // Assert
        Assert.Equal("arg1", decorator.Args[0]);
        Assert.Equal("modified_arg2", decorator.Args[1]);
        Assert.Equal("arg3", decorator.Args[2]);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenRemovingArgAt_ThenArgIsRemoved()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");

        // Act
        decorator.Args.RemoveAt(1);

        // Assert
        Assert.Equal(2, decorator.Args.Count);
        Assert.Equal("arg1", decorator.Args[0]);
        Assert.Equal("arg3", decorator.Args[1]);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenCopyingToArray_ThenArrayIsCopied()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");
        var array = new string[3];

        // Act
        decorator.Args.CopyTo(array, 0);

        // Assert
        Assert.Equal("arg1", array[0]);
        Assert.Equal("arg2", array[1]);
        Assert.Equal("arg3", array[2]);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenCheckingIsReadOnly_ThenReturnsFalse()
    {
        // Arrange
        var decorator = new DecoratorDefinition();

        // Act & Assert
        Assert.False(decorator.Args.IsReadOnly);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenEnumeratingArgs_ThenArgsAreEnumerated()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");
        var enumeratedArgs = new List<string>();

        // Act
        foreach (var arg in decorator.Args)
        {
            enumeratedArgs.Add(arg);
        }

        // Assert
        Assert.Equal(3, enumeratedArgs.Count);
        Assert.Equal("arg1", enumeratedArgs[0]);
        Assert.Equal("arg2", enumeratedArgs[1]);
        Assert.Equal("arg3", enumeratedArgs[2]);
    }

    [Fact]
    public void GivenDecoratorDefinition_WhenUsingLinq_ThenLinqOperationsWork()
    {
        // Arrange
        var decorator = new DecoratorDefinition();
        decorator.Args.Add("arg1");
        decorator.Args.Add("arg2");
        decorator.Args.Add("arg3");

        // Act
        var filteredArgs = decorator.Args.Where(arg => arg.Contains("arg2")).ToList();
        var firstArg = decorator.Args.First();
        var lastArg = decorator.Args.Last();

        // Assert
        Assert.Single(filteredArgs);
        Assert.Equal("arg2", filteredArgs[0]);
        Assert.Equal("arg1", firstArg);
        Assert.Equal("arg3", lastArg);
    }
}
