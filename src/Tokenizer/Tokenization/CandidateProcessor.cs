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
    private readonly TokenizeResultBase _result;
    private readonly Template _template;
    private readonly DecoratorPipeline _pipeline;
    private readonly IDiagnosticCollector _collector;
    private readonly ILogger _logger;

    public CandidateProcessor(
        TokenizeResultBase result,
        Template template,
        DecoratorPipeline pipeline,
        IDiagnosticCollector collector,
        ILogger logger)
    {
        _result = result;
        _template = template;
        _pipeline = pipeline;
        _collector = collector;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to evaluate the accumulated replacement value against candidate tokens.
    /// Returns true if evaluation succeeded and a match was recorded.
    /// </summary>
    public bool TryAssign(TokenizationContext context, FileLocation location)
    {
        if (_collector.IsEnabled)
        {
            _collector.Record(DiagnosticEventType.TokenAssignmentAttempted,
                tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                location: location,
                value: context.Replacement.ToString());
        }

        try
        {
            if (context.Candidates.TryEvaluate(context.Replacement, _pipeline, location, out var evaluated, out var evaluatedValue))
            {
                if (_collector.IsEnabled)
                {
                    _collector.Record(DiagnosticEventType.TokenAssigned,
                        tokenName: evaluated.Name, tokenId: evaluated.Id,
                        location: location,
                        value: evaluatedValue?.ToString());
                }

                if (evaluatedValue != null)
                {
                    _result.Tokens.AddMatch(evaluated, evaluatedValue, location);
                    AddMatchedTokenIds(evaluated, context.MatchIds);
                }

                return true;
            }

            if (_collector.IsEnabled)
            {
                _collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                    tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                    location: location,
                    value: context.Replacement.ToString());
            }

            return false;
        }
        catch (Exception e)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
            }
            _result.AddException(e);
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

        if (!context.Candidates.CanAnyEvaluate(replacementValue, _pipeline))
        {
            if (_collector.IsEnabled)
            {
                _collector.Record(DiagnosticEventType.BacktrackStarted,
                    tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                    location: context.Enumerator.Location,
                    value: replacementValue);
            }

            var advanceLength = context.Candidates.Preamble.Length;
            if (advanceLength == 0 && context.Candidates.Tokens.Count > 0)
            {
                var tokenNames = string.Join(", ", context.Candidates.Tokens.Select(t => t.Name));
                _logger.LogError(
                    "Infinite loop detected: Cannot backtrack with empty preamble for tokens [{TokenNames}]. " +
                    "This occurs when consecutive tokens have no separator and assignment fails. " +
                    "Current position: Line {Line}, Column {Column}",
                    tokenNames, context.Enumerator.Location.Line, context.Enumerator.Location.Column);

                throw new InvalidOperationException(
                    "Tokenization cannot proceed: tokens with empty preambles (" + tokenNames + ") cannot be " +
                    "distinguished from each other. Add separators (preambles) between consecutive tokens, " +
                    "or ensure the target object has writable properties.");
            }

            for (var i = 0; i < context.Candidates.Tokens.Count; i++)
            {
                var token = context.Candidates.Tokens[i];
                if (WasLastMatchedToken(token) && string.IsNullOrWhiteSpace(token.Preamble) && string.IsNullOrWhiteSpace(replacementValue))
                {
                    if (_collector.IsEnabled)
                    {
                        _collector.Record(DiagnosticEventType.RepeatingTokenDisabled,
                            tokenName: token.Name, tokenId: token.Id,
                            location: context.Enumerator.Location);
                    }
                    context.DisabledRepeatingTokens.Add(token.Id);
                    context.Candidates.Remove(token);
                    i--;
                }
                else if (token.IsSingleUse)
                {
                    if (_collector.IsEnabled)
                    {
                        _collector.Record(DiagnosticEventType.SingleUseTokenRemoved,
                            tokenName: token.Name, tokenId: token.Id,
                            location: context.Enumerator.Location);
                    }
                    context.Candidates.Remove(token);
                    _result.Tokens.AddMiss(token);
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
    /// Handles newline-terminated token processing: optionally disables repeating tokens
    /// that span non-adjacent lines, attempts assignment, then clears candidates,
    /// replacement, and updates the replacement location.
    /// </summary>
    public void HandleNewline(TokenizationContext context)
    {
        var location = context.Enumerator.Location;
        var firstToken = context.Candidates.Tokens[0];

        if (_collector.IsEnabled)
        {
            _collector.Record(DiagnosticEventType.NewlineTerminatedTokenProcessed,
                tokenName: firstToken.Name,
                tokenId: firstToken.Id,
                value: context.Replacement.ToString(),
                location: location);
        }

        if (firstToken.IsRepeating &&
            string.IsNullOrWhiteSpace(context.Candidates.Preamble) &&
            _result.Tokens.HasMatches)
        {
            var matches = _result.Tokens.Matches;
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
        _template.GetTokenIdsUpTo(matchedToken, matchIds);
    }

    private bool WasLastMatchedToken(Token token)
    {
        var matches = _result.Tokens.Matches;
        if (matches.Count == 0)
        {
            return false;
        }

        return matches[matches.Count - 1].Token.Id == token.Id;
    }
}
