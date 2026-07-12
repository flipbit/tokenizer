namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for RepeatingTokenCutShort issues by looking for prior
/// validator or transformer failures for the same token in the event index.
/// </summary>
internal sealed class RepeatingTokenHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   TokenizationEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.RepeatingTokenCutShort)
            return null;

        if (tokenName != null && trace.RejectionsPerToken != null &&
            trace.RejectionsPerToken.TryGetValue(tokenName, out var rejections) &&
            rejections.Count > 0)
        {
            var last = rejections[rejections.Count - 1];

            if (last.Type == TokenizationEventType.ValidatorFailed)
            {
                var validator = last.DecoratorName ?? "unknown validator";
                var value = last.Value ?? "unknown value";
                return $"Repeating token '{tokenName}' was disabled. " +
                       $"The value '{value}' failed {validator} validation.";
            }

            if (last.Type == TokenizationEventType.TransformerFailed)
            {
                var transformer = last.DecoratorName ?? "unknown transformer";
                var value = last.Value ?? "unknown value";
                return $"Repeating token '{tokenName}' was disabled. " +
                       $"The value '{value}' failed {transformer} transformation.";
            }
        }

        if (!string.IsNullOrEmpty(sourceEvent.Detail))
        {
            return $"Repeating token '{tokenName}' was disabled: {sourceEvent.Detail}";
        }

        return null;
    }
}
