using System.Text;
using Tokens.Extensions;

namespace Tokens.Diagnostics;

internal static class AlignmentRenderer
{
    public static string Render(DiagnosticResult diagnostics, string? inputContent)
    {
        var sb = new StringBuilder();
        var tokens = diagnostics.Tokens;

        var matchedTokens = tokens.Where(t => t.Outcome == TokenOutcome.Matched).ToList();
        var rejectedTokens = tokens.Where(t => t.Outcome == TokenOutcome.Rejected).ToList();
        var neverFoundTokens = tokens.Where(t => t.Outcome == TokenOutcome.NeverFound).ToList();

        var inputLineCount = CountLines(inputContent);
        var totalTokens = matchedTokens.Count + rejectedTokens.Count + neverFoundTokens.Count;

        // Header
        sb.AppendLine("═══ Tokenization Alignment ═══");
        sb.Append("Tokens: ").Append(totalTokens).Append(" | Input: ").Append(inputLineCount).Append(" lines | ").AppendLine(diagnostics.Verdict);

        // Matched tokens
        if (matchedTokens.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Matched Tokens ──");
            foreach (var token in matchedTokens)
            {
                var line = token.AssignedLocation != null ? $" (line {token.AssignedLocation.Line.ToInvariant()})" : string.Empty;
                sb.Append("  ✓ ").Append(token.TokenName).Append(" = \"").Append(token.AssignedValue).Append('"').AppendLine(line);
            }
        }

        // Failures (rejected tokens with attempts)
        if (rejectedTokens.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Failures ──");
            foreach (var token in rejectedTokens)
            {
                foreach (var attempt in token.Attempts)
                {
                    var decoratorDesc = !string.IsNullOrEmpty(attempt.DecoratorName) ? attempt.DecoratorName : "decorator";
                    sb.Append("  ✗ ").Append(token.TokenName).Append(": ").Append(attempt.Outcome).Append(" — ").Append(decoratorDesc).Append(" failed on '").Append(attempt.Value).AppendLine("'");
                }

                foreach (var issue in token.Issues)
                {
                    if (issue.Hint != null)
                        sb.Append("      Hint: ").AppendLine(issue.Hint);
                }
            }
        }

        // Unmatched tokens (never found)
        if (neverFoundTokens.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Unmatched Tokens ──");
            foreach (var token in neverFoundTokens)
            {
                sb.Append("  ✗ ").Append(token.TokenName).AppendLine(" — preamble never found");

                foreach (var issue in token.Issues)
                {
                    if (issue.Hint != null)
                        sb.Append("      Hint: ").AppendLine(issue.Hint);
                }
            }
        }

        // Summary
        sb.AppendLine();
        sb.AppendLine("═══ Summary ═══");
        sb.Append("  Matched: ").Append(matchedTokens.Count).Append(" | Missed: ").Append(rejectedTokens.Count + neverFoundTokens.Count).Append(" | Failures: ").Append(rejectedTokens.Sum(t => t.Attempts.Count));

        return sb.ToString();
    }

    private static int CountLines(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        var count = 1;
        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var c in content!)
        {
            if (c == '\n')
                count++;
        }
        return count;
    }
}
