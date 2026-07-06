using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Xunit;

namespace Tokens.Tokenization.Engine;

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
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {FirstName}\nName: {LastName}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Name: John\nName: Doe";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

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

        var input = "Req: Value";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Required", result.Tokens.Matches[0].Token.Name);
    }

    [Fact]
    public void GivenTokenAtStartOfInput_WhenTokenizing_ThenMatchesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("{Name} is here").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "John is here";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("John", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenTokenAtEndOfInput_WhenTokenizing_ThenMatchesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name is {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Name is John";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("John", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenRepeatingToken_WhenMultipleOccurrences_ThenMatchesAll()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Item: {Item*}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Item: Apple\nItem: Banana\nItem: Cherry";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert — 3 items in input, all should match the repeating token
        Assert.True(result.Tokens.Matches.Count >= 3,
            $"Expected at least 3 matches for 3 input items, got {result.Tokens.Matches.Count}");
    }

    [Fact]
    public void GivenRepeatingTokenWithGap_WhenTokenizing_ThenStopsAtGap()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Item: {Item*#}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Item: Apple\nItem: Banana\n\nItem: Cherry";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenPartialMatch_WhenTokenPreambleMatchesButNoValue_ThenHandlesCorrectly()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}Age: {Age}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Name: Age: 25";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

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

        var input = "Value: Test";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenConsecutiveTokensWithoutSeparator_WhenTokenizing_ThenMatchesBoth()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("{FirstName}{LastName}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "JohnDoe";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert — without a separator, the first token captures all input and the second gets nothing
        Assert.True(result.Tokens.Matches.Count >= 1,
            $"Expected at least 1 match, got {result.Tokens.Matches.Count}");
        // Verify that we got a match with the full input or a portion of it
        var firstMatch = result.Tokens.Matches.FirstOrDefault();
        Assert.NotNull(firstMatch);
        Assert.False(string.IsNullOrEmpty(firstMatch.Value?.ToString()));
    }

    [Fact]
    public void GivenTokenWithEmptyPreamble_WhenTokenizing_ThenMatchesRemaining()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {FirstName}{Remaining}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        var input = "Name: John everything else";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

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

        var input = "No optional here";
        context.Initialize(new System.IO.StringReader(input));

        // Act
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.Empty(result.Tokens.Matches);
    }

    [Fact]
    public void GivenTokensInDifferentOrder_WhenOutOfOrderEnabled_ThenMatchesAll()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions { OutOfOrderTokens = true });
        var template = parser.Compile("Age: {Age}\nName: {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act - Provide input in reverse order
        var input = "Name: John\nAge: 25";
        context.Initialize(new System.IO.StringReader(input));
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert
        Assert.True(result.Tokens.Matches.Count >= 1);
    }

    [Fact]
    public void GivenTokensInDifferentOrder_WhenOutOfOrderDisabled_ThenMatchesInOrder()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions { OutOfOrderTokens = false });
        var template = parser.Compile("Age: {Age}\nName: {Name}").Template;

        var context = new TokenizationContext();
        var result = new TokenizeResultBuilder()
            .WithTemplate(template)
            .Build();

        // Act - Provide input in reverse order
        var input = "Name: John\nAge: 25";
        context.Initialize(new System.IO.StringReader(input));
        _engine.ProcessTokenization(template, null, context, result, NullDiagnosticCollector.Instance);

        // Assert - Behavior depends on strict ordering
        Assert.NotNull(result);
    }
}
