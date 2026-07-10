namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for ValidatorRejection or TransformerFailure issues when
/// a prior decorator on the same token succeeded in the event trace.
/// Returns null when there is no prior success (single-decorator chain).
/// </summary>
internal sealed class ChainedDecoratorHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                   DiagnosticResult trace)
    {
        if (issue.Type != DiagnosticIssueType.ValidatorRejection &&
            issue.Type != DiagnosticIssueType.TransformerFailure)
        {
            return null;
        }

        var tokenName = sourceEvent.TokenName;
        var failingDecorator = sourceEvent.DecoratorName;
        var value = sourceEvent.Value ?? string.Empty;

        if (tokenName == null || failingDecorator == null)
            return null;

        string? priorDecorator = null;

        foreach (var evt in trace.RawEvents)
        {
            // Stop at the failing event — only consider prior successes
            if (ReferenceEquals(evt, sourceEvent))
                break;

            if (!string.Equals(evt.TokenName, tokenName, StringComparison.Ordinal))
                continue;

            if (evt.Type == DiagnosticEventType.ValidatorPassed ||
                evt.Type == DiagnosticEventType.TransformerSucceeded)
            {
                if (evt.DecoratorName != null)
                    priorDecorator = evt.DecoratorName;
            }
        }

        if (priorDecorator == null)
            return null;

        var action = issue.Type == DiagnosticIssueType.ValidatorRejection ? "rejected" : "failed on";
        return $"Decorator chain: '{priorDecorator}' succeeded \u2192 '{failingDecorator}' {action} value '{value}'.";
    }
}
