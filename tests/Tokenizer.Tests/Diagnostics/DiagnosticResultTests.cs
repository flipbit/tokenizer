using Xunit;

namespace Tokens.Diagnostics;

public class DiagnosticResultTests
{
    [Fact]
    public void GivenMixedEvents_WhenFailuresAccessed_ThenOnlyFailureTypesReturned()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Name" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Age" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.ValidatorFailed, TokenName = "Email" });

        // Act
        var failures = result.Failures.ToList();

        // Assert
        Assert.Equal(2, failures.Count);
        Assert.Contains(failures, e => e.Type == DiagnosticEventType.TokenMissed);
        Assert.Contains(failures, e => e.Type == DiagnosticEventType.ValidatorFailed);
        Assert.DoesNotContain(failures, e => e.Type == DiagnosticEventType.TokenAssigned);
    }

    [Fact]
    public void GivenEvents_WhenForTokenCalled_ThenOnlyMatchingEventsReturned()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Name" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Age" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Name" });

        // Act
        var nameEvents = result.ForToken("Name").ToList();

        // Assert
        Assert.Equal(2, nameEvents.Count);
        Assert.All(nameEvents, e => Assert.Equal("Name", e.TokenName));
    }

    [Fact]
    public void GivenFailure_WhenFirstFailureAccessed_ThenReturnsFirstFailureEvent()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenAssigned, TokenName = "Name" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.TokenMissed, TokenName = "Age" });
        result.AddEvent(new DiagnosticEvent { Type = DiagnosticEventType.ValidatorFailed, TokenName = "Email" });

        // Act
        var firstFailure = result.FirstFailure;

        // Assert
        Assert.NotNull(firstFailure);
        Assert.Equal(DiagnosticEventType.TokenMissed, firstFailure!.Type);
        Assert.Equal("Age", firstFailure.TokenName);
    }

    [Fact]
    public void GivenEmptyResult_WhenQueried_ThenReturnsEmptyCollections()
    {
        // Arrange
        var result = new DiagnosticResult(inputContent: null);

        // Act & Assert
        Assert.Empty(result.Events);
        Assert.Empty(result.Failures);
        Assert.Null(result.FirstFailure);
    }
}
