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
    internal DiagnosticIssue Create(DiagnosticIssueType type, DiagnosticEvent sourceEvent,
                                    string description, DiagnosticResult diagnostics)
    {
        var hint = GenerateHint(type, sourceEvent, diagnostics);

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
    internal DiagnosticIssue CreateValueMismatch(string tokenName, string missedTokenName, DiagnosticResult diagnostics)
    {
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenAssigned,
            TokenName = tokenName,
            Detail = missedTokenName,
        };

        return Create(
            DiagnosticIssueType.ValueMismatch,
            sourceEvent,
            $"Token '{tokenName}' captured value containing preamble of token '{missedTokenName}'.",
            diagnostics);
    }

    /// <summary>
    /// Creates a <see cref="DiagnosticIssue"/> of type <see cref="DiagnosticIssueType.Blocked"/>
    /// for a token that was not searched because a prior required token failed to match.
    /// </summary>
    internal DiagnosticIssue CreateBlocked(string tokenName, string blockerName, DiagnosticResult diagnostics)
    {
        var sourceEvent = new DiagnosticEvent
        {
            Type = DiagnosticEventType.TokenMissed,
            TokenName = tokenName,
            Detail = blockerName,
        };

        return Create(
            DiagnosticIssueType.Blocked,
            sourceEvent,
            $"Token '{tokenName}' was not searched for because '{blockerName}' was not matched.",
            diagnostics);
    }

    private string? GenerateHint(DiagnosticIssueType type, DiagnosticEvent sourceEvent,
                                  DiagnosticResult diagnostics)
    {
        foreach (var generator in _hintGenerators)
        {
            var hint = generator.TryGenerateHint(type, sourceEvent.TokenName, sourceEvent, diagnostics);
            if (hint != null)
                return hint;
        }
        return null;
    }
}
