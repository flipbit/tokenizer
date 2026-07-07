using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Builders;
using Tokens.Compilation;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

public class CandidateProcessorTests
{
    [Fact]
    public void GivenMatchingCandidate_WhenTryAssign_ThenReturnsTrue()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("Alice");
        var location = new FileLocation();

        // Act
        var assigned = processor.TryAssign(context, location);

        // Assert
        Assert.True(assigned);
        Assert.Single(result.Tokens.Matches);
    }

    [Fact]
    public void GivenNonMatchingCandidate_WhenTryAssign_ThenReturnsFalse()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name:IsNumeric}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: NotANumber"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("NotANumber");
        var location = new FileLocation();

        // Act
        var assigned = processor.TryAssign(context, location);

        // Assert
        Assert.False(assigned);
    }

    [Fact]
    public void GivenRemainingCandidates_WhenProcessRemaining_ThenAssignsThem()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Bob"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("Bob");

        // Act
        processor.ProcessRemaining(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Bob", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenNoCandidates_WhenProcessRemaining_ThenDoesNothing()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Bob"));
        // No candidates added

        // Act
        processor.ProcessRemaining(context);

        // Assert
        Assert.Empty(result.Tokens.Matches);
    }

    [Fact]
    public void GivenEmptyReplacement_WhenProcessRemaining_ThenDoesNothing()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: "));
        context.Candidates.AddRange(template.Tokens);
        // Replacement is empty

        // Act
        processor.ProcessRemaining(context);

        // Assert
        Assert.Empty(result.Tokens.Matches);
    }

    [Fact]
    public void GivenNewlineTerminatedToken_WhenHandleNewline_ThenAssignsValue()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}\n").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice\n"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("Alice");

        // Act
        processor.HandleNewline(context);

        // Assert
        Assert.Single(result.Tokens.Matches);
        Assert.Equal("Alice", result.Tokens.Matches[0].Value);
    }

    [Fact]
    public void GivenNewlineTerminatedToken_WhenHandleNewline_ThenClearsCandidatesAndReplacement()
    {
        // Arrange
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}\n").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            targetObject: null, result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice\n"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("Alice");

        // Act
        processor.HandleNewline(context);

        // Assert
        Assert.False(context.Candidates.HasCandidates);
        Assert.Equal(0, context.Replacement.Length);
    }

    [Fact]
    public void GivenThrowingAssignment_WhenTryAssign_ThenReturnsFalseAndRecordsException()
    {
        // Arrange — use a target object whose property setter throws
        var parser = new TemplateCompiler(new TokenizerOptions());
        var template = parser.Compile("Name: {Name}").Template;
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var processor = new CandidateProcessor(
            new ThrowingTarget(), result, template,
            NullDiagnosticCollector.Instance,
            NullLogger<TokenizationEngine>.Instance);

        var context = new TokenizationContext();
        context.Initialize(new System.IO.StringReader("Name: Alice"));
        context.Candidates.AddRange(template.Tokens);
        context.Replacement.Append("Alice");
        var location = new FileLocation();

        // Act
        var assigned = processor.TryAssign(context, location);

        // Assert
        Assert.False(assigned);
        Assert.Single(result.Exceptions);
    }

    private class ThrowingTarget
    {
#pragma warning disable CA1822 // Accessed via reflection as instance property
        public string Name
#pragma warning restore CA1822
        {
            get => throw new InvalidOperationException("boom");
            set => throw new InvalidOperationException("boom");
        }
    }
}
