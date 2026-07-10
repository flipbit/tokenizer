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
    public string? TryGenerateHint(DiagnosticIssueType type, string? tokenName,
                                   DiagnosticEvent sourceEvent, DiagnosticResult trace)
    {
        if (type != DiagnosticIssueType.ValidatorRejection &&
            type != DiagnosticIssueType.TransformerFailure)
        {
            return null;
        }

        if (tokenName == null)
            return null;

        if (trace.RejectionsPerToken == null ||
            !trace.RejectionsPerToken.TryGetValue(tokenName, out var rejections) ||
            rejections.Count < 2)
        {
            return null;
        }

        if (!ReferenceEquals(rejections[rejections.Count - 1], sourceEvent))
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
}
