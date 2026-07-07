using Tokens.Builders;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

/// <summary>
/// Tests for basic ResultBuilder operations (add matches/misses, create results, etc.)
/// </summary>
public class ResultBuilder_Basic_Tests
{
    private readonly ResultBuilder _builder = new();

    [Fact]
    public void GivenTemplate_WhenCreateTokenizeResult_ThenReturnsValidResult()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();

        // Act
        var result = _builder.CreateTokenizeResult(template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(template, result.Template);
    }

    [Fact]
    public void GivenTemplate_WhenCreateTokenizeResultGeneric_ThenReturnsValidTypedResult()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();

        // Act
        var result = _builder.CreateTokenizeResult<TestClass>(template);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(template, result.Template);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void GivenValidToken_WhenAddTokenMatch_ThenAddsMatch()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();
        var token = new TokenBuilder()
            .WithName("Name")
            .Build();
        var assignedValue = "TestValue";
        var location = new FileLocation();

        // Act
        _builder.AddTokenMatch(token, assignedValue, location, result);

        // Assert
        Assert.True(result.Tokens.Matches.Count > 0);
    }

    [Fact]
    public void GivenValidToken_WhenAddTokenMiss_ThenAddsMiss()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();
        var token = new TokenBuilder()
            .WithName("Name")
            .Build();

        // Act
        _builder.AddTokenMiss(token, result);

        // Assert
        Assert.True(result.Tokens.Misses.Count > 0);
    }

    [Fact]
    public void GivenException_WhenAddException_ThenAddsException()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();
        var exception = new System.InvalidOperationException("Test exception");

        // Act
        _builder.AddException(exception, result);

        // Assert
        Assert.True(result.Exceptions.Count > 0);
        Assert.Contains(exception, result.Exceptions);
    }

    private static Template CreateTemplate(string name = "TestTemplate")
    {
        return new TemplateBuilder()
            .WithName(name)
            .WithTokens(new TokenBuilder()
                .WithName("Name")
                .Build())
            .Build();
    }

    private static TokenizeResult CreateResult(Template? template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? CreateTemplate())
            .Build();
    }

    private sealed class TestClass
    {
        public string Name { get; set; } = null!;
    }
}
