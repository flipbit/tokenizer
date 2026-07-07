using Xunit;

namespace Tokens;

public class ImmutableCollectionsTests
{
    [Fact]
    public void GivenTokenResult_WhenAccessingMatches_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(TokenResult).GetProperty("Matches")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<TokenMatch>), propertyType);
    }

    [Fact]
    public void GivenTokenResult_WhenAccessingMisses_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(TokenResult).GetProperty("Misses")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<Token>), propertyType);
    }

    [Fact]
    public void GivenHintResult_WhenAccessingMatches_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(HintResult).GetProperty("Matches")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<HintMatch>), propertyType);
    }

    [Fact]
    public void GivenHintResult_WhenAccessingMisses_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(HintResult).GetProperty("Misses")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<Hint>), propertyType);
    }

    [Fact]
    public void GivenTokenizeResultBase_WhenAccessingExceptions_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(TokenizeResultBase).GetProperty("Exceptions")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<Exception>), propertyType);
    }

    [Fact]
    public void GivenTokenMatcherResult_WhenAccessingResults_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(TokenMatcherResult).GetProperty("Results")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<TokenizeResult>), propertyType);
    }

    [Fact]
    public void GivenGenericTokenMatcherResult_WhenAccessingResults_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(TokenMatcherResult<TestClass>).GetProperty("Results")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<TokenizeResult<TestClass>>), propertyType);
    }

    [Fact]
    public void GivenTemplate_WhenAccessingHints_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(Template).GetProperty("Hints")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<Hint>), propertyType);
    }

    [Fact]
    public void GivenTemplate_WhenAccessingTags_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(Template).GetProperty("Tags")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<string>), propertyType);
    }

    [Fact]
    public void GivenToken_WhenAccessingDecorators_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(Token).GetProperty("Decorators")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<TokenDecoratorContext>), propertyType);
    }

    [Fact]
    public void GivenTokenDecoratorContext_WhenAccessingParameters_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(TokenDecoratorContext).GetProperty("Parameters")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<string>), propertyType);
    }

    [Fact]
    public void GivenTokenizeResult_WhenAccessingMatches_ThenPropertyTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var propertyType = typeof(TokenizeResult).GetProperty("Matches")!.PropertyType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<TokenMatch>), propertyType);
    }

    [Fact]
    public void GivenTokenizeResult_WhenCallingAll_ThenReturnTypeIsIReadOnlyList()
    {
        // Arrange & Act
        var returnType = typeof(TokenizeResult).GetMethod("All")!.ReturnType;

        // Assert
        Assert.Equal(typeof(IReadOnlyList<object>), returnType);
    }

    private sealed class TestClass;
}
