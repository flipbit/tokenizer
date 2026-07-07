using Tokens.Builders;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tokenization;

public class FrontMatterProcessorTests
{
    [Fact]
    public void GivenFrontMatterToken_WhenProcessing_ThenAssignsAndRecordsMatch()
    {
        // Arrange
        var token = new TokenBuilder()
            .WithName("TemplateName")
            .WithIsFrontMatterToken()
            .Build();
        var template = new TemplateBuilder()
            .WithName("Test")
            .WithTokens(token)
            .WithDefaultOptions()
            .Build();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var collector = new DiagnosticCollector(inputContent: null);
        var assigner = new TokenAssigner(template.Options, collector);
        var location = new FileLocation();

        // Act
        FrontMatterProcessor.Process(template, targetObject: null, result, assigner, location);

        // Assert
        Assert.Contains(collector.GetResult()!.Events,
            e => e.Type == DiagnosticEventType.FrontMatterTokenAssigned);
    }

    [Fact]
    public void GivenNonFrontMatterToken_WhenProcessing_ThenSkipsToken()
    {
        // Arrange
        var token = new TokenBuilder()
            .WithName("Name")
            .WithPreamble("Name: ")
            .Build();
        var template = new TemplateBuilder()
            .WithName("Test")
            .WithTokens(token)
            .WithDefaultOptions()
            .Build();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var collector = new DiagnosticCollector(inputContent: null);
        var assigner = new TokenAssigner(template.Options, collector);
        var location = new FileLocation();

        // Act
        FrontMatterProcessor.Process(template, targetObject: null, result, assigner, location);

        // Assert
        var diagnosticResult = collector.GetResult();
        Assert.DoesNotContain(diagnosticResult!.Events,
            e => e.Type == DiagnosticEventType.FrontMatterTokenAssigned);
        Assert.DoesNotContain(diagnosticResult.Events,
            e => e.Type == DiagnosticEventType.FrontMatterTokenFailed);
    }

    [Fact]
    public void GivenFrontMatterTokenThatFailsAssignment_WhenProcessing_ThenRecordsFailedEvent()
    {
        // Arrange — a front matter token with an empty name causes Assign to return false
        var token = new TokenBuilder()
            .WithName(" ")
            .WithIsFrontMatterToken()
            .Build();
        var template = new TemplateBuilder()
            .WithName("Test")
            .WithTokens(token)
            .WithDefaultOptions()
            .Build();
        var result = new TokenizeResultBuilder().WithTemplate(template).Build();
        var collector = new DiagnosticCollector(inputContent: null);
        var assigner = new TokenAssigner(template.Options, collector);
        var location = new FileLocation();

        // Act
        FrontMatterProcessor.Process(template, targetObject: null, result, assigner, location);

        // Assert
        Assert.Contains(collector.GetResult()!.Events,
            e => e.Type == DiagnosticEventType.FrontMatterTokenFailed);
        Assert.Empty(result.Tokens.Matches);
    }
}
