namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for Blocked issues, suggesting the user fix the blocking
/// token first. The blocker name is read from <see cref="DiagnosticEvent.Detail"/>.
/// </summary>
internal sealed class BlockedTokenHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                   DiagnosticResult trace)
    {
        if (issue.Type != DiagnosticIssueType.Blocked)
            return null;

        var blockerName = sourceEvent.Detail;
        if (string.IsNullOrEmpty(blockerName))
            return null;

        return $"Fix '{blockerName}' first — this token may match once '{blockerName}' is resolved.";
    }
}
