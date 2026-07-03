using System.Text;

namespace Tokens.Diagnostics;

internal static class AlignmentRenderer
{
    public static string Render(TokenizationDiagnostics diagnostics, string? templateContent, string? inputContent)
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
        sb.AppendLine($"Tokens: {tokenCount} | Input: {inputLineCount} lines | {summary.Verdict}");

        // Matched tokens
        if (matchedEvents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Matched Tokens ──");
            foreach (var evt in matchedEvents)
            {
                var line = evt.Location != null ? $" (line {evt.Location.Line})" : string.Empty;
                sb.AppendLine($"  ✓ {evt.TokenName} = \"{evt.Value}\"{line}");
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
                sb.AppendLine($"  ✗ {evt.TokenName}: {evt.Type} — {decoratorDesc} failed on '{evt.Value}'");

                var issue = summary.Issues.FirstOrDefault(i => i.TokenName == evt.TokenName
                    && (i.Type == DiagnosticIssueType.TransformerFailure
                     || i.Type == DiagnosticIssueType.ValidatorRejection));
                if (issue?.Hint != null)
                    sb.AppendLine($"      Hint: {issue.Hint}");
            }
        }

        // Unmatched tokens
        if (missedEvents.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Unmatched Tokens ──");
            foreach (var evt in missedEvents)
            {
                sb.AppendLine($"  ✗ {evt.TokenName} — preamble never found");

                var issue = summary.Issues.FirstOrDefault(i => i.TokenName == evt.TokenName);
                if (issue?.Hint != null)
                    sb.AppendLine($"      Hint: {issue.Hint}");
            }
        }

        // Summary
        sb.AppendLine();
        sb.AppendLine("═══ Summary ═══");
        sb.Append($"  Matched: {matchedEvents.Count} | Missed: {missedEvents.Count} | Failures: {failureEvents.Count}");

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
