using System.Text;
using Tokens.Diagnostics.Hints;
using Tokens.Extensions;

namespace Tokens.Diagnostics;

internal static class TokenDiagnosticBuilder
{
    private static readonly IHintGenerator[] HintGenerators =
    {
        new DateFormatHintGenerator(),
        new PreambleNearMissHintGenerator(),
        new ValidatorValueHintGenerator(),
        new UnmatchedInputHintGenerator(),
        new RepeatingTokenHintGenerator(),
    };

    public static (IReadOnlyList<TokenDiagnostic> tokens, string verdict) Build(DiagnosticResult diagnostics)
    {
        var events = diagnostics.RawEvents;
        var attempts = new Dictionary<string, List<TokenAttempt>>(StringComparer.Ordinal);
        var issues = new Dictionary<string, List<DiagnosticIssue>>(StringComparer.Ordinal);
        var assignedTokens = new Dictionary<string, (string? value, Enumerators.FileLocation? location)>(StringComparer.Ordinal);
        var tokenIds = new Dictionary<string, int>(StringComparer.Ordinal);

        // Collect token names that have transformer or validator failures
        var tokensWithFailures = new HashSet<string>(
            events
                .Where(e => (e.Type == DiagnosticEventType.TransformerFailed
                          || e.Type == DiagnosticEventType.ValidatorFailed)
                         && e.TokenName != null)
                .Select(e => e.TokenName!),
            StringComparer.Ordinal);

        // Track all unique token names in order of first appearance
        var tokenOrder = new List<string>();
        var seenTokens = new HashSet<string>(StringComparer.Ordinal);

        // Global issues (e.g. HintMissing without a token name)
        var globalIssues = new List<DiagnosticIssue>();

        foreach (var evt in events)
        {
            if (evt.TokenName != null && seenTokens.Add(evt.TokenName))
            {
                tokenOrder.Add(evt.TokenName);
            }

            if (evt.TokenName != null && evt.TokenId.HasValue && !tokenIds.ContainsKey(evt.TokenName))
            {
                tokenIds[evt.TokenName] = evt.TokenId.Value;
            }

            switch (evt.Type)
            {
                case DiagnosticEventType.ValidatorFailed:
                    AddAttempt(attempts, evt.TokenName!, new TokenAttempt
                    {
                        Location = evt.Location,
                        Value = evt.Value,
                        Outcome = AttemptOutcome.ValidatorRejected,
                        DecoratorName = evt.DecoratorName,
                        Reason = BuildValidatorDescription(evt),
                    });
                    AddIssue(issues, evt, DiagnosticIssueType.ValidatorRejection,
                        BuildValidatorDescription(evt), diagnostics);
                    break;

                case DiagnosticEventType.TransformerFailed:
                    AddAttempt(attempts, evt.TokenName!, new TokenAttempt
                    {
                        Location = evt.Location,
                        Value = evt.Value,
                        Outcome = AttemptOutcome.TransformerFailed,
                        DecoratorName = evt.DecoratorName,
                        Reason = BuildTransformerDescription(evt),
                    });
                    AddIssue(issues, evt, DiagnosticIssueType.TransformerFailure,
                        BuildTransformerDescription(evt), diagnostics);
                    break;

                case DiagnosticEventType.TokenAssigned:
                    if (evt.TokenName != null)
                    {
                        assignedTokens[evt.TokenName] = (evt.Value, evt.Location);
                        AddAttempt(attempts, evt.TokenName, new TokenAttempt
                        {
                            Location = evt.Location,
                            Value = evt.Value,
                            Outcome = AttemptOutcome.Assigned,
                        });
                    }
                    break;

                case DiagnosticEventType.BacktrackStarted:
                    if (evt.TokenName != null)
                    {
                        AddAttempt(attempts, evt.TokenName, new TokenAttempt
                        {
                            Location = evt.Location,
                            Value = evt.Value,
                            Outcome = AttemptOutcome.Backtracked,
                        });
                    }
                    break;

                case DiagnosticEventType.TokenMissed:
                    if (evt.TokenName != null && !tokensWithFailures.Contains(evt.TokenName))
                    {
                        AddIssue(issues, evt, DiagnosticIssueType.PreambleNeverFound,
                            $"Token '{evt.TokenName}' was never matched in the input.", diagnostics);
                    }
                    break;

                case DiagnosticEventType.RepeatingTokenDisabled:
                    if (evt.TokenName != null)
                    {
                        AddIssue(issues, evt, DiagnosticIssueType.RepeatingTokenCutShort,
                            BuildRepeatingTokenDescription(evt), diagnostics);
                    }
                    break;

                case DiagnosticEventType.HintMissing:
                    var hintDesc = string.IsNullOrEmpty(evt.Value)
                        ? "A required hint was not found in the input."
                        : $"Required hint not found in input: '{evt.Value}'.";
                    if (evt.TokenName != null)
                    {
                        AddIssue(issues, evt, DiagnosticIssueType.HintMissing, hintDesc, diagnostics);
                    }
                    else
                    {
                        globalIssues.Add(CreateIssue(DiagnosticIssueType.HintMissing, evt, hintDesc, diagnostics));
                    }
                    break;
            }
        }

        // Build TokenDiagnostic list
        var result = new List<TokenDiagnostic>();

        foreach (var tokenName in tokenOrder)
        {
            var isAssigned = assignedTokens.ContainsKey(tokenName);
            var hasFailures = tokensWithFailures.Contains(tokenName);
            var isMissed = events.Any(e => e.Type == DiagnosticEventType.TokenMissed
                && string.Equals(e.TokenName, tokenName, StringComparison.Ordinal));

            TokenOutcome outcome;
            if (isAssigned)
                outcome = TokenOutcome.Matched;
            else if (hasFailures)
                outcome = TokenOutcome.Rejected;
            else if (isMissed)
                outcome = TokenOutcome.NeverFound;
            else
                continue; // Mentioned in events but has no terminal state

            var tokenAttempts = attempts.TryGetValue(tokenName, out var a) ? a : new List<TokenAttempt>();
            var tokenIssues = issues.TryGetValue(tokenName, out var i) ? i : new List<DiagnosticIssue>();
            var assigned = isAssigned ? assignedTokens[tokenName] : default;
            tokenIds.TryGetValue(tokenName, out var tokenId);

            result.Add(new TokenDiagnostic
            {
                TokenName = tokenName,
                TokenId = tokenId,
                Outcome = outcome,
                Attempts = tokenAttempts,
                AssignedValue = assigned.value,
                AssignedLocation = assigned.location,
                Issues = tokenIssues,
            });
        }

        // Handle global hint-missing issues: create a synthetic TokenDiagnostic for them
        if (globalIssues.Count > 0)
        {
            result.Add(new TokenDiagnostic
            {
                TokenName = "(global)",
                Outcome = TokenOutcome.NeverFound,
                Issues = globalIssues,
            });
        }

        // Build verdict
        var matchedCount = events.Count(e => e.Type == DiagnosticEventType.TokenAssigned);
        var missedCount = events.Count(e => e.Type == DiagnosticEventType.TokenMissed);
        var totalCount = matchedCount + missedCount;
        var verdict = BuildVerdict(matchedCount, totalCount, missedCount);

        return (result, verdict);
    }

    private static void AddAttempt(Dictionary<string, List<TokenAttempt>> attempts, string tokenName, TokenAttempt attempt)
    {
        if (!attempts.TryGetValue(tokenName, out var list))
        {
            list = new List<TokenAttempt>();
            attempts[tokenName] = list;
        }
        list.Add(attempt);
    }

    private static void AddIssue(Dictionary<string, List<DiagnosticIssue>> issues, DiagnosticEvent evt,
                                   DiagnosticIssueType type, string description, DiagnosticResult diagnostics)
    {
        var tokenName = evt.TokenName!;
        if (!issues.TryGetValue(tokenName, out var list))
        {
            list = new List<DiagnosticIssue>();
            issues[tokenName] = list;
        }
        list.Add(CreateIssue(type, evt, description, diagnostics));
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

    private static string BuildVerdict(int matched, int total, int missed)
    {
        if (missed == 0)
            return $"Matched {matched.ToInvariant()} of {total.ToInvariant()} tokens.";

        return $"Matched {matched.ToInvariant()} of {total.ToInvariant()} tokens ({missed.ToInvariant()} missed).";
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
