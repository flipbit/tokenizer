using System.Linq;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Xunit;

namespace Tokens.Tests.Diagnostics;

public class DiagnosticCollectorTests
{
    [Fact]
    public void GivenNullCollector_WhenRecordingEvent_ThenGetResultReturnsNull()
    {
        // Arrange
        var collector = NullDiagnosticCollector.Instance;

        // Act
        collector.Record(DiagnosticEventType.TokenizationStarted, value: "test");
        var result = collector.GetResult();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GivenActiveCollector_WhenRecordingEvent_ThenEventIsStored()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");

        // Act
        collector.Record(DiagnosticEventType.TokenAssigned,
            tokenName: "DomainName", tokenId: 1,
            location: new FileLocation(), value: "bbc.co.uk");
        var result = collector.GetResult();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Events);
        Assert.Equal(DiagnosticEventType.TokenAssigned, result.Events[0].Type);
        Assert.Equal("DomainName", result.Events[0].TokenName);
        Assert.Equal("bbc.co.uk", result.Events[0].Value);
    }

    [Fact]
    public void GivenActiveCollector_WhenRecordingMultipleEvents_ThenEventsAreInOrder()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");

        // Act
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var result = collector.GetResult();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result!.Events.Count);
        Assert.Equal(DiagnosticEventType.TokenizationStarted, result.Events[0].Type);
        Assert.Equal(DiagnosticEventType.TokenizationCompleted, result.Events[3].Type);
    }

    [Fact]
    public void GivenDiagnostics_WhenQueryingFailures_ThenReturnsOnlyFailureEvents()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: "Second",
            decoratorName: "IsEmail", value: "notanemail");
        collector.Record(DiagnosticEventType.TransformerFailed, tokenName: "Third",
            decoratorName: "ToDateTimeUtc", value: "bad-date");
        collector.Record(DiagnosticEventType.TokenMissed, tokenName: "Fourth");

        // Act
        var result = collector.GetResult()!;

        // Assert
        var failures = result.Failures.ToList();
        Assert.Equal(3, failures.Count);
        Assert.All(failures, f => Assert.Contains(f.Type, new[]
        {
            DiagnosticEventType.ValidatorFailed,
            DiagnosticEventType.TransformerFailed,
            DiagnosticEventType.TokenMissed
        }));
    }

    [Fact]
    public void GivenDiagnostics_WhenQueryingForToken_ThenReturnsEventsForThatToken()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "Second");

        // Act
        var result = collector.GetResult()!;
        var firstEvents = result.ForToken("First").ToList();

        // Assert
        Assert.Equal(2, firstEvents.Count);
        Assert.All(firstEvents, e => Assert.Equal("First", e.TokenName));
    }

    [Fact]
    public void GivenDiagnostics_WhenQueryingFirstFailure_ThenReturnsFirstFailureEvent()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.ValidatorFailed, tokenName: "Second");
        collector.Record(DiagnosticEventType.TransformerFailed, tokenName: "Third");

        // Act
        var result = collector.GetResult()!;

        // Assert
        Assert.NotNull(result.FirstFailure);
        Assert.Equal("Second", result.FirstFailure!.TokenName);
        Assert.Equal(DiagnosticEventType.ValidatorFailed, result.FirstFailure.Type);
    }

    [Fact]
    public void GivenDiagnosticsWithNoFailures_WhenQueryingFirstFailure_ThenReturnsNull()
    {
        // Arrange
        var collector = new DiagnosticCollector("template", "input");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");

        // Act
        var result = collector.GetResult()!;

        // Assert
        Assert.Null(result.FirstFailure);
    }
}
