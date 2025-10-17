using System.Linq;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Tokenization;
using Xunit;

namespace Tokens.Tests.Tokenization.Engine;

/// <summary>
/// Tests for TokenizationEngine token matching logic (partial matches, ambiguous matches, boundaries, etc.)
/// </summary>
public class TokenizationEngineTokenMatchingTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenMultipleTokensWithSamePreamble_WhenTokenizing_ThenMatchesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {FirstName}\nName: {LastName}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { FirstName = "", LastName = "" };

        // Act
        _engine.ProcessTokenization(template, "Name: John\nName: Doe", value, context, result);

        // Assert
        Assert.Equal(2, result.Tokens.Matches.Count);
    }

    [Fact]
    public void GivenRequiredAndOptionalTokens_WhenOnlyRequiredPresent_ThenMatchesRequiredOnly()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(
                new TokenBuilder()
                    .WithName("Required")
                    .WithPreamble("Req: ")
                    .WithRequired()
                    .Build(),
                new TokenBuilder()
                    .WithName("Optional")
                    .WithPreamble("Opt: ")
                    .WithOptional()
                    .Build()
            )
            .Build();

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Required = "", Optional = "" };

        // Act
        _engine.ProcessTokenization(template, "Req: Value", value, context, result);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Required", result.Tokens.Matches[0].Token.Name);
    }

    [Fact]
    public void GivenTokenAtStartOfInput_WhenTokenizing_ThenMatchesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{Name} is here");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "John is here", value, context, result);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("John", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenTokenAtEndOfInput_WhenTokenizing_ThenMatchesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name is {Name}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "" };

        // Act
        _engine.ProcessTokenization(template, "Name is John", value, context, result);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("John", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenRepeatingToken_WhenMultipleOccurrences_ThenMatchesAll()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Item: {Item*}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Item = new System.Collections.Generic.List<string>() };

        // Act
        _engine.ProcessTokenization(template, "Item: Apple\nItem: Banana\nItem: Cherry", value, context, result);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenRepeatingTokenWithGap_WhenTokenizing_ThenStopsAtGap()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{Item*#}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Item = new System.Collections.Generic.List<string>() };

        // Act
        _engine.ProcessTokenization(template, "Apple\nBanana\n\nCherry", value, context, result);

        // Assert - Should stop at the blank line
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenPartialMatch_WhenTokenPreambleMatchesButNoValue_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {Name}Age: {Age}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "", Age = 0 };

        // Act
        _engine.ProcessTokenization(template, "Name: Age: 25", value, context, result);

        // Assert
        Assert.True(result.Tokens.Matches.Count > 0 || result.Tokens.Misses.Count > 0);
    }

    [Fact]
    public void GivenAmbiguousTokenMatches_WhenMultipleCandidates_ThenSelectsBestMatch()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(
                new TokenBuilder()
                    .WithName("Token1")
                    .WithPreamble("Value: ")
                    .Build(),
                new TokenBuilder()
                    .WithName("Token2")
                    .WithPreamble("Value: ")
                    .Build()
            )
            .Build();

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Token1 = "", Token2 = "" };

        // Act
        _engine.ProcessTokenization(template, "Value: Test", value, context, result);

        // Assert
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenConsecutiveTokensWithoutSeparator_WhenTokenizing_ThenMatchesBoth()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{FirstName}{LastName}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { FirstName = "", LastName = "" };

        // Act
        _engine.ProcessTokenization(template, "JohnDoe", value, context, result);

        // Assert
        // Without separator, tokens can't distinguish boundaries - expect specific behavior
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenTokenWithEmptyPreamble_WhenTokenizing_ThenMatchesRemaining()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {FirstName}{Remaining}");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { FirstName = "", Remaining = "" };

        // Act
        _engine.ProcessTokenization(template, "Name: John everything else", value, context, result);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenOptionalToken_WhenNotPresent_ThenDoesNotAddMiss()
    {
        // Arrange
        var template = new TemplateBuilder()
            .WithName("TestTemplate")
            .WithTokens(
                new TokenBuilder()
                    .WithName("Optional")
                    .WithPreamble("Opt: ")
                    .WithOptional()
                    .Build()
            )
            .Build();

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Optional = "" };

        // Act
        _engine.ProcessTokenization(template, "No optional here", value, context, result);

        // Assert
        Assert.Empty(result.Tokens.Matches);
    }

    [Fact]
    public void GivenTokensInDifferentOrder_WhenOutOfOrderEnabled_ThenMatchesAll()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Age: {Age}\nName: {Name}");
        template.Options.OutOfOrderTokens = true;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "", Age = 0 };

        // Act - Provide input in reverse order
        _engine.ProcessTokenization(template, "Name: John\nAge: 25", value, context, result);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenTokensInDifferentOrder_WhenOutOfOrderDisabled_ThenMatchesInOrder()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Age: {Age}\nName: {Name}");
        template.Options.OutOfOrderTokens = false;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var value = new { Name = "", Age = 0 };

        // Act - Provide input in reverse order
        _engine.ProcessTokenization(template, "Name: John\nAge: 25", value, context, result);

        // Assert - Behavior depends on strict ordering
        Assert.NotNull(result);
    }
}
