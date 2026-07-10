using System.Text;
using Tokens.Extensions;

namespace Tokens.Diagnostics.Hints;

/// <summary>
/// Generates a hint for ValidatorRejection or TransformerFailure issues when
/// the same token was rejected two or more times during tokenization.
/// Only fires for the last rejection event to avoid duplicate hints.
/// Lists all rejected values to help the user understand the pattern.
/// </summary>
internal sealed class MultipleRejectionHintGenerator : IHintGenerator
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

        if (tokenName == null)
            return null;

        var rejections = CollectRejections(trace, tokenName);

        if (rejections.Count < 2)
            return null;

        // Only fire on the last rejection to avoid duplicate hints
        var last = rejections[rejections.Count - 1];
        if (!string.Equals(last.Value, sourceEvent.Value, StringComparison.Ordinal))
            return null;

        var sb = new StringBuilder();
        sb.Append("Token was rejected ").Append(rejections.Count.ToInvariant()).Append(" times. Values tried: ");

        for (var i = 0; i < rejections.Count; i++)
        {
            if (i > 0)
                sb.Append(", ");

            var evt = rejections[i];
            sb.Append('\'').Append(evt.Value ?? string.Empty).Append('\'');

            if (evt.Location != null)
                sb.Append(" (line ").Append(evt.Location.Line.ToInvariant()).Append(')');
        }

        sb.Append('.');
        return sb.ToString();
    }

    private static List<DiagnosticEvent> CollectRejections(DiagnosticResult trace, string tokenName)
    {
        var result = new List<DiagnosticEvent>();

        foreach (var evt in trace.RawEvents)
        {
            if (!string.Equals(evt.TokenName, tokenName, StringComparison.Ordinal))
                continue;

            if (evt.Type == DiagnosticEventType.ValidatorFailed ||
                evt.Type == DiagnosticEventType.TransformerFailed)
            {
                result.Add(evt);
            }
        }

        return result;
    }
}
