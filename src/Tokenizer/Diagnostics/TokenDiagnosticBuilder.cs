using System.Text;
using Tokens.Diagnostics.Hints;
using Tokens.Extensions;

namespace Tokens.Diagnostics;

internal sealed class TokenDiagnosticBuilder
{
    // Static shared instance — safe because IssueFactory and all IHintGenerator
    // implementations are stateless (no mutable fields).
    private static readonly IssueFactory DefaultIssueFactory = new IssueFactory(new IHintGenerator[]
    {
        new BlockedTokenHintGenerator(),
        new ChainedDecoratorHintGenerator(),
        new DateFormatHintGenerator(),
        new MultipleRejectionHintGenerator(),
        new ValueMismatchHintGenerator(),
        new PreambleNearMissHintGenerator(),
        new ValidatorValueHintGenerator(),
        new OptionalTokenHintGenerator(),
        new RepeatingTokenHintGenerator(),
    });

    private readonly TokenizationDiagnostics _diagnostics;
    private readonly IssueFactory _issueFactory;
    private readonly BuildContext _context;

    public TokenDiagnosticBuilder(TokenizationDiagnostics diagnostics, IssueFactory? issueFactory = null)
    {
        _diagnostics = diagnostics;
        _issueFactory = issueFactory ?? DefaultIssueFactory;
        _context = new BuildContext(diagnostics.InputContent, diagnostics.OutOfOrderTokens, diagnostics.OptionalTokenNames);
    }

    /// <summary>
    /// Executes the build pipeline. Phases must run in this order:
    /// 1. CollectEvents — populates context indexes and collects attempts/issues
    /// 2. ClassifyOutcomes — creates TokenDiagnostics from collected data (calls ApplyValueMismatchIssues)
    /// 3. ApplyBlockedAnnotations — reclassifies NeverFound tokens downstream of a blocker
    /// 4. BuildVerdict — generates the human-readable summary string
    /// </summary>
    public (IReadOnlyList<TokenDiagnostic> tokens, string verdict, int matchedCount, int missedCount, int totalCount) Build()
    {
        var collected = CollectEvents();
        var result = ClassifyOutcomes(collected);

        // Causality pass: in ordered mode, non-optional missed tokens block subsequent ones
        if (!_context.OutOfOrderTokens)
        {
            ApplyBlockedAnnotations(result);
        }

        // Handle global hint-missing issues: create a synthetic TokenDiagnostic for them
        if (collected.GlobalIssues.Count > 0)
        {
            result.Add(new TokenDiagnostic
            {
                TokenName = "(global)",
                Outcome = TokenOutcome.NeverFound,
                Issues = collected.GlobalIssues,
            });
        }

        var verdict = BuildVerdict(collected.MatchedCount, collected.MatchedCount + collected.MissedCount, collected.MissedCount);

        return (result, verdict, collected.MatchedCount, collected.MissedCount, collected.MatchedCount + collected.MissedCount);
    }

    private sealed class CollectedEventData
    {
        public Dictionary<string, List<TokenAttempt>> Attempts { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, List<DiagnosticIssue>> Issues { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, (string? value, Enumerators.FileLocation? location)> AssignedTokens { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, int> TokenIds { get; } = new(StringComparer.Ordinal);
        public HashSet<string> TokensWithFailures { get; } = new(StringComparer.Ordinal);
        public HashSet<string> MissedTokenNames { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Maps token name to its preamble text, collected from TokenMissed events.
        /// Used for ValueMismatch detection.
        /// </summary>
        public Dictionary<string, string> PreambleTexts { get; } = new(StringComparer.Ordinal);

        public List<string> TokenOrder { get; } = new();
        public List<DiagnosticIssue> GlobalIssues { get; } = new();
        public int MatchedCount { get; set; }
        public int MissedCount { get; set; }
    }

    private static void AddToIndex(Dictionary<string, List<TokenizationEvent>> index, string tokenName, TokenizationEvent evt)
    {
        if (!index.TryGetValue(tokenName, out var list))
        {
            list = new List<TokenizationEvent>();
            index[tokenName] = list;
        }
        list.Add(evt);
    }

    private CollectedEventData CollectEvents()
    {
        var data = new CollectedEventData();
        var seenTokens = new HashSet<string>(StringComparer.Ordinal);

        foreach (var evt in _diagnostics.RawEvents)
        {
            if (evt.TokenName != null && seenTokens.Add(evt.TokenName))
            {
                data.TokenOrder.Add(evt.TokenName);
            }

            if (evt.TokenName != null && evt.TokenId.HasValue && !data.TokenIds.ContainsKey(evt.TokenName))
            {
                data.TokenIds[evt.TokenName] = evt.TokenId.Value;
            }

            switch (evt.Type)
            {
                case TokenizationEventType.ValidatorFailed:
                    if (evt.TokenName == null)
                        break;
                    AddToIndex(_context.RejectionsPerToken, evt.TokenName, evt);
                    var validatorDescription = BuildValidatorDescription(evt);
                    data.TokensWithFailures.Add(evt.TokenName);
                    AddAttempt(data.Attempts, evt.TokenName, new TokenAttempt
                    {
                        Location = evt.Location,
                        Value = evt.Value,
                        Outcome = AttemptOutcome.ValidatorRejected,
                        DecoratorName = evt.DecoratorName,
                        Reason = validatorDescription,
                    });
                    AddIssue(data.Issues, _issueFactory.Create(DiagnosticIssueType.ValidatorRejection, evt, validatorDescription, _context));
                    break;

                case TokenizationEventType.TransformerFailed:
                    if (evt.TokenName == null)
                        break;
                    AddToIndex(_context.RejectionsPerToken, evt.TokenName, evt);
                    var transformerDescription = BuildTransformerDescription(evt);
                    data.TokensWithFailures.Add(evt.TokenName);
                    AddAttempt(data.Attempts, evt.TokenName, new TokenAttempt
                    {
                        Location = evt.Location,
                        Value = evt.Value,
                        Outcome = AttemptOutcome.TransformerFailed,
                        DecoratorName = evt.DecoratorName,
                        Reason = transformerDescription,
                    });
                    AddIssue(data.Issues, _issueFactory.Create(DiagnosticIssueType.TransformerFailure, evt, transformerDescription, _context));
                    break;

                case TokenizationEventType.ValidatorPassed:
                case TokenizationEventType.TransformerSucceeded:
                    if (evt.TokenName != null)
                        AddToIndex(_context.DecoratorSuccessesPerToken, evt.TokenName, evt);
                    break;

                case TokenizationEventType.TokenAssigned:
                    if (evt.TokenName != null)
                    {
                        if (!data.AssignedTokens.ContainsKey(evt.TokenName))
                        {
                            data.MatchedCount++;
                        }
                        data.AssignedTokens[evt.TokenName] = (evt.Value, evt.Location);
                        AddAttempt(data.Attempts, evt.TokenName, new TokenAttempt
                        {
                            Location = evt.Location,
                            Value = evt.Value,
                            Outcome = AttemptOutcome.Assigned,
                        });
                    }
                    break;

                case TokenizationEventType.BacktrackStarted:
                    if (evt.TokenName != null)
                    {
                        AddAttempt(data.Attempts, evt.TokenName, new TokenAttempt
                        {
                            Location = evt.Location,
                            Value = evt.Value,
                            Outcome = AttemptOutcome.Backtracked,
                        });
                    }
                    break;

                case TokenizationEventType.PreambleMatched:
                    if (evt.TokenName != null && !string.IsNullOrEmpty(evt.Detail)
                        && !data.PreambleTexts.ContainsKey(evt.TokenName))
                    {
                        data.PreambleTexts[evt.TokenName] = evt.Detail!;
                    }
                    break;

                case TokenizationEventType.TokenMissed:
                    if (evt.TokenName != null)
                    {
                        var isFirstMiss = data.MissedTokenNames.Add(evt.TokenName);
                        if (!data.AssignedTokens.ContainsKey(evt.TokenName) && isFirstMiss)
                        {
                            data.MissedCount++;
                        }
                        var preambleDetail = evt.Detail;
                        if (!string.IsNullOrEmpty(preambleDetail) && !data.PreambleTexts.ContainsKey(evt.TokenName))
                            data.PreambleTexts[evt.TokenName] = preambleDetail!;
                        if (isFirstMiss && !data.TokensWithFailures.Contains(evt.TokenName))
                        {
                            AddIssue(data.Issues, _issueFactory.Create(DiagnosticIssueType.PreambleNeverFound, evt,
                                $"Token '{evt.TokenName}' was never matched in the input.", _context));
                        }
                    }
                    break;

                case TokenizationEventType.RepeatingTokenDisabled:
                    if (evt.TokenName != null)
                    {
                        AddIssue(data.Issues, _issueFactory.Create(DiagnosticIssueType.RepeatingTokenCutShort, evt,
                            BuildRepeatingTokenDescription(evt), _context));
                    }
                    break;

                case TokenizationEventType.HintMissing:
                    var hintDesc = string.IsNullOrEmpty(evt.Value)
                        ? "A required hint was not found in the input."
                        : $"Required hint not found in input: '{evt.Value}'.";
                    if (evt.TokenName != null)
                    {
                        AddIssue(data.Issues, _issueFactory.Create(DiagnosticIssueType.HintMissing, evt, hintDesc, _context));
                    }
                    else
                    {
                        data.GlobalIssues.Add(_issueFactory.Create(DiagnosticIssueType.HintMissing, evt, hintDesc, _context));
                    }
                    break;
            }
        }

        return data;
    }

    private List<TokenDiagnostic> ClassifyOutcomes(CollectedEventData data)
    {
        // ValueMismatch detection: before building TokenDiagnostic objects, check if any
        // matched token's value contains the preamble of a missed/rejected token.
        // We add issues to data.Issues BEFORE construction so there's no need to downcast
        // the IReadOnlyList<DiagnosticIssue> after the fact.
        ApplyValueMismatchIssues(data);

        var result = new List<TokenDiagnostic>();

        foreach (var tokenName in data.TokenOrder)
        {
            var isAssigned = data.AssignedTokens.ContainsKey(tokenName);
            var hasFailures = data.TokensWithFailures.Contains(tokenName);
            var isMissed = data.MissedTokenNames.Contains(tokenName);

            TokenOutcome outcome;
            if (isAssigned)
                outcome = TokenOutcome.Matched;
            else if (hasFailures)
                outcome = TokenOutcome.Rejected;
            else if (isMissed)
                outcome = TokenOutcome.NeverFound;
            else
                continue; // Mentioned in events but has no terminal state

            var tokenAttempts = data.Attempts.TryGetValue(tokenName, out var a) ? a : new List<TokenAttempt>();
            var tokenIssues = data.Issues.TryGetValue(tokenName, out var i) ? i : new List<DiagnosticIssue>();
            var assigned = isAssigned ? data.AssignedTokens[tokenName] : default;
            data.TokenIds.TryGetValue(tokenName, out var tokenId);

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

        return result;
    }

    /// <summary>
    /// Detects tokens whose assigned value contains the preamble of a missed/rejected token.
    /// Complexity: O(matched × missed × value_length). Bounded by template token count
    /// (typically &lt;50) and short preamble/value strings. Acceptable at current scale.
    /// </summary>
    private void ApplyValueMismatchIssues(CollectedEventData data)
    {
        if (data.PreambleTexts.Count == 0)
            return;

        var missedOrRejected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in data.MissedTokenNames)
            missedOrRejected.Add(name);
        foreach (var name in data.TokensWithFailures)
        {
            if (!data.AssignedTokens.ContainsKey(name))
                missedOrRejected.Add(name);
        }

        if (missedOrRejected.Count == 0)
            return;

        foreach (var tokenName in data.TokenOrder)
        {
            if (!data.AssignedTokens.ContainsKey(tokenName))
                continue;

            var assignedValue = data.AssignedTokens[tokenName].value;
            if (string.IsNullOrEmpty(assignedValue))
                continue;

            foreach (var missedName in missedOrRejected)
            {
                if (!data.PreambleTexts.TryGetValue(missedName, out var preamble))
                    continue;
                if (string.IsNullOrEmpty(preamble))
                    continue;

                if (assignedValue!.IndexOf(preamble, StringComparison.Ordinal) >= 0)
                {
                    if (!data.Issues.TryGetValue(tokenName, out var issues))
                    {
                        issues = new List<DiagnosticIssue>();
                        data.Issues[tokenName] = issues;
                    }
                    issues.Add(_issueFactory.CreateValueMismatch(tokenName, missedName, _context));
                    break;
                }
            }
        }
    }

    private void ApplyBlockedAnnotations(List<TokenDiagnostic> tokens)
    {
        // Find the first non-optional, non-matched token — that's the blocker.
        // All subsequent NeverFound tokens are blocked by it.
        string? blockerName = null;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (token.Outcome == TokenOutcome.Matched)
                continue;

            if (blockerName == null)
            {
                // This is a failed token. Is it non-optional (i.e. a blocker)?
                if (!_context.OptionalTokenNames.Contains(token.TokenName))
                {
                    blockerName = token.TokenName;
                }
                continue;
            }

            // We have a blocker — mark subsequent NeverFound tokens as Blocked
            // Only NeverFound tokens are reclassified as Blocked. Rejected tokens were
            // actively attempted and carry their own diagnostic value (validator feedback, hints).
            if (token.Outcome == TokenOutcome.NeverFound)
            {
                tokens[i] = token with
                {
                    Outcome = TokenOutcome.Blocked,
                    BlockedBy = blockerName,
                    Issues = new List<DiagnosticIssue>(token.Issues)
                    {
                        _issueFactory.CreateBlocked(token.TokenName, blockerName, _context),
                    },
                };
            }
        }
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

    private static void AddIssue(Dictionary<string, List<DiagnosticIssue>> issues, DiagnosticIssue issue)
    {
        if (issue.TokenName == null)
            return;

        var tokenName = issue.TokenName;
        if (!issues.TryGetValue(tokenName, out var list))
        {
            list = new List<DiagnosticIssue>();
            issues[tokenName] = list;
        }
        list.Add(issue);
    }

    private static string BuildVerdict(int matched, int total, int missed)
    {
        if (missed == 0)
            return $"Matched {matched.ToInvariant()} of {total.ToInvariant()} tokens.";

        return $"Matched {matched.ToInvariant()} of {total.ToInvariant()} tokens ({missed.ToInvariant()} missed).";
    }

    private static string BuildTransformerDescription(TokenizationEvent evt)
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

    private static string BuildValidatorDescription(TokenizationEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append("Validator '").Append(evt.DecoratorName ?? "unknown").Append('\'');
        sb.Append(" rejected value '").Append(evt.Value ?? "null").Append('\'');

        if (evt.TokenName != null)
            sb.Append(" for token '").Append(evt.TokenName).Append('\'');

        sb.Append('.');
        return sb.ToString();
    }

    private static string BuildRepeatingTokenDescription(TokenizationEvent evt)
    {
        var sb = new StringBuilder();
        sb.Append("Repeating token '").Append(evt.TokenName ?? "unknown").Append("' was cut short");

        if (!string.IsNullOrEmpty(evt.Detail))
            sb.Append(": ").Append(evt.Detail);

        sb.Append('.');
        return sb.ToString();
    }
}
