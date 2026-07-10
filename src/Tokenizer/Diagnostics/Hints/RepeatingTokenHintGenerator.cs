namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for RepeatingTokenCutShort issues by looking for prior
/// validator or transformer failures for the same token in the event trace.
/// </summary>
internal sealed class RepeatingTokenHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                   DiagnosticResult trace)
    {
        if (issue.Type != DiagnosticIssueType.RepeatingTokenCutShort)
            return null;

        var tokenName = sourceEvent.TokenName;

        var priorValidatorFailure = trace.RawEvents
            .LastOrDefault(e => e.Type == DiagnosticEventType.ValidatorFailed
                             && string.Equals(e.TokenName, tokenName, StringComparison.Ordinal));

        if (priorValidatorFailure != null)
        {
            var validator = priorValidatorFailure.DecoratorName ?? "unknown validator";
            var value = priorValidatorFailure.Value ?? "unknown value";
            return $"Repeating token '{tokenName}' was disabled. " +
                   $"The value '{value}' failed {validator} validation.";
        }

        var priorTransformerFailure = trace.RawEvents
            .LastOrDefault(e => e.Type == DiagnosticEventType.TransformerFailed
                             && string.Equals(e.TokenName, tokenName, StringComparison.Ordinal));

        if (priorTransformerFailure != null)
        {
            var transformer = priorTransformerFailure.DecoratorName ?? "unknown transformer";
            var value = priorTransformerFailure.Value ?? "unknown value";
            return $"Repeating token '{tokenName}' was disabled. " +
                   $"The value '{value}' failed {transformer} transformation.";
        }

        if (!string.IsNullOrEmpty(sourceEvent.Detail))
        {
            return $"Repeating token '{tokenName}' was disabled: {sourceEvent.Detail}";
        }

        return null;
    }
}
