using System.Text;
using Tokens.Extensions;

namespace Tokens.Diagnostics;

internal static class AlignmentRenderer
{
    public static string Render(DiagnosticResult diagnostics, string? inputContent)
    {
        var sb = new StringBuilder();
        var summary = diagnostics.Summary;
        var events = diagnostics.Events;

        var matchedEvents = events
            .Where(e => e.Type == DiagnosticEventType.TokenAssigned)
            .ToList();

        var failureEvents = events
            .Where(e => e.Type == DiagnosticEventType.TransformerFailed
                     || e.Type == DiagnosticEventType.ValidatorFailed)
            .ToList();

        var missedEvents = events
            .Where(e => e.Type == DiagnosticEventType.TokenMissed)
            .ToList();

        var inputLineCount = CountLines(inputContent);
        var tokenCount = matchedEvents.Count + missedEvents.Count;

        // Header
        sb.AppendLine("═══ Tokenization Alignment ═══");
        sb.Append("Tokens: ").Append(tokenCount).Append(" | Input: ").Append(inputLineCount).Append(" lines | ").AppendLine(summary.Verdict);

        // Matched tokens
        if (matchedEvents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Matched Tokens ──");
            foreach (var evt in matchedEvents)
            {
                var line = evt.Location != null ? $" (line {evt.Location.Line.ToInvariant()})" : string.Empty;
                sb.Append("  ✓ ").Append(evt.TokenName).Append(" = \"").Append(evt.Value).Append('"').AppendLine(line);
            }
        }

        // Failures (transformer/validator)
        if (failureEvents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Failures ──");
            foreach (var evt in failureEvents)
            {
                var decoratorDesc = BuildDecoratorDescription(evt);
                sb.Append("  ✗ ").Append(evt.TokenName).Append(": ").Append(evt.Type).Append(" — ").Append(decoratorDesc).Append(" failed on '").Append(evt.Value).AppendLine("'");

                var issue = summary.Issues.FirstOrDefault(i => string.Equals(i.TokenName, evt.TokenName, StringComparison.Ordinal)
                    && (i.Type == DiagnosticIssueType.TransformerFailure
                     || i.Type == DiagnosticIssueType.ValidatorRejection));
                if (issue?.Hint != null)
                    sb.Append("      Hint: ").AppendLine(issue.Hint);
            }
        }

        // Unmatched tokens
        if (missedEvents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Unmatched Tokens ──");
            foreach (var evt in missedEvents)
            {
                sb.Append("  ✗ ").Append(evt.TokenName).AppendLine(" — preamble never found");

                var issue = summary.Issues.FirstOrDefault(i => string.Equals(i.TokenName, evt.TokenName, StringComparison.Ordinal));
                if (issue?.Hint != null)
                    sb.Append("      Hint: ").AppendLine(issue.Hint);
            }
        }

        // Summary
        sb.AppendLine();
        sb.AppendLine("═══ Summary ═══");
        sb.Append("  Matched: ").Append(matchedEvents.Count).Append(" | Missed: ").Append(missedEvents.Count).Append(" | Failures: ").Append(failureEvents.Count);

        return sb.ToString();
    }

    private static int CountLines(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var count = 1;
        foreach (var c in content!)
        {
            if (c == '\n')
                count++;
        }
        return count;
    }

    private static string BuildDecoratorDescription(DiagnosticEvent evt)
    {
        if (string.IsNullOrEmpty(evt.DecoratorName))
            return "decorator";

        if (evt.DecoratorArgs != null && evt.DecoratorArgs.Length > 0)
            return $"{evt.DecoratorName}({string.Join(", ", evt.DecoratorArgs)})";

        return evt.DecoratorName!;
    }
}
