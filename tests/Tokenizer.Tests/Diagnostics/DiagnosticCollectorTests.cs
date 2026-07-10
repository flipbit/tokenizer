using Tokens.Enumerators;
using Xunit;

namespace Tokens.Diagnostics;

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
    public void GivenRuntimeCollector_WhenRecordingEvent_ThenEventIsStored()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");

        // Act
        collector.Record(DiagnosticEventType.TokenAssigned,
            tokenName: "DomainName", tokenId: 1,
            location: new FileLocation(), value: "bbc.co.uk");
        var result = collector.GetResult();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.RawEvents);
        Assert.Equal(DiagnosticEventType.TokenAssigned, result.RawEvents[0].Type);
        Assert.Equal("DomainName", result.RawEvents[0].TokenName);
        Assert.Equal("bbc.co.uk", result.RawEvents[0].Value);
    }

    [Fact]
    public void GivenRuntimeCollector_WhenRecordingMultipleEvents_ThenEventsAreInOrder()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");

        // Act
        collector.Record(DiagnosticEventType.TokenizationStarted);
        collector.Record(DiagnosticEventType.PreambleMatched, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "First");
        collector.Record(DiagnosticEventType.TokenizationCompleted);
        var result = collector.GetResult();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result!.RawEvents.Count);
        Assert.Equal(DiagnosticEventType.TokenizationStarted, result.RawEvents[0].Type);
        Assert.Equal(DiagnosticEventType.TokenizationCompleted, result.RawEvents[3].Type);
    }

    [Fact]
    public void GivenRuntimeCollector_WhenGettingCompilationResult_ThenReturnsNull()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");

        // Act
        var result = collector.GetCompilationResult();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GivenCompilationCollector_WhenRecordingCompilationEvent_ThenEventIsStored()
    {
        // Arrange
        var collector = new CompilationDiagnosticCollector();

        // Act
        collector.RecordCompilation(CompilationEventType.TokenCreated, tokenName: "DomainName", tokenId: 1);
        var result = collector.GetCompilationResult();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Events);
        Assert.Equal(CompilationEventType.TokenCreated, result.Events[0].Type);
        Assert.Equal("DomainName", result.Events[0].TokenName);
    }

    [Fact]
    public void GivenCompilationCollector_WhenRecordingRuntimeEvent_ThenEventIsDiscarded()
    {
        // Arrange
        var collector = new CompilationDiagnosticCollector();

        // Act
        collector.Record(DiagnosticEventType.TokenAssigned, tokenName: "DomainName", tokenId: 1);
        var result = collector.GetCompilationResult();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result!.Events);
    }

    [Fact]
    public void GivenCompilationCollector_WhenGettingResult_ThenReturnsNull()
    {
        // Arrange
        var collector = new CompilationDiagnosticCollector();

        // Act
        var result = collector.GetResult();

        // Assert
        Assert.Null(result);
    }
}
