using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Diagnostics;
using Tokens.Enumerators;
using Tokens.Exceptions;

namespace Tokens.Tokenization;

internal static class ArgumentValidation
{
#if NETSTANDARD2_0
    public static void ThrowIfNull(object argument, string paramName)
    {
        if (argument == null) throw new ArgumentNullException(paramName);
    }
#else
    public static void ThrowIfNull(object argument, string paramName)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }
#endif
}

/// <summary>
/// Core tokenization engine that processes input text and matches tokens according to template patterns.
/// This service encapsulates the main tokenization algorithm and handles candidate token processing,
/// input enumeration, and token matching logic.
/// </summary>
internal class TokenizationEngine : ITokenizationEngine
{
    private readonly ILogger<TokenizationEngine> log;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizationEngine"/> class.
    /// </summary>
    public TokenizationEngine() : this(null)
    {
    }

    public TokenizationEngine(ILogger<TokenizationEngine>? logger)
    {
        log = logger ?? NullLogger<TokenizationEngine>.Instance;
    }

    /// <summary>
    /// Processes the main tokenization algorithm, matching tokens from input text
    /// and assigning values to the target object.
    /// </summary>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="targetObject">The object to populate with matched token values</param>
    /// <param name="context">The tokenization context containing shared state (must be initialized by the caller)</param>
    /// <param name="result">The result object to populate with matches and misses</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    /// <param name="hintStrategy">Optional hint strategy to notify when token preambles match.</param>
    public void ProcessTokenization(
        Template template,
        object? targetObject,
        TokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null)
    {
        var continuation = BeginTokenization(template, targetObject, context, result, collector, hintStrategy);
        do
        {
            context.Enumerator.FillBuffer();
        }
        while (!ContinueTokenization(continuation, context, CancellationToken.None));
        EndTokenization(continuation, context);
    }

    /// <summary>
    /// Initializes tokenization state on the context and validates arguments.
    /// This is the setup phase before the main tokenization loop.
    /// </summary>
    public TokenizationContinuation BeginTokenization(
        Template template,
        object? targetObject,
        TokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null)
    {
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(context, nameof(context));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        // Layer 1: Entry Point Validation
        // Validate that targetObject has settable properties if it's not null and not a dictionary
        if (targetObject != null && !(targetObject is System.Collections.Generic.IDictionary<string, object>))
        {
            // Entry-point validation only — runs once per Tokenize call, not in the inner loop.
            // Reflection caching is not warranted here.
            var properties = targetObject.GetType().GetProperties();
            var hasSettableProperty = properties.Any(p => p.CanWrite && p.GetSetMethod() != null);

            // Layer 4: Debug Instrumentation
            // Log target object details for forensics
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.LogDebug("Target object type: {TypeName}, Properties: {PropertyCount}, Settable: {SettableCount}",
                    targetObject.GetType().Name,
                    properties.Length,
                    properties.Count(p => p.CanWrite && p.GetSetMethod() != null));
            }

            if (!hasSettableProperty)
            {
                throw new ArgumentException(
                    $"Target object of type '{targetObject.GetType().Name}' has no settable properties. " +
                    "Anonymous types and objects with read-only properties cannot be used as tokenization targets. " +
                    "Consider using a class with writable properties or passing null as the target.",
                    nameof(targetObject));
            }
        }

        collector.Record(DiagnosticEventType.TokenizationStarted,
            detail: $"Template: {template.Name}, Tokens: {template.Tokens.Count}");

        context.MatchBuffer.Clear();

        return new TokenizationContinuation(
            template, targetObject, result, collector, hintStrategy,
            hasExplicitLimit: template.Options.MaxIterations > 0);
    }

    /// <summary>
    /// Runs the main tokenization loop. Returns true when the input is fully consumed,
    /// or false when the enumerator needs a buffer refill (for cooperative async yielding).
    /// </summary>
    public bool ContinueTokenization(TokenizationContinuation continuation, TokenizationContext context, CancellationToken ct)
    {
        var template = continuation.Template;

        while (context.Enumerator.IsEmpty == false)
        {
            if (context.Enumerator.NeedsRefill)
                return false;

            ct.ThrowIfCancellationRequested();

            continuation.IterationCount++;
            if (continuation.HasExplicitLimit && continuation.IterationCount > template.Options.MaxIterations)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded maximum iteration count of {template.Options.MaxIterations:N0}. " +
                    "This may indicate a problematic template pattern. " +
                    "Increase TokenizerOptions.MaxIterations to allow more iterations.");
            }

            if (!continuation.HasExplicitLimit && continuation.IterationCount > context.Enumerator.CharactersConsumed * 2 + 100)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded derived iteration limit (iterations: {continuation.IterationCount:N0}, " +
                    $"characters consumed: {context.Enumerator.CharactersConsumed:N0}). " +
                    "This may indicate a problematic template pattern. " +
                    "Set TokenizerOptions.MaxIterations to override the automatic limit.");
            }

            var next = context.Enumerator.Peek();

            // Check for repeated current token
            if (ShouldProcessRepeatedToken(context))
            {
                if (!ProcessRepeatedTokens(continuation, context))
                {
                    continue;
                }
            }

            // Assign newline terminated token
            if (ShouldProcessNewlineTerminatedToken(context, next))
            {
                HandleNewlineTerminatedToken(continuation, context);
                continue;
            }

            // Check for next token
            if (context.Enumerator.TryMatch(template.TokensExcluding(context.MatchIds, context.Candidates, context.DisabledRepeatingTokens, context.ExclusionBuffer, context.TokenFilterBuffer, context.TokenFilterIds), template.Options.OutOfOrderTokens, context.MatchBuffer))
            {
                continuation.Collector.Record(DiagnosticEventType.PreambleMatched,
                    tokenName: string.Join(", ", context.MatchBuffer.Select(m => m.Name)),
                    location: context.Enumerator.Location);

                // Notify hint strategy of matched tokens
                if (continuation.HintStrategy != null)
                {
                    foreach (var match in context.MatchBuffer)
                    {
                        continuation.HintStrategy.OnTokenMatched(match);
                    }
                }

                // Special case: first token found, just prepare to read token value
                if (context.Candidates.HasCandidates == false)
                {
                    HandleFirstTokenMatch(context, context.MatchBuffer);
                    continue;
                }

                // Only switch if we've accumulated a value — otherwise consume a character first
                if (context.Replacement.Length > 0)
                {
                    HandleTokenSwitch(continuation, context, context.MatchBuffer);
                }
                else
                {
                    HandleNoTokenMatch(context, next);
                }
            }
            else
            {
                HandleNoTokenMatch(context, next);
            }
        }

        return true;
    }

    /// <summary>
    /// Finalizes tokenization by processing remaining candidates and front matter tokens.
    /// </summary>
    public void EndTokenization(TokenizationContinuation continuation, TokenizationContext context)
    {
        // Handle remaining candidates
        if (ShouldProcessRemainingCandidates(context))
        {
            TryAssignCandidateTokens(continuation, context, context.ReplacementLocation);
        }

        // Process front matter tokens
        ProcessFrontMatterTokens(continuation, context.Enumerator.Location);

        continuation.Collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: $"Matches: {continuation.Result.Tokens.Matches.Count}, Misses: {continuation.Result.Tokens.Misses.Count}");
    }

    /// <summary>
    /// Processes candidate tokens and attempts to assign values to the target object.
    /// </summary>
    /// <param name="continuation">The continuation state from BeginTokenization.</param>
    /// <param name="context">The tokenization context containing candidates and replacement state.</param>
    /// <param name="location">The location where the token was found.</param>
    /// <returns>True if any tokens were successfully assigned</returns>
    private bool TryAssignCandidateTokens(
        TokenizationContinuation continuation,
        TokenizationContext context,
        FileLocation location)
    {
        var replacementValue = context.Replacement.ToString();

        continuation.Collector.Record(DiagnosticEventType.TokenAssignmentAttempted,
            tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
            location: location,
            value: replacementValue);

        try
        {
            if (context.Candidates.TryAssign(continuation.TargetObject, context.Replacement, continuation.Template.Options, location, out var assigned, out var assignedValue, continuation.Collector))
            {
                continuation.Collector.Record(DiagnosticEventType.TokenAssigned,
                    tokenName: assigned.Name, tokenId: assigned.Id,
                    location: location,
                    value: assignedValue?.ToString());

                if (assignedValue != null)
                {
                    continuation.Result.Tokens.AddMatch(assigned, assignedValue, location);
                    AddMatchedTokenIds(continuation.Template, assigned, context.MatchIds);
                }

                return true;
            }
            else
            {
                continuation.Collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                    tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                    location: location,
                    value: replacementValue);

                return false;
            }
        }
        catch (Exception e)
        {
            if (log.IsEnabled(LogLevel.Warning))
            {
                log.LogWarning(e, "Error Assigning Value: {Message}", e.Message);
            }
            continuation.Result.AddException(e);
            return false;
        }
    }

    /// <summary>
    /// Processes front matter tokens that don't require input text matching.
    /// </summary>
    /// <param name="continuation">The continuation state from BeginTokenization.</param>
    /// <param name="location">The current file location.</param>
    private void ProcessFrontMatterTokens(
        TokenizationContinuation continuation,
        FileLocation location)
    {
        foreach (var token in continuation.Template.Tokens)
        {
            if (!token.IsFrontMatterToken) continue;

            if (token.Assign(continuation.TargetObject, string.Empty, continuation.Template.Options, location, out var assignedValue, continuation.Collector))
            {
                continuation.Collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                    tokenName: token.Name, tokenId: token.Id,
                    value: assignedValue?.ToString());
                if (assignedValue != null)
                {
                    continuation.Result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
            }
            else
            {
                continuation.Collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                    tokenName: token.Name, tokenId: token.Id);
            }
        }
    }

    /// <summary>
    /// Handles the processing of repeated tokens and manages disabled repeating tokens.
    /// </summary>
    /// <param name="continuation">The continuation state from BeginTokenization.</param>
    /// <param name="context">The tokenization context containing candidates and replacement state.</param>
    /// <returns>True if processing should continue, false if candidates were cleared</returns>
    private bool ProcessRepeatedTokens(
        TokenizationContinuation continuation,
        TokenizationContext context)
    {
        var replacementValue = context.Replacement.ToString();

        // Can't assign, so clear current context and move to next match
        if (context.Candidates.CanAnyAssign(replacementValue) == false)
        {
            continuation.Collector.Record(DiagnosticEventType.BacktrackStarted,
                tokenName: string.Join(", ", context.Candidates.Tokens.Select(t => t.Name)),
                location: context.Enumerator.Location,
                value: replacementValue);

            // Prevent infinite loop when backtracking with empty preamble
            var advanceLength = context.Candidates.Preamble.Length;
            if (advanceLength == 0 && context.Candidates.Tokens.Count > 0)
            {
                var tokenNames = string.Join(", ", context.Candidates.Tokens.Select(t => t.Name));
                log.LogError(
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
                // If repeated token was the last match, then this non-match will stop it
                // matching any future results
                var token = context.Candidates.Tokens[i];
                if (WasLastMatchedToken(continuation.Result, token) && string.IsNullOrWhiteSpace(token.Preamble) && string.IsNullOrWhiteSpace(replacementValue))
                {
                    continuation.Collector.Record(DiagnosticEventType.RepeatingTokenDisabled,
                        tokenName: token.Name, tokenId: token.Id,
                        location: context.Enumerator.Location);
                    context.DisabledRepeatingTokens.Add(token.Id);
                    context.Candidates.Remove(token);
                    i--;
                }
                else if (token.IsSingleUse)
                {
                    continuation.Collector.Record(DiagnosticEventType.SingleUseTokenRemoved,
                        tokenName: token.Name, tokenId: token.Id,
                        location: context.Enumerator.Location);
                    context.Candidates.Remove(token);
                    continuation.Result.Tokens.AddMiss(token);
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
    /// Handles newline-terminated token processing.
    /// </summary>
    /// <param name="continuation">The continuation state from BeginTokenization.</param>
    /// <param name="context">The tokenization context containing candidates and replacement state.</param>
    /// <param name="location">The location where the newline was found.</param>
    private void ProcessNewlineTerminatedTokens(
        TokenizationContinuation continuation,
        TokenizationContext context,
        FileLocation location)
    {
        var replacementValue = context.Replacement.ToString();
        var firstToken = context.Candidates.Tokens[0];

        continuation.Collector.Record(DiagnosticEventType.NewlineTerminatedTokenProcessed,
            tokenName: firstToken.Name,
            tokenId: firstToken.Id,
            value: replacementValue,
            location: location);

        if (firstToken.IsRepeating &&
            string.IsNullOrWhiteSpace(context.Candidates.Preamble) &&
            continuation.Result.Tokens.HasMatches)
        {
            var matches = continuation.Result.Tokens.Matches;
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

        TryAssignCandidateTokens(continuation, context, location);
    }

    /// <summary>
    /// Adds matched token IDs to the tracking set for template ordering logic.
    /// </summary>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="matchedToken">The token that was matched</param>
    /// <param name="matchIds">The set of matched token IDs to update</param>
    private void AddMatchedTokenIds(Template template, Token matchedToken, HashSet<int> matchIds)
    {
        template.GetTokenIdsUpTo(matchedToken, matchIds);
    }

    /// <summary>
    /// Checks if a token was the last matched token in the result.
    /// </summary>
    /// <param name="result">The result object to check</param>
    /// <param name="token">The token to check</param>
    /// <returns>True if the token was the last matched token</returns>
    private bool WasLastMatchedToken(TokenizeResultBase result, Token token)
    {
        var lastMatch = result.Tokens.Matches.LastOrDefault();

        if (lastMatch != null)
        {
            return lastMatch.Token.Id == token.Id;
        }

        return false;
    }


    /// <summary>
    /// Determines if a repeated token should be processed.
    /// </summary>
    private bool ShouldProcessRepeatedToken(ITokenizationContext context)
    {
        return context.Candidates.HasCandidates &&
               context.Enumerator.TryMatch(context.Candidates.Preamble) &&
               context.Candidates.Preamble.Length > 0;
    }

    /// <summary>
    /// Determines if a newline-terminated token should be processed.
    /// </summary>
    private bool ShouldProcessNewlineTerminatedToken(ITokenizationContext context, char next)
    {
        return context.Candidates.HasCandidates && context.Candidates.TerminateOnNewLine && next == '\n';
    }

    /// <summary>
    /// Determines if remaining candidates should be processed after the main loop.
    /// </summary>
    private bool ShouldProcessRemainingCandidates(ITokenizationContext context)
    {
        return context.Candidates.HasCandidates && context.Replacement.Length > 0 && !context.Candidates.IsNullToken;
    }

    /// <summary>
    /// Handles processing of newline-terminated tokens.
    /// </summary>
    private void HandleNewlineTerminatedToken(TokenizationContinuation continuation, TokenizationContext context)
    {
        ProcessNewlineTerminatedTokens(continuation, context, context.Enumerator.Location);

        context.ClearCandidates();
        context.ClearReplacement();
        context.ReplacementLocation = context.Enumerator.Location;
    }

    /// <summary>
    /// Handles the first token match in the input, preparing the context for value collection.
    /// </summary>
    /// <param name="context">The tokenization context</param>
    /// <param name="matches">The matched tokens</param>
    private void HandleFirstTokenMatch(ITokenizationContext context, IList<Token> matches)
    {
        context.Candidates.AddRange(matches);
        context.ClearReplacement();
        context.Enumerator.Advance(context.Candidates.Preamble.Length);
    }

    /// <summary>
    /// Handles switching from one token to another, assigning the previous token's value
    /// and preparing to collect the new token's value.
    /// </summary>
    /// <param name="continuation">The continuation state from BeginTokenization.</param>
    /// <param name="context">The tokenization context.</param>
    /// <param name="matches">The newly matched tokens.</param>
    private void HandleTokenSwitch(TokenizationContinuation continuation, TokenizationContext context, IList<Token> matches)
    {
        TryAssignCandidateTokens(continuation, context, context.ReplacementLocation);

        context.ClearCandidates();
        context.Candidates.AddRange(matches);
        context.ClearReplacement();
        context.Enumerator.Advance(context.Candidates.Preamble.Length);
        context.ReplacementLocation = context.Enumerator.Location;
    }

    /// <summary>
    /// Handles characters that don't match any token preamble, accumulating them in the replacement buffer.
    /// </summary>
    /// <param name="context">The tokenization context</param>
    /// <param name="next">The current character to append</param>
    private void HandleNoTokenMatch(ITokenizationContext context, char next)
    {
        context.Replacement.Append(next);
        context.Enumerator.Next();
    }
}
