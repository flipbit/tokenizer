namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for PreambleNeverFound issues when the token is marked
/// optional in the template, reassuring the user that the miss is expected.
/// </summary>
internal sealed class OptionalTokenHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                   DiagnosticResult trace)
    {
        if (issue.Type != DiagnosticIssueType.PreambleNeverFound)
            return null;

        var tokenName = issue.TokenName;

        if (tokenName == null)
            return null;

        if (!trace.OptionalTokenNames.Contains(tokenName))
            return null;

        return $"Token '{tokenName}' is optional — no action needed unless you expected it to match.";
    }
}
