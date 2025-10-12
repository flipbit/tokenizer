using System;
using System.Collections.Generic;
using System.Linq;
using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Parsers;
using Xunit;

namespace Tokens.Tokenization;

public class ResultBuilderTests
{
    private readonly ResultBuilder _builder = new();

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
        var exception = new InvalidOperationException("Test exception");

        // Act
        _builder.AddException(exception, result);

        // Assert
        Assert.True(result.Exceptions.Count > 0);
        Assert.Contains(exception, result.Exceptions);
    }

    [Fact]
    public void GivenTemplateWithTokens_WhenBuildUnmatchedTokens_ThenAddsUnmatchedTokens()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello {Name} and {Age}");

        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _builder.BuildUnmatchedTokens(template, result);

        // Assert
        Assert.True(result.Tokens.Misses.Count > 0);
    }

    [Fact]
    public void GivenTemplateWithMatchedTokens_WhenBuildUnmatchedTokens_ThenOnlyAddsUnmatchedTokens()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello {Name} and {Age}");
            
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Add a match for the Name token
        var nameToken = template.Tokens.First(t => t.Name == "Name");
        result.Tokens.AddMatch(nameToken, "TestName", new FileLocation());

        // Act
        _builder.BuildUnmatchedTokens(template, result);

        // Assert
        Assert.Equal(1, result.Tokens.Misses.Count);
        Assert.Equal("Age", result.Tokens.Misses[0].Name);
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
            .WithMatches(new Match
            {
                Token = token,
                Value = "TestName",
                Location = new FileLocation()
            })
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

    [Fact]
    public void GivenNullResult_WhenAddTokenMatch_ThenThrowsException()
    {
        // Arrange
        var token = new TokenBuilder().Build();
        var assignedValue = "TestValue";
        var location = new FileLocation();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.AddTokenMatch(token, assignedValue, location, null));
    }

    [Fact]
    public void GivenNullResult_WhenAddTokenMiss_ThenThrowsException()
    {
        // Arrange
        var token = new TokenBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.AddTokenMiss(token, null));
    }

    [Fact]
    public void GivenNullResult_WhenAddException_ThenThrowsException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.AddException(exception, null));
    }

    [Fact]
    public void GivenNullResult_WhenBuildUnmatchedTokens_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.BuildUnmatchedTokens(template, null));
    }

    [Fact]
    public void GivenNullTemplate_WhenBuildUnmatchedTokens_ThenThrowsException()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.BuildUnmatchedTokens(null, result));
    }

    [Fact]
    public void GivenNullTemplate_WhenAddMatchedTokenIds_ThenThrowsException()
    {
        // Arrange
        var match = new TokenBuilder().Build();
        var matchIds = new HashSet<int>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.AddMatchedTokenIds(null, match, matchIds));
    }

    [Fact]
    public void GivenNullMatch_WhenAddMatchedTokenIds_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();
        var matchIds = new HashSet<int>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.AddMatchedTokenIds(template, null, matchIds));
    }

    [Fact]
    public void GivenNullMatchIds_WhenAddMatchedTokenIds_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder().Build();
        var match = new TokenBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.AddMatchedTokenIds(template, match, null));
    }

    [Fact]
    public void GivenNullResult_WhenWasLastMatchedToken_ThenThrowsException()
    {
        // Arrange
        var token = new TokenBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.WasLastMatchedToken(null, token));
    }

    [Fact]
    public void GivenNullToken_WhenWasLastMatchedToken_ThenThrowsException()
    {
        // Arrange
        var result = new TokenizeResultBuilder().Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _builder.WasLastMatchedToken(result, null));
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

    private TokenizeResult CreateResult(Template template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? CreateTemplate())
            .Build();
    }

    private class TestClass
    {
        public string Name { get; set; }
    }
}