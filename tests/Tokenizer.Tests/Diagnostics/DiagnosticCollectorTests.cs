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
        collector.Record(TokenizationEventType.TokenizationStarted, value: "test");
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
        collector.Record(TokenizationEventType.TokenAssigned,
            tokenName: "DomainName", tokenId: 1,
            location: new FileLocation(), value: "bbc.co.uk");
        var result = collector.GetResult();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.RawEvents);
        Assert.Equal(TokenizationEventType.TokenAssigned, result.RawEvents[0].Type);
        Assert.Equal("DomainName", result.RawEvents[0].TokenName);
        Assert.Equal("bbc.co.uk", result.RawEvents[0].Value);
    }

    [Fact]
    public void GivenRuntimeCollector_WhenRecordingMultipleEvents_ThenEventsAreInOrder()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("input");

        // Act
        collector.Record(TokenizationEventType.TokenizationStarted);
        collector.Record(TokenizationEventType.PreambleMatched, tokenName: "First");
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "First");
        collector.Record(TokenizationEventType.TokenizationCompleted);
        var result = collector.GetResult();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result!.RawEvents.Count);
        Assert.Equal(TokenizationEventType.TokenizationStarted, result.RawEvents[0].Type);
        Assert.Equal(TokenizationEventType.TokenizationCompleted, result.RawEvents[3].Type);
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
        collector.Record(TokenizationEventType.TokenAssigned, tokenName: "DomainName", tokenId: 1);
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

    [Fact]
    public void GivenNullCollector_WhenCheckingIsEnabled_ThenReturnsFalse()
    {
        // Assert
        Assert.False(NullDiagnosticCollector.Instance.IsEnabled);
    }

    [Fact]
    public void GivenRuntimeCollector_WhenCheckingIsEnabled_ThenReturnsTrue()
    {
        // Arrange
        var collector = new RuntimeDiagnosticCollector("x");

        // Assert
        Assert.True(collector.IsEnabled);
    }

    [Fact]
    public void GivenCompilationCollector_WhenCheckingIsEnabled_ThenReturnsTrue()
    {
        // Arrange
        var collector = new CompilationDiagnosticCollector();

        // Assert
        Assert.True(collector.IsEnabled);
    }

    [Fact]
    public void GivenNullCollector_WhenGetCompilationResult_ThenReturnsNull()
    {
        // Act
        var result = NullDiagnosticCollector.Instance.GetCompilationResult();

        // Assert
        Assert.Null(result);
    }
}
