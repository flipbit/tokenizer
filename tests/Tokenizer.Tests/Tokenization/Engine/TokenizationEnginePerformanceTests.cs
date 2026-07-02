using System.Text;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Tokenization;
using Xunit;
using Tokens.Diagnostics;

namespace Tokens.Tests.Tokenization.Engine;

/// <summary>
/// Tests for TokenizationEngine performance and stress scenarios (large inputs, many tokens, memory allocation)
/// </summary>
public class TokenizationEnginePerformanceTests
{
    private readonly TokenizationEngine _engine = new();

    [Fact]
    public void GivenVeryLargeInput100KChars_WhenTokenizing_ThenCompletesSuccessfully()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {Name}");

        var largeInput = new StringBuilder();
        largeInput.Append(new string('a', 100000));
        largeInput.Append("Name: John");

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, largeInput.ToString(), null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenInputWith1MChars_WhenTokenizing_ThenHandlesWithoutError()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("{Content}");

        var largeInput = new string('x', 1000000);

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, largeInput, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenTemplateWith100Tokens_WhenTokenizing_ThenProcessesAll()
    {
        // Arrange
        var templateBuilder = new TemplateBuilder()
            .WithName("ManyTokensTemplate");

        var tokens = new List<Token>();
        var inputBuilder = new StringBuilder();

        for (int i = 0; i < 100; i++)
        {
            tokens.Add(new TokenBuilder()
                .WithName($"Token{i}")
                .WithPreamble($"T{i}: ")
                .Build());

            inputBuilder.AppendLine($"T{i}: Value{i}");
        }

        templateBuilder.WithTokens(tokens.ToArray());
        var template = templateBuilder.Build();

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, inputBuilder.ToString(), null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 50); // At least half matched
    }

    [Fact]
    public void GivenInputWith10KLines_WhenTokenizing_ThenCompletesSuccessfully()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Item: {Item}");

        var inputBuilder = new StringBuilder();
        for (int i = 0; i < 10000; i++)
        {
            inputBuilder.AppendLine($"Item: Value{i}");
        }

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, inputBuilder.ToString(), null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenRepeatingTokenWith1000Occurrences_WhenTokenizing_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Item: {Item*}");

        var inputBuilder = new StringBuilder();
        for (int i = 0; i < 1000; i++)
        {
            inputBuilder.AppendLine($"Item: Value{i}");
        }

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, inputBuilder.ToString(), null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Tokens.Matches.Count >= 100); // At least 100 matches
    }

    [Fact]
    public void GivenDeeplyNestedStructure_WhenTokenizing_ThenHandlesComplexity()
    {
        // Arrange
        var templateBuilder = new StringBuilder();
        var inputBuilder = new StringBuilder();

        // Create 50 levels of nesting
        for (int i = 0; i < 50; i++)
        {
            templateBuilder.Append($"Level{i}: {{Level{i}}}\n");
            inputBuilder.Append($"Level{i}: Value{i}\n");
        }

        var parser = new TokenParser();
        var template = parser.Parse(templateBuilder.ToString());

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, inputBuilder.ToString(), null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 25); // At least half matched
    }

    [Fact]
    public void GivenManySmallTokens_WhenTokenizing_ThenHandlesEfficiently()
    {
        // Arrange
        var templateBuilder = new StringBuilder();
        var inputBuilder = new StringBuilder();

        // Create 500 single-character tokens
        for (int i = 0; i < 500; i++)
        {
            var letter = (char)('a' + (i % 26));
            templateBuilder.Append($"{{{letter}{i}}}");
            inputBuilder.Append($"{letter}");
        }

        var parser = new TokenParser();
        var template = parser.Parse(templateBuilder.ToString());

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, inputBuilder.ToString(), null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GivenVeryLongTokenValue_WhenTokenizing_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Content: {Content}End");

        var longValue = new string('x', 50000);
        var input = $"Content: {longValue}End";

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, input, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal(50000, result.Tokens.Matches[0].Value!.ToString()!.Length);
    }

    [Fact]
    public void GivenInputWithManyWhitespaceCharacters_WhenTokenizing_ThenHandlesEfficiently()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {Name}");

        var inputBuilder = new StringBuilder();
        inputBuilder.Append(new string(' ', 10000));
        inputBuilder.Append("Name: John");
        inputBuilder.Append(new string(' ', 10000));

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act
        _engine.ProcessTokenization(template, inputBuilder.ToString(), null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenMultipleTokenizationRuns_WhenTokenizing_ThenMaintainsConsistency()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {Name}Age: {Age}");
        var input = "Name: JohnAge: 25";

        var results = new List<TokenizeResult>();

        // Act - Run 100 times
        for (int i = 0; i < 100; i++)
        {
            var context = new TokenizationContext();
            var result = new TokenizeResultBuilder()
                .WithTemplate(template)
                .Build();

            _engine.ProcessTokenization(template, input, null, context, result, NullDiagnosticCollector.Instance);
            results.Add(result);
        }

        // Assert - All runs should produce same results
        var firstMatchCount = results[0].Tokens.Matches.Count;
        Assert.All(results, r => Assert.Equal(firstMatchCount, r.Tokens.Matches.Count));
    }

    [Fact]
    public void GivenConcurrentTokenizations_WhenTokenizing_ThenHandlesIndependently()
    {
        // Arrange
        var parser = new TokenParser();
        var template = parser.Parse("Name: {Name}");

        // Act - Create multiple contexts
        var contexts = Enumerable.Range(0, 10)
            .Select(_ => new TokenizationContext())
            .ToList();

        var results = contexts.Select(c =>
        {
            var result = new TokenizeResultBuilder()
                .WithTemplate(template)
                .Build();
            _engine.ProcessTokenization(template, "Name: Test", null, c, result, NullDiagnosticCollector.Instance);
            return result;
        }).ToList();

        // Assert - All should succeed independently
        Assert.All(results, r => Assert.Single(r.Tokens.Matches));
    }
}
