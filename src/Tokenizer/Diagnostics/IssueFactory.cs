using Tokens.Diagnostics.Hints;

namespace Tokens.Diagnostics;

/// <summary>
/// Creates <see cref="DiagnosticIssue"/> instances, generating adaptive hints
/// by running the issue and its source event through a chain of hint generators.
/// </summary>
internal sealed class IssueFactory
{
    private readonly IHintGenerator[] _hintGenerators;

    internal IssueFactory(IHintGenerator[] hintGenerators)
    {
        _hintGenerators = hintGenerators;
    }

    /// <summary>
    /// Creates a <see cref="DiagnosticIssue"/> for the given type and source event,
    /// generating a hint from the hint generator chain if possible.
    /// </summary>
    internal DiagnosticIssue Create(DiagnosticIssueType type, TokenizationEvent sourceEvent,
                                    string description, BuildContext context)
    {
        var hint = GenerateHint(type, sourceEvent, context);

        return new DiagnosticIssue
        {
            Type = type,
            TokenName = sourceEvent.TokenName,
            Description = description,
            Location = sourceEvent.Location,
            Hint = hint,
        };
    }

    /// <summary>
    /// Creates a <see cref="DiagnosticIssue"/> of type <see cref="DiagnosticIssueType.ValueMismatch"/>
    /// for a matched token whose assigned value contains the preamble of a missed or rejected token,
    /// suggesting greedy capture consumed more than intended.
    /// </summary>
    internal DiagnosticIssue CreateValueMismatch(string tokenName, string missedTokenName, BuildContext context)
    {
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenAssigned,
            TokenName = tokenName,
            Detail = missedTokenName,
        };

        return Create(
            DiagnosticIssueType.ValueMismatch,
            sourceEvent,
            $"Token '{tokenName}' captured value containing preamble of token '{missedTokenName}'.",
            context);
    }

    /// <summary>
    /// Creates a <see cref="DiagnosticIssue"/> of type <see cref="DiagnosticIssueType.Blocked"/>
    /// for a token that was not searched because a prior required token failed to match.
    /// </summary>
    internal DiagnosticIssue CreateBlocked(string tokenName, string blockerName, BuildContext context)
    {
        var sourceEvent = new TokenizationEvent
        {
            Type = TokenizationEventType.TokenMissed,
            TokenName = tokenName,
            Detail = blockerName,
        };

        return Create(
            DiagnosticIssueType.Blocked,
            sourceEvent,
            $"Token '{tokenName}' was not searched for because '{blockerName}' was not matched.",
            context);
    }

    private string? GenerateHint(DiagnosticIssueType type, TokenizationEvent sourceEvent,
                                  BuildContext context)
    {
        foreach (var generator in _hintGenerators)
        {
            var hint = generator.TryGenerateHint(type, sourceEvent.TokenName, sourceEvent, context);
            if (hint != null)
                return hint;
        }
        return null;
    }
}
