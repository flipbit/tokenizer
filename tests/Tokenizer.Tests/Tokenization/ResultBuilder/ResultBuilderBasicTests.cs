using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Tokenization.ResultBuilderTests;

/// <summary>
/// Tests for basic ResultBuilder operations (add matches/misses, create results, etc.)
/// </summary>
public class ResultBuilderBasicTests
{
    private readonly Tokens.Tokenization.ResultBuilder _builder = new();

    [Fact]
    public void GivenTemplate_WhenCreateTokenizeResult_ThenReturnsValidResult()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
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
            .WithContent("Hello {Name}")
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
            .WithContent("{Name}")
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
            .WithContent("{Name}")
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

    [Fact]
    public void GivenTemplateAndMatch_WhenAddMatchedTokenIds_ThenAddsTokenIds()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello {Name}");

        var match = template.Tokens.First();
        var matchIds = new HashSet<int>();

        // Act
        _builder.AddMatchedTokenIds(template, match, matchIds);

        // Assert
        Assert.True(matchIds.Count > 0);
    }

    [Fact]
    public void GivenResultWithLastMatch_WhenWasLastMatchedToken_ThenReturnsTrue()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Name}").WithName("Name").Build();
        var result = new TokenizeResultBuilder()
            .WithMatches(new TokenMatch(token, "TestName", new FileLocation()))
            .Build();

        // Act
        var wasLast = _builder.WasLastMatchedToken(result, token);

        // Assert
        Assert.True(wasLast);
    }

    [Fact]
    public void GivenResultWithoutMatches_WhenWasLastMatchedToken_ThenReturnsFalse()
    {
        // Arrange
        var token = new TokenBuilder().WithContent("{Name}").WithName("Name").Build();
        var result = new TokenizeResultBuilder().Build();

        // Act
        var wasLast = _builder.WasLastMatchedToken(result, token);

        // Assert
        Assert.False(wasLast);
    }

    private Template CreateTemplate(string name = "TestTemplate", string content = "Hello {Name}")
    {
        return new TemplateBuilder()
            .WithName(name)
            .WithContent(content)
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .Build())
            .Build();
    }

    private TokenizeResult CreateResult(Template? template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? CreateTemplate())
            .Build();
    }

    private class TestClass
    {
        public string Name { get; set; } = null!;
    }
}
