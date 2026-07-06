using Tokens.Enumerators;
using Xunit;

namespace Tokens.Diagnostics;

public class DiagnosticEventTests
{
    [Fact]
    public void GivenDiagnosticEvent_WhenCreated_ThenPropertiesAreSet()
    {
        // Arrange & Act
        var evt = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenAssigned,
            TokenName = "DomainName",
            TokenId = 1,
            Location = new FileLocation(),
            Value = "bbc.co.uk",
            Detail = "Assigned successfully",
            DecoratorName = null,
            DecoratorArgs = null,
        };

        // Assert
        Assert.Equal(DiagnosticEventType.TokenAssigned, evt.Type);
        Assert.Equal("DomainName", evt.TokenName);
        Assert.Equal(1, evt.TokenId);
        Assert.NotNull(evt.Location);
        Assert.Equal("bbc.co.uk", evt.Value);
        Assert.Equal("Assigned successfully", evt.Detail);
        Assert.Null(evt.DecoratorName);
        Assert.Null(evt.DecoratorArgs);
    }

    [Fact]
    public void GivenDiagnosticEvent_WhenCreatedWithDecoratorInfo_ThenDecoratorPropertiesAreSet()
    {
        // Arrange & Act
        var evt = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TransformerFailed,
            TokenName = "Registered",
            TokenId = 5,
            Value = "21/11/2005",
            DecoratorName = "ToDateTimeUtc",
            DecoratorArgs = new[] { "yyyy-MM-dd" },
        };

        // Assert
        Assert.Equal(DiagnosticEventType.TransformerFailed, evt.Type);
        Assert.Equal("ToDateTimeUtc", evt.DecoratorName);
        Assert.Single(evt.DecoratorArgs);
        Assert.Equal("yyyy-MM-dd", evt.DecoratorArgs[0]);
    }

    [Fact]
    public void GivenDiagnosticIssue_WhenCreated_ThenPropertiesAreSet()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.TransformerFailure,
            TokenName = "Registered",
            Description = "ToDateTimeUtc('yyyy-MM-dd') failed on '21/11/2005'",
            Location = new FileLocation(),
            Hint = "Value matches format 'dd/MM/yyyy'",
        };

        // Assert
        Assert.Equal(DiagnosticIssueType.TransformerFailure, issue.Type);
        Assert.Equal("Registered", issue.TokenName);
        Assert.NotNull(issue.Hint);
    }
}
