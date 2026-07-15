using System.Text;
using Tokens.Extensions;

namespace Tokens.Diagnostics;

internal static class AlignmentRenderer
{
    public static string Render(TokenizationDiagnostics diagnostics, string? inputContent)
    {
        var sb = new StringBuilder();
        var tokens = diagnostics.Tokens;

        var matchedTokens = new List<TokenDiagnostic>();
        var rejectedTokens = new List<TokenDiagnostic>();
        var neverFoundTokens = new List<TokenDiagnostic>();
        var blockedTokens = new List<TokenDiagnostic>();

        foreach (var token in tokens)
        {
            switch (token.Outcome)
            {
                case TokenOutcome.Matched:
                    matchedTokens.Add(token);
                    break;
                case TokenOutcome.Rejected:
                    rejectedTokens.Add(token);
                    break;
                case TokenOutcome.NeverFound:
                    neverFoundTokens.Add(token);
                    break;
                case TokenOutcome.Blocked:
                    blockedTokens.Add(token);
                    break;
            }
        }

        var inputLineCount = CountLines(inputContent);
        var totalTokens = matchedTokens.Count + rejectedTokens.Count + neverFoundTokens.Count + blockedTokens.Count;

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
                sb.Append("  ✓ ").Append(token.TokenName).Append(" = ");

                if (token.AssignedValues.Count <= 1)
                {
                    // Single value — preserve existing format
                    sb.Append('"').Append(token.AssignedValues.Count == 1 ? token.AssignedValues[0] : string.Empty).Append('"');
                    if (token.AssignedLocations.Count == 1)
                    {
                        sb.Append(" (line ").Append(token.AssignedLocations[0].Line.ToInvariant()).Append(')');
                    }
                }
                else
                {
                    // Multiple values — comma-separated with line range
                    for (var vi = 0; vi < token.AssignedValues.Count; vi++)
                    {
                        if (vi > 0) sb.Append(", ");
                        sb.Append('"').Append(token.AssignedValues[vi]).Append('"');
                    }

                    if (token.AssignedLocations.Count >= 2)
                    {
                        var firstLine = token.AssignedLocations[0].Line;
                        var lastLine = token.AssignedLocations[token.AssignedLocations.Count - 1].Line;
                        if (firstLine > 0 && lastLine > 0)
                        {
                            sb.Append(" (lines ").Append(firstLine.ToInvariant()).Append('–').Append(lastLine.ToInvariant()).Append(')');
                        }
                    }
                }

                sb.AppendLine();
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
                    if (attempt.Outcome != AttemptOutcome.ValidatorRejected &&
                        attempt.Outcome != AttemptOutcome.TransformerFailed)
                        continue;

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

        // Blocked tokens (not searched because a prior non-optional token was missing)
        if (blockedTokens.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("── Blocked Tokens ──");
            foreach (var token in blockedTokens)
            {
                sb.Append("  ⊘ ").Append(token.TokenName).Append(" — blocked by '").Append(token.BlockedBy).AppendLine("'");

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
        sb.Append("  Matched: ").Append(matchedTokens.Count)
            .Append(" | Missed: ").Append(rejectedTokens.Count + neverFoundTokens.Count)
            .Append(" | Blocked: ").Append(blockedTokens.Count)
            .Append(" | Failures: ").Append(CountFailures(rejectedTokens));

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

    private static int CountFailures(List<TokenDiagnostic> tokens)
    {
        var count = 0;
        foreach (var token in tokens)
            foreach (var attempt in token.Attempts)
                if (attempt.Outcome == AttemptOutcome.ValidatorRejected ||
                    attempt.Outcome == AttemptOutcome.TransformerFailed)
                    count++;
        return count;
    }
}
