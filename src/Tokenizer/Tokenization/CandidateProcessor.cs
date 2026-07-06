using System.Linq;
using Microsoft.Extensions.Logging;
using Tokens.Diagnostics;
using Tokens.Enumerators;

namespace Tokens.Tokenization;

/// <summary>
/// Handles token candidate assignment, backtracking, and newline-terminated token processing.
/// Constructed once per tokenization session with session-scoped dependencies.
/// </summary>
internal sealed class CandidateProcessor
{
    private readonly object? targetObject;
    private readonly TokenizeResultBase result;
    private readonly Template template;
    private readonly IDiagnosticCollector collector;
    private readonly ILogger logger;

    public CandidateProcessor(
        object? targetObject,
        TokenizeResultBase result,
        Template template,
        IDiagnosticCollector collector,
        ILogger logger)
    {
        this.targetObject = targetObject;
        this.result = result;
        this.template = template;
        this.collector = collector;
        this.logger = logger;
    }

    /// <summary>
    /// Attempts to assign the accumulated replacement value to a candidate token.
    /// Returns true if assignment succeeded.
    /// </summary>
    public bool TryAssign(TokenizationContext context, FileLocation location)
    {
        if (collector.IsEnabled)
        {
            collector.Record(DiagnosticEventType.TokenAssignmentAttempted,
                tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                location: location,
                value: context.Replacement.ToString());
        }

        try
        {
            if (context.Candidates.TryAssign(targetObject, context.Replacement, template.Options, location, out var assigned, out var assignedValue, collector))
            {
                if (collector.IsEnabled)
                {
                    collector.Record(DiagnosticEventType.TokenAssigned,
                        tokenName: assigned.Name, tokenId: assigned.Id,
                        location: location,
                        value: assignedValue?.ToString());
                }

                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(assigned, assignedValue, location);
                    AddMatchedTokenIds(assigned, context.MatchIds);
                }

                return true;
            }
            else
            {
                if (collector.IsEnabled)
                {
                    collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                        tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                        location: location,
                        value: context.Replacement.ToString());
                }

                return false;
            }
        }
        catch (Exception e)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
            }
            result.AddException(e);
            return false;
        }
    }

    /// <summary>
    /// Handles repeated token backtracking when the accumulated value cannot be assigned.
    /// Returns true if the outer loop should continue processing, false if candidates were cleared.
    /// </summary>
    public bool HandleRepeat(TokenizationContext context)
    {
        var replacementValue = context.Replacement.ToString();

        if (context.Candidates.CanAnyAssign(replacementValue) == false)
        {
            if (collector.IsEnabled)
            {
                collector.Record(DiagnosticEventType.BacktrackStarted,
                    tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                    location: context.Enumerator.Location,
                    value: replacementValue);
            }

            var advanceLength = context.Candidates.Preamble.Length;
            if (advanceLength == 0 && context.Candidates.Tokens.Count > 0)
            {
                var tokenNames = string.Join(", ", context.Candidates.Tokens.Select(t => t.Name));
                logger.LogError(
                    "Infinite loop detected: Cannot backtrack with empty preamble for tokens [{TokenNames}]. " +
                    "This occurs when consecutive tokens have no separator and assignment fails. " +
                    "Current position: Line {Line}, Column {Column}",
                    tokenNames, context.Enumerator.Location.Line, context.Enumerator.Location.Column);

                throw new InvalidOperationException(
                    $"Tokenization cannot proceed: tokens with empty preambles ({tokenNames}) cannot be " +
                    $"distinguished from each other. Add separators (preambles) between consecutive tokens, " +
                    $"or ensure the target object has writable properties.");
            }

            for (var i = 0; i < context.Candidates.Tokens.Count; i++)
            {
                var token = context.Candidates.Tokens[i];
                if (WasLastMatchedToken(token) && string.IsNullOrWhiteSpace(token.Preamble) && string.IsNullOrWhiteSpace(replacementValue))
                {
                    if (collector.IsEnabled)
                    {
                        collector.Record(DiagnosticEventType.RepeatingTokenDisabled,
                            tokenName: token.Name, tokenId: token.Id,
                            location: context.Enumerator.Location);
                    }
                    context.DisabledRepeatingTokens.Add(token.Id);
                    context.Candidates.Remove(token);
                    i--;
                }
                else if (token.IsSingleUse)
                {
                    if (collector.IsEnabled)
                    {
                        collector.Record(DiagnosticEventType.SingleUseTokenRemoved,
                            tokenName: token.Name, tokenId: token.Id,
                            location: context.Enumerator.Location);
                    }
                    context.Candidates.Remove(token);
                    result.Tokens.AddMiss(token);
                    context.MatchIds.Add(token.Id);
                }
            }

            context.Replacement.Clear();
            context.Enumerator.Advance(advanceLength);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles newline-terminated token processing: assigns the current value and
    /// optionally disables repeating tokens that span non-adjacent lines.
    /// Clears candidates and replacement after processing.
    /// </summary>
    public void HandleNewline(TokenizationContext context)
    {
        var location = context.Enumerator.Location;
        var firstToken = context.Candidates.Tokens[0];

        if (collector.IsEnabled)
        {
            collector.Record(DiagnosticEventType.NewlineTerminatedTokenProcessed,
                tokenName: firstToken.Name,
                tokenId: firstToken.Id,
                value: context.Replacement.ToString(),
                location: location);
        }

        if (firstToken.IsRepeating &&
            string.IsNullOrWhiteSpace(context.Candidates.Preamble) &&
            result.Tokens.HasMatches)
        {
            var matches = result.Tokens.Matches;
            var lastMatch = matches[matches.Count - 1];
            if (lastMatch.Token.Id == firstToken.Id)
            {
                if (context.Enumerator.Location.Line > lastMatch.Location.Line + 1)
                {
                    context.DisabledRepeatingTokens.Add(firstToken.Id);
                    context.Candidates.Remove(firstToken);
                }
            }
        }

        TryAssign(context, location);

        context.ClearCandidates();
        context.ClearReplacement();
        context.ReplacementLocation = context.Enumerator.Location;
    }

    /// <summary>
    /// Processes any remaining candidates after the main tokenization loop completes.
    /// </summary>
    public void ProcessRemaining(TokenizationContext context)
    {
        if (context.Candidates.HasCandidates && context.Replacement.Length > 0 && !context.Candidates.IsNullToken)
        {
            TryAssign(context, context.ReplacementLocation);
        }
    }

    private void AddMatchedTokenIds(Token matchedToken, HashSet<int> matchIds)
    {
        template.GetTokenIdsUpTo(matchedToken, matchIds);
    }

    private bool WasLastMatchedToken(Token token)
    {
        var matches = result.Tokens.Matches;
        if (matches.Count == 0)
        {
            return false;
        }

        return matches[matches.Count - 1].Token.Id == token.Id;
    }
}
