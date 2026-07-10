using Xunit;

namespace Tokens.Diagnostics;

public class IssueCodeMapTests
{
    [Theory]
    [InlineData(DiagnosticIssueType.PreambleNeverFound, "TK001")]
    [InlineData(DiagnosticIssueType.ValidatorRejection, "TK002")]
    [InlineData(DiagnosticIssueType.TransformerFailure, "TK003")]
    [InlineData(DiagnosticIssueType.ValueMismatch, "TK004")]
    [InlineData(DiagnosticIssueType.RepeatingTokenCutShort, "TK005")]
    [InlineData(DiagnosticIssueType.UnmatchedInputSection, "TK006")]
    [InlineData(DiagnosticIssueType.HintMissing, "TK007")]
    public void GivenIssueType_WhenGetCode_ThenReturnsExpectedCode(DiagnosticIssueType type, string expectedCode)
    {
        // Act
        var code = IssueCodeMap.GetCode(type);

        // Assert
        Assert.Equal(expectedCode, code);
    }

    [Fact]
    public void GivenIssue_WhenCodeAccessed_ThenDerivedFromType()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.ValidatorRejection,
            TokenName = "Email",
            Description = "Validator rejected value.",
        };

        // Assert
        Assert.Equal("TK002", issue.Code);
    }

    [Fact]
    public void GivenPreambleNeverFoundIssue_WhenCodeAccessed_ThenReturnsTK001()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Type = DiagnosticIssueType.PreambleNeverFound,
            TokenName = "Name",
            Description = "Token 'Name' was never matched in the input.",
        };

        // Assert
        Assert.Equal("TK001", issue.Code);
    }

    [Fact]
    public void GivenAllIssueTypes_WhenMapped_ThenCodesAreUnique()
    {
        // Arrange
        var types = Enum.GetValues<DiagnosticIssueType>();

        // Act
        var codes = types.Select(IssueCodeMap.GetCode).ToList();

        // Assert
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }
}
