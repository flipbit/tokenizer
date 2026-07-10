using System.Text;
using Tokens.Diagnostics.Hints;
using Tokens.Extensions;

namespace Tokens.Diagnostics;

internal static class DiagnosticSummaryBuilder
{
    private static readonly IHintGenerator[] HintGenerators =
    {
        new DateFormatHintGenerator(),
        new PreambleNearMissHintGenerator(),
        new ValidatorValueHintGenerator(),
        new UnmatchedInputHintGenerator(),
        new RepeatingTokenHintGenerator(),
    };

    private static string? GenerateHint(DiagnosticIssue issue, DiagnosticEvent sourceEvent,
                                        DiagnosticResult diagnostics)
    {
        foreach (var generator in HintGenerators)
        {
            var hint = generator.TryGenerateHint(issue, sourceEvent, diagnostics);
            if (hint != null)
                return hint;
        }
        return null;
    }

    public static DiagnosticSummary Build(DiagnosticResult diagnostics)
    {
        var events = diagnostics.Events;

        var matchedCount = events.Count(e => e.Type == DiagnosticEventType.TokenAssigned);
        var missedCount = events.Count(e => e.Type == DiagnosticEventType.TokenMissed);
        var totalCount = matchedCount + missedCount;

        var verdict = BuildVerdict(matchedCount, totalCount, missedCount);
        var issues = BuildIssues(events, diagnostics);

        return new DiagnosticSummary
        {
            Verdict = verdict,
            Issues = issues,
        };
    }

    private static string BuildVerdict(int matched, int total, int missed)
    {
        if (missed == 0)
            return $"Matched {matched.ToInvariant()} of {total.ToInvariant()} tokens.";

        return $"Matched {matched.ToInvariant()} of {total.ToInvariant()} tokens ({missed.ToInvariant()} missed).";
    }

    private static DiagnosticIssue CreateIssue(DiagnosticIssueType type, DiagnosticEvent sourceEvent,
                                                string description, DiagnosticResult diagnostics)
    {
        var issue = new DiagnosticIssue
        {
            Type = type,
            TokenName = sourceEvent.TokenName,
            Description = description,
            Location = sourceEvent.Location,
        };
        return new DiagnosticIssue
        {
            Type = issue.Type,
            TokenName = issue.TokenName,
            Description = issue.Description,
            Location = issue.Location,
            Hint = GenerateHint(issue, sourceEvent, diagnostics),
        };
    }

    private static IReadOnlyList<DiagnosticIssue> BuildIssues(IReadOnlyList<DiagnosticEvent> events,
                                                               DiagnosticResult diagnostics)
    {
        var issues = new List<DiagnosticIssue>();

        // Collect token names that have transformer or validator failures
        var tokensWithFailures = new HashSet<string>(
            events
                .Where(e => (e.Type == DiagnosticEventType.TransformerFailed
                          || e.Type == DiagnosticEventType.ValidatorFailed)
                         && e.TokenName != null)
                .Select(e => e.TokenName!),
            StringComparer.Ordinal);

        foreach (var evt in events)
        {
            switch (evt.Type)
            {
                case DiagnosticEventType.TransformerFailed:
                    issues.Add(CreateIssue(DiagnosticIssueType.TransformerFailure, evt,
                        BuildTransformerDescription(evt), diagnostics));
                    break;

                case DiagnosticEventType.ValidatorFailed:
                    issues.Add(CreateIssue(DiagnosticIssueType.ValidatorRejection, evt,
                        BuildValidatorDescription(evt), diagnostics));
                    break;

                case DiagnosticEventType.TokenMissed:
                    if (evt.TokenName != null && !tokensWithFailures.Contains(evt.TokenName))
                    {
                        issues.Add(CreateIssue(DiagnosticIssueType.PreambleNeverFound, evt,
                            $"Token '{evt.TokenName}' was never matched in the input.", diagnostics));
                    }
                    break;

                case DiagnosticEventType.RepeatingTokenDisabled:
                    issues.Add(CreateIssue(DiagnosticIssueType.RepeatingTokenCutShort, evt,
                        BuildRepeatingTokenDescription(evt), diagnostics));
                    break;

                case DiagnosticEventType.HintMissing:
                    issues.Add(CreateIssue(DiagnosticIssueType.HintMissing, evt,
                        string.IsNullOrEmpty(evt.Value)
                            ? "A required hint was not found in the input."
                            : $"Required hint not found in input: '{evt.Value}'.",
                        diagnostics));
                    break;
            }
        }

        return issues;
    }

    private static string BuildTransformerDescription(DiagnosticEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append("Transformer '").Append(evt.DecoratorName ?? "unknown").Append('\'');

        if (evt.DecoratorArgs != null && evt.DecoratorArgs.Length > 0)
#if NETSTANDARD2_0
            sb.Append('(').Append(string.Join(", ", evt.DecoratorArgs)).Append(')');
#else
            sb.Append('(').AppendJoin(", ", evt.DecoratorArgs).Append(')');
#endif

        sb.Append(" failed to transform value '").Append(evt.Value ?? "null").Append('\'');

        if (evt.TokenName != null)
            sb.Append(" for token '").Append(evt.TokenName).Append('\'');

        sb.Append('.');
        return sb.ToString();
    }

    private static string BuildValidatorDescription(DiagnosticEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append("Validator '").Append(evt.DecoratorName ?? "unknown").Append('\'');
        sb.Append(" rejected value '").Append(evt.Value ?? "null").Append('\'');

        if (evt.TokenName != null)
            sb.Append(" for token '").Append(evt.TokenName).Append('\'');

        sb.Append('.');
        return sb.ToString();
    }

    private static string BuildRepeatingTokenDescription(DiagnosticEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append("Repeating token '").Append(evt.TokenName ?? "unknown").Append("' was cut short");

        if (!string.IsNullOrEmpty(evt.Detail))
            sb.Append(": ").Append(evt.Detail);

        sb.Append('.');
        return sb.ToString();
    }
}
