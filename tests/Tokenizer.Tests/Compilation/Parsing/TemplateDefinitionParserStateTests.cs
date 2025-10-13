using Tokens.Compilation.Parsing;
using Xunit;

namespace Tokens.Tests.Compilation.Parsing;

public class TemplateDefinitionParserStateTests
{
    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingAtStart_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.AtStart;

        // Assert
        Assert.Equal(0, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInFrontMatter_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InFrontMatter;

        // Assert
        Assert.Equal(1, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInFrontMatterOption_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InFrontMatterOption;

        // Assert
        Assert.Equal(2, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInFrontMatterOptionValue_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InFrontMatterOptionValue;

        // Assert
        Assert.Equal(3, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInFrontMatterComment_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InFrontMatterComment;

        // Assert
        Assert.Equal(4, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInPreamble_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InPreamble;

        // Assert
        Assert.Equal(5, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInTokenName_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InTokenName;

        // Assert
        Assert.Equal(6, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInDecorator_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InDecorator;

        // Assert
        Assert.Equal(7, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInDecoratorArgument_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InDecoratorArgument;

        // Assert
        Assert.Equal(8, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInDecoratorArgumentSingleQuotes_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InDecoratorArgumentSingleQuotes;

        // Assert
        Assert.Equal(9, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInDecoratorArgumentDoubleQuotes_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InDecoratorArgumentDoubleQuotes;

        // Assert
        Assert.Equal(10, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInDecoratorArgumentRunOff_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InDecoratorArgumentRunOff;

        // Assert
        Assert.Equal(11, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInTokenValue_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InTokenValue;

        // Assert
        Assert.Equal(12, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInTokenValueSingleQuotes_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InTokenValueSingleQuotes;

        // Assert
        Assert.Equal(13, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInTokenValueDoubleQuotes_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InTokenValueDoubleQuotes;

        // Assert
        Assert.Equal(14, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenCheckingInTokenValueRunOff_ThenHasCorrectValue()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InTokenValueRunOff;

        // Assert
        Assert.Equal(15, (int)state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenGettingAllValues_ThenAllValuesArePresent()
    {
        // Arrange & Act
        var values = System.Enum.GetValues<TemplateDefinitionParserState>();

        // Assert
        Assert.Equal(16, values.Length);
        Assert.Contains(TemplateDefinitionParserState.AtStart, values);
        Assert.Contains(TemplateDefinitionParserState.InFrontMatter, values);
        Assert.Contains(TemplateDefinitionParserState.InFrontMatterOption, values);
        Assert.Contains(TemplateDefinitionParserState.InFrontMatterOptionValue, values);
        Assert.Contains(TemplateDefinitionParserState.InFrontMatterComment, values);
        Assert.Contains(TemplateDefinitionParserState.InPreamble, values);
        Assert.Contains(TemplateDefinitionParserState.InTokenName, values);
        Assert.Contains(TemplateDefinitionParserState.InDecorator, values);
        Assert.Contains(TemplateDefinitionParserState.InDecoratorArgument, values);
        Assert.Contains(TemplateDefinitionParserState.InDecoratorArgumentSingleQuotes, values);
        Assert.Contains(TemplateDefinitionParserState.InDecoratorArgumentDoubleQuotes, values);
        Assert.Contains(TemplateDefinitionParserState.InDecoratorArgumentRunOff, values);
        Assert.Contains(TemplateDefinitionParserState.InTokenValue, values);
        Assert.Contains(TemplateDefinitionParserState.InTokenValueSingleQuotes, values);
        Assert.Contains(TemplateDefinitionParserState.InTokenValueDoubleQuotes, values);
        Assert.Contains(TemplateDefinitionParserState.InTokenValueRunOff, values);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenParsingFromString_ThenReturnsCorrectValue()
    {
        // Arrange & Act
        var state = System.Enum.Parse<TemplateDefinitionParserState>("InTokenName");

        // Assert
        Assert.Equal(TemplateDefinitionParserState.InTokenName, state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenParsingFromInt_ThenReturnsCorrectValue()
    {
        // Arrange & Act
        var state = (TemplateDefinitionParserState)7;

        // Assert
        Assert.Equal(TemplateDefinitionParserState.InDecorator, state);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenConvertingToString_ThenReturnsCorrectString()
    {
        // Arrange & Act
        var state = TemplateDefinitionParserState.InPreamble;
        var stateString = state.ToString();

        // Assert
        Assert.Equal("InPreamble", stateString);
    }

    [Fact]
    public void GivenTemplateDefinitionParserState_WhenComparingStates_ThenComparisonsWork()
    {
        // Arrange
        var state1 = TemplateDefinitionParserState.AtStart;
        var state2 = TemplateDefinitionParserState.InPreamble;
        var state3 = TemplateDefinitionParserState.InPreamble;

        // Act & Assert
        Assert.True(state1 < state2);
        Assert.True(state2 > state1);
        Assert.True(state2 == state3);
        Assert.True(state1 != state2);
    }
}
