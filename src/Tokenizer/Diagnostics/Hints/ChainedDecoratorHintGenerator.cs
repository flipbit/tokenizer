namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for ValidatorRejection or TransformerFailure issues when
/// a prior decorator on the same token succeeded in the event trace.
/// Returns null when there is no prior success (single-decorator chain).
/// </summary>
internal sealed class ChainedDecoratorHintGenerator : IHintGenerator
{
    /// <inheritdoc />
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.ValidatorRejection &&
            type != DiagnosticIssueType.TransformerFailure)
        {
            return null;
        }

        var failingDecorator = sourceEvent.DecoratorName;
        var value = sourceEvent.Value ?? string.Empty;

        if (tokenName == null || failingDecorator == null)
            return null;

        if (trace.DecoratorSuccessesPerToken == null ||
            !trace.DecoratorSuccessesPerToken.TryGetValue(tokenName, out var successes) ||
            successes.Count == 0)
        {
            return null;
        }

        var priorDecorator = successes[successes.Count - 1].DecoratorName;
        if (priorDecorator == null)
            return null;

        var action = type == DiagnosticIssueType.ValidatorRejection ? "rejected" : "failed on";
        return $"Decorator chain: '{priorDecorator}' succeeded \u2192 '{failingDecorator}' {action} value '{value}'.";
    }
}
