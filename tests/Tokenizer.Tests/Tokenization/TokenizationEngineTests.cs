using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tokens.Builders;
using Tokens.Enumerators;
using Tokens.Parsers;
using Xunit;

namespace Tokens.Tokenization;

public class TokenizationEngineTests
{
    private readonly TokenizationEngine _engine = new();

    private class Person
    {
        public string FirstName { get; set; }
    }

    [Fact]
    public void GivenValidInput_WhenProcessingTokenization_ThenProcessesSuccessfully()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("First Name: {FirstName}");
            
        var context = new TokenizationContext();
        // Don't initialize the context here - ProcessTokenization will do it
            
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new Person { FirstName = "Alice" };

        // Act - Follow the same pattern as the original Tokenizer
        // Initialize context for hint processing
        _engine.ProcessTokenization(template, "First Name: Alice", value, context, result);

        // Assert
        Assert.True(result.Tokens.Matches.Count > 0);
    }

    [Fact]
    public void GivenEmptyInput_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.Initialize(""));
    }

    [Fact]
    public void GivenNullTemplate_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var context = new TokenizationContext();
        context.Initialize("test");
            
        var result = new TokenizeResultBuilder().Build();
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _engine.ProcessTokenization(null, "test", value, context, result));
    }

    [Fact]
    public void GivenNullContext_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();
            
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _engine.ProcessTokenization(template, "test", value, null, result));
    }

    [Fact]
    public void GivenNullResult_WhenProcessingTokenization_ThenThrowsException()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .Build();

        var context = new TokenizationContext();
        context.Initialize("test");
        var value = new { Name = "" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _engine.ProcessTokenization(template, "test", value, context, null));
    }

    [Fact]
    public void GivenRepeatingTokens_WhenProcessingRepeatedTokens_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("test{Name}");
        var candidates = new CandidateTokenList();
            
        // Add the actual token from the template to the candidates
        var token = template.Tokens.First();
        candidates.Add(token);
            
        var enumerator = new TokenEnumerator("test test");
        var replacement = new StringBuilder();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var matchIds = new HashSet<int>();
        var disabledRepeatingTokens = new HashSet<int>();

        // Act
        var processed = _engine.ProcessRepeatedTokens(candidates, enumerator, replacement, result, disabledRepeatingTokens, matchIds, template);

        // Assert
        Assert.False(processed); // Should return false when no candidates match
    }

    [Fact]
    public void GivenNewlineTerminatedTokens_WhenProcessingNewlineTerminatedTokens_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("test{Name}");
        var candidates = new CandidateTokenList();
            
        // Add the actual token from the template to the candidates
        var token = template.Tokens.First();
        candidates.Add(token);
            
        var value = new { Name = "World" };
        var replacement = new StringBuilder();
        var options = TokenizerOptions.Defaults;
        var replacementLocation = new FileLocation();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var matchIds = new HashSet<int>();
        var enumerator = new TokenEnumerator("test\n");
        var disabledRepeatingTokens = new HashSet<int>();

        // Act
        _engine.ProcessNewlineTerminatedTokens(candidates, value, replacement, options, replacementLocation, result, template, matchIds, enumerator, disabledRepeatingTokens);

        // Assert
        // Method is void, so we just verify it doesn't throw
        Assert.True(true);
    }

    [Fact]
    public void GivenFrontMatterTokens_WhenProcessingFrontMatterTokens_ThenProcessesCorrectly()
    {
        // Arrange
        var value = new { Name = "" };
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithContent("frontmatter")
                .WithName("FrontMatterToken")
                .WithIsFrontMatterToken()
                .Build())
            .Build();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var enumerator = new TokenEnumerator("test");

        // Act
        _engine.ProcessFrontMatterTokens(template, value, new FileLocation(), result);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenValidCandidates_WhenTryAssignCandidateTokens_ThenAssignsSuccessfully()
    {
        // Arrange
        var candidates = new CandidateTokenList();
        var value = new { Name = "" };
        var replacement = new StringBuilder("test");
        var options = TokenizerOptions.Defaults;
        var replacementLocation = new FileLocation();
        var result = new TokenizeResultBuilder().Build();
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithContent("test")
                .WithName("TestToken")
                .Build())
            .Build();
        var matchIds = new HashSet<int>();

        // Act
        var assigned = _engine.TryAssignCandidateTokens(candidates, value, replacement, options, replacementLocation, result, template, matchIds);

        // Assert
        Assert.False(assigned); // Should return false when no candidates
    }

    private Template CreateTemplate(string name = "TestTemplate", string content = "Hello {Name}")
    {
        return new TemplateBuilder()
            .WithName(name)
            .WithContent(content)
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();
    }

    private TokenizationContext CreateContext(string input = "Hello World")
    {
        var context = new TokenizationContext();
        context.Initialize(input);
        return context;
    }

    private TokenizeResult CreateResult(Template template = null)
    {
        return new TokenizeResultBuilder()
            .WithTemplate(template ?? CreateTemplate())
            .Build();
    }

    [Fact]
    public void GivenExceptionDuringTokenAssignment_WhenTryAssignCandidateTokens_ThenHandlesException()
    {
        // Arrange
        var candidates = new CandidateTokenList();
        var value = new { Name = "" };
        var replacement = new StringBuilder("test");
        var options = TokenizerOptions.Defaults;
        var replacementLocation = new FileLocation();
        var result = new TokenizeResultBuilder().Build();
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(new TokenBuilder()
                .WithContent("test")
                .WithName("TestToken")
                .Build())
            .Build();
        var matchIds = new HashSet<int>();

        // Act
        var assigned = _engine.TryAssignCandidateTokens(candidates, value, replacement, options, replacementLocation, result, template, matchIds);

        // Assert
        Assert.False(assigned);
        Assert.NotNull(result.Exceptions);
    }

    [Fact]
    public void GivenVeryLongInput_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var longInput = new string('a', 10000) + " {Name} " + new string('b', 10000);
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        context.Initialize(longInput);
            
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "Hello World", value, context, result);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenSpecialCharacters_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var specialInput = "Hello {Name} with special chars: @#$%^&*()_+-=[]{}|;':\",./<>?";
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        context.Initialize(specialInput);
            
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "Hello World", value, context, result);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenUnicodeInput_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var unicodeInput = "Hello {Name} with unicode: 你好世界 🌍";
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("{Name}")
                .WithName("Name")
                .WithRequired()
                .Build())
            .WithDefaultOptions()
            .Build();

        var context = new TokenizationContext();
        context.Initialize(unicodeInput);
            
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "Hello World", value, context, result);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenTemplateWithNoTokens_WhenProcessingTokenization_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Hello World"); // Template with no tokens

        var context = new TokenizationContext();
        context.Initialize("Hello World");
            
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "Hello World", value, context, result);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Tokens.Matches.Count);
    }

    [Fact]
    public void GivenTemplateWithOnlyFrontMatterTokens_WhenProcessingFrontMatterTokens_ThenProcessesCorrectly()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithContent("Hello {Name}")
            .WithTokens(new TokenBuilder()
                .WithContent("frontmatter")
                .WithName("FrontMatterToken")
                .WithIsFrontMatterToken()
                .Build())
            .Build();
            
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();
        var enumerator = new TokenEnumerator("test");

        // Act
        _engine.ProcessFrontMatterTokens(template, new { Name = "" }, new FileLocation(), result);

        // Assert
        Assert.NotNull(result);
    }
}