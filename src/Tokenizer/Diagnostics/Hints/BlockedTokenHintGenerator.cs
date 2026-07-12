namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for Blocked issues, suggesting the user fix the blocking
/// token first. The blocker name is read from <see cref="TokenizationEvent.Detail"/>.
/// </summary>
internal sealed class BlockedTokenHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   TokenizationEvent sourceEvent, BuildContext context)
    {
        if (type != DiagnosticIssueType.Blocked)
            return null;

        var blockerName = sourceEvent.Detail;
        if (string.IsNullOrEmpty(blockerName))
            return null;

        return $"Fix '{blockerName}' first — this token may match once '{blockerName}' is resolved.";
    }
}
