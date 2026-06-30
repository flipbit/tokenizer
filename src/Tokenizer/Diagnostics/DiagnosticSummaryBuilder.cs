using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tokens.Diagnostics.Hints;

namespace Tokens.Diagnostics
{
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
                                            TokenizationDiagnostics diagnostics)
        {
            foreach (var generator in HintGenerators)
            {
                var hint = generator.TryGenerateHint(issue, sourceEvent, diagnostics);
                if (hint != null)
                    return hint;
            }
            return null;
        }

        public static DiagnosticSummary Build(TokenizationDiagnostics diagnostics)
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
                return $"Matched {matched} of {total} tokens.";

            return $"Matched {matched} of {total} tokens ({missed} missed).";
        }

        private static IReadOnlyList<DiagnosticIssue> BuildIssues(List<DiagnosticEvent> events,
                                                                   TokenizationDiagnostics diagnostics)
        {
            var issues = new List<DiagnosticIssue>();

            // Collect token names that have transformer or validator failures
            var tokensWithFailures = new HashSet<string>(
                events
                    .Where(e => e.Type == DiagnosticEventType.TransformerFailed
                             || e.Type == DiagnosticEventType.ValidatorFailed)
                    .Where(e => e.TokenName != null)
                    .Select(e => e.TokenName!));

            // 1. Transformer failures
            foreach (var evt in events.Where(e => e.Type == DiagnosticEventType.TransformerFailed))
            {
                var description = BuildTransformerDescription(evt);
                var partialIssue = new DiagnosticIssue
                {
                    Type = DiagnosticIssueType.TransformerFailure,
                    TokenName = evt.TokenName,
                    Description = description,
                    Location = evt.Location,
                };
                issues.Add(new DiagnosticIssue
                {
                    Type = partialIssue.Type,
                    TokenName = partialIssue.TokenName,
                    Description = partialIssue.Description,
                    Location = partialIssue.Location,
                    Hint = GenerateHint(partialIssue, evt, diagnostics),
                });
            }

            // 2. Validator failures
            foreach (var evt in events.Where(e => e.Type == DiagnosticEventType.ValidatorFailed))
            {
                var description = BuildValidatorDescription(evt);
                var partialIssue = new DiagnosticIssue
                {
                    Type = DiagnosticIssueType.ValidatorRejection,
                    TokenName = evt.TokenName,
                    Description = description,
                    Location = evt.Location,
                };
                issues.Add(new DiagnosticIssue
                {
                    Type = partialIssue.Type,
                    TokenName = partialIssue.TokenName,
                    Description = partialIssue.Description,
                    Location = partialIssue.Location,
                    Hint = GenerateHint(partialIssue, evt, diagnostics),
                });
            }

            // 3. Missed tokens with no prior transformer/validator failure (preamble never found)
            foreach (var evt in events.Where(e => e.Type == DiagnosticEventType.TokenMissed))
            {
                if (evt.TokenName == null || tokensWithFailures.Contains(evt.TokenName))
                    continue;

                var partialIssue = new DiagnosticIssue
                {
                    Type = DiagnosticIssueType.PreambleNeverFound,
                    TokenName = evt.TokenName,
                    Description = $"Token '{evt.TokenName}' was never matched in the input.",
                    Location = evt.Location,
                };
                issues.Add(new DiagnosticIssue
                {
                    Type = partialIssue.Type,
                    TokenName = partialIssue.TokenName,
                    Description = partialIssue.Description,
                    Location = partialIssue.Location,
                    Hint = GenerateHint(partialIssue, evt, diagnostics),
                });
            }

            // 4. Repeating token disabled
            foreach (var evt in events.Where(e => e.Type == DiagnosticEventType.RepeatingTokenDisabled))
            {
                var description = BuildRepeatingTokenDescription(evt);
                var partialIssue = new DiagnosticIssue
                {
                    Type = DiagnosticIssueType.RepeatingTokenCutShort,
                    TokenName = evt.TokenName,
                    Description = description,
                    Location = evt.Location,
                };
                issues.Add(new DiagnosticIssue
                {
                    Type = partialIssue.Type,
                    TokenName = partialIssue.TokenName,
                    Description = partialIssue.Description,
                    Location = partialIssue.Location,
                    Hint = GenerateHint(partialIssue, evt, diagnostics),
                });
            }

            // 5. Hint missing
            foreach (var evt in events.Where(e => e.Type == DiagnosticEventType.HintMissing))
            {
                var description = string.IsNullOrEmpty(evt.Value)
                    ? "A required hint was not found in the input."
                    : $"Required hint not found in input: '{evt.Value}'.";

                var partialIssue = new DiagnosticIssue
                {
                    Type = DiagnosticIssueType.HintMissing,
                    TokenName = evt.TokenName,
                    Description = description,
                    Location = evt.Location,
                };
                issues.Add(new DiagnosticIssue
                {
                    Type = partialIssue.Type,
                    TokenName = partialIssue.TokenName,
                    Description = partialIssue.Description,
                    Location = partialIssue.Location,
                    Hint = GenerateHint(partialIssue, evt, diagnostics),
                });
            }

            return issues;
        }

        private static string BuildTransformerDescription(DiagnosticEvent evt)
        {
            var sb = new StringBuilder();
            sb.Append($"Transformer '{evt.DecoratorName ?? "unknown"}'");

            if (evt.DecoratorArgs != null && evt.DecoratorArgs.Length > 0)
                sb.Append($"({string.Join(", ", evt.DecoratorArgs)})");

            sb.Append($" failed to transform value '{evt.Value ?? "null"}'");

            if (evt.TokenName != null)
                sb.Append($" for token '{evt.TokenName}'");

            sb.Append(".");
            return sb.ToString();
        }

        private static string BuildValidatorDescription(DiagnosticEvent evt)
        {
            var sb = new StringBuilder();
            sb.Append($"Validator '{evt.DecoratorName ?? "unknown"}'");
            sb.Append($" rejected value '{evt.Value ?? "null"}'");

            if (evt.TokenName != null)
                sb.Append($" for token '{evt.TokenName}'");

            sb.Append(".");
            return sb.ToString();
        }

        private static string BuildRepeatingTokenDescription(DiagnosticEvent evt)
        {
            var sb = new StringBuilder();
            sb.Append($"Repeating token '{evt.TokenName ?? "unknown"}' was cut short");

            if (!string.IsNullOrEmpty(evt.Detail))
                sb.Append($": {evt.Detail}");

            sb.Append(".");
            return sb.ToString();
        }
    }
}
