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
        ITokenizationContext context,
        TokenizeResultBase result,
        IDiagnosticCollector collector,
        IHintStrategy? hintStrategy = null)
    {
        var ctx = (TokenizationContext)context;

        BeginTokenization(template, targetObject, ctx, result, collector, hintStrategy);
        do
        {
            ctx.Enumerator.FillBuffer();
        }
        while (!ContinueTokenization(ctx, CancellationToken.None));
        EndTokenization(ctx);
    }

    /// <summary>
    /// Initializes tokenization state on the context and validates arguments.
    /// This is the setup phase before the main tokenization loop.
    /// </summary>
    public void BeginTokenization(
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

        // Store state on context for Continue/End phases
        context.Template = template;
        context.TargetObject = targetObject;
        context.Result = result;
        context.Collector = collector;
        context.HintStrategy = hintStrategy;
        context.HasExplicitLimit = template.Options.MaxIterations > 0;
        context.IterationCount = 0;
        context.MatchBuffer.Clear();
    }

    /// <summary>
    /// Runs the main tokenization loop. Returns true when the input is fully consumed,
    /// or false when the enumerator needs a buffer refill (for cooperative async yielding).
    /// </summary>
    public bool ContinueTokenization(TokenizationContext context, CancellationToken ct)
    {
        var template = context.Template!;
        var targetObject = context.TargetObject;
        var result = context.Result!;
        var collector = context.Collector!;
        var hintStrategy = context.HintStrategy;

        while (context.Enumerator.IsEmpty == false)
        {
            if (context.Enumerator.NeedsRefill)
                return false;

            ct.ThrowIfCancellationRequested();

            context.IterationCount++;
            if (context.HasExplicitLimit && context.IterationCount > template.Options.MaxIterations)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded maximum iteration count of {template.Options.MaxIterations:N0}. " +
                    "This may indicate a problematic template pattern. " +
                    "Increase TokenizerOptions.MaxIterations to allow more iterations.");
            }

            if (!context.HasExplicitLimit && context.IterationCount > context.Enumerator.CharactersConsumed * 2 + 100)
            {
                throw new TokenizerException(
                    $"Tokenization exceeded derived iteration limit (iterations: {context.IterationCount:N0}, " +
                    $"characters consumed: {context.Enumerator.CharactersConsumed:N0}). " +
                    "This may indicate a problematic template pattern. " +
                    "Set TokenizerOptions.MaxIterations to override the automatic limit.");
            }

            var next = context.Enumerator.Peek();

            // Check for repeated current token
            if (ShouldProcessRepeatedToken(context))
            {
                if (!HandleRepeatedTokenMatching(context, template, result, targetObject, collector))
                {
                    continue;
                }
            }

            // Assign newline terminated token
            if (ShouldProcessNewlineTerminatedToken(context, next))
            {
                HandleNewlineTerminatedToken(context, template, targetObject, result, collector);
                continue;
            }

            // Check for next token
            if (context.Enumerator.TryMatch(template.TokensExcluding(context.MatchIds, context.Candidates, context.DisabledRepeatingTokens, context.ExclusionBuffer, context.TokenFilterBuffer, context.TokenFilterIds), template.Options.OutOfOrderTokens, context.MatchBuffer))
            {
                collector.Record(DiagnosticEventType.PreambleMatched,
                    tokenName: string.Join(", ", context.MatchBuffer.Select(m => m.Name)),
                    location: context.Enumerator.Location);

                // Notify hint strategy of matched tokens
                if (hintStrategy != null)
                {
                    foreach (var match in context.MatchBuffer)
                    {
                        hintStrategy.OnTokenMatched(match);
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
                    HandleTokenSwitch(context, template, targetObject, result, context.MatchBuffer, collector);
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
    public void EndTokenization(TokenizationContext context)
    {
        var template = context.Template!;
        var targetObject = context.TargetObject;
        var result = context.Result!;
        var collector = context.Collector!;

        // Handle remaining candidates
        if (ShouldProcessRemainingCandidates(context))
        {
            TryAssignCandidateTokens(context.Candidates, targetObject, context.Replacement,
                template.Options, context.ReplacementLocation, result, template, context.MatchIds, collector);
        }

        // Process front matter tokens
        ProcessFrontMatterTokens(template, targetObject, context.Enumerator.Location, result, collector);

        collector.Record(DiagnosticEventType.TokenizationCompleted,
            detail: $"Matches: {result.Tokens.Matches.Count}, Misses: {result.Tokens.Misses.Count}");
    }

    /// <summary>
    /// Processes candidate tokens and attempts to assign values to the target object.
    /// </summary>
    /// <param name="candidates">The list of candidate tokens to process</param>
    /// <param name="targetObject">The object to populate with matched token values</param>
    /// <param name="replacement">The StringBuilder containing the token value</param>
    /// <param name="options">The tokenizer options</param>
    /// <param name="location">The location where the token was found</param>
    /// <param name="result">The result object to populate with matches</param>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="matchIds">The set of matched token IDs</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    /// <returns>True if any tokens were successfully assigned</returns>
    private bool TryAssignCandidateTokens(
        CandidateTokenList candidates,
        object? targetObject,
        StringBuilder replacement,
        TokenizerOptions options,
        FileLocation location,
        TokenizeResultBase result,
        Template template,
        HashSet<int> matchIds,
        IDiagnosticCollector collector)
    {
        ArgumentValidation.ThrowIfNull(candidates, nameof(candidates));
        // Note: targetObject can be null - this is a valid use case for Tokenize(Template, string)
        ArgumentValidation.ThrowIfNull(replacement, nameof(replacement));
        ArgumentValidation.ThrowIfNull(options, nameof(options));
        ArgumentValidation.ThrowIfNull(location, nameof(location));
        ArgumentValidation.ThrowIfNull(result, nameof(result));
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(matchIds, nameof(matchIds));

        var replacementValue = replacement.ToString();

        collector.Record(DiagnosticEventType.TokenAssignmentAttempted,
            tokenName: string.Join(", ", candidates.Tokens.Select(t => t.Name)),
            location: location,
            value: replacementValue);

        try
        {
            if (candidates.TryAssign(targetObject, replacement, options, location, out var assigned, out var assignedValue, collector))
            {
                collector.Record(DiagnosticEventType.TokenAssigned,
                    tokenName: assigned.Name, tokenId: assigned.Id,
                    location: location,
                    value: assignedValue?.ToString());

                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(assigned, assignedValue, location);
                    AddMatchedTokenIds(template, assigned, matchIds);
                }

                return true;
            }
            else
            {
                collector.Record(DiagnosticEventType.TokenAssignmentFailed,
                    tokenName: string.Join(", ", candidates.Tokens.Select(t => t.Name)),
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
            result.AddException(e);
            return false;
        }
    }

    /// <summary>
    /// Processes front matter tokens that don't require input text matching.
    /// </summary>
    /// <param name="template">The template containing front matter token definitions</param>
    /// <param name="targetObject">The object to populate with front matter token values</param>
    /// <param name="location">The current file location</param>
    /// <param name="result">The result object to populate with matches</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    private void ProcessFrontMatterTokens(
        Template template,
        object? targetObject,
        FileLocation location,
        TokenizeResultBase result,
        IDiagnosticCollector collector)
    {
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        // Note: targetObject can be null - this is a valid use case for Tokenize(Template, string)
        ArgumentValidation.ThrowIfNull(location, nameof(location));
        ArgumentValidation.ThrowIfNull(result, nameof(result));

        foreach (var token in template.Tokens)
        {
            if (!token.IsFrontMatterToken) continue;

            if (token.Assign(targetObject, string.Empty, template.Options, location, out var assignedValue, collector))
            {
                collector.Record(DiagnosticEventType.FrontMatterTokenAssigned,
                    tokenName: token.Name, tokenId: token.Id,
                    value: assignedValue?.ToString());
                if (assignedValue != null)
                {
                    result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
            }
            else
            {
                collector.Record(DiagnosticEventType.FrontMatterTokenFailed,
                    tokenName: token.Name, tokenId: token.Id);
            }
        }
    }

    /// <summary>
    /// Handles the processing of repeated tokens and manages disabled repeating tokens.
    /// </summary>
    /// <param name="candidates">The list of candidate tokens</param>
    /// <param name="enumerator">The token enumerator</param>
    /// <param name="replacement">The StringBuilder containing the token value</param>
    /// <param name="result">The result object</param>
    /// <param name="disabledRepeatingTokens">The set of disabled repeating token IDs</param>
    /// <param name="matchIds">The set of matched token IDs</param>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    /// <returns>True if processing should continue, false if candidates were cleared</returns>
    private bool ProcessRepeatedTokens(
        CandidateTokenList candidates,
        TokenEnumerator enumerator,
        StringBuilder replacement,
        TokenizeResultBase result,
        HashSet<int> disabledRepeatingTokens,
        HashSet<int> matchIds,
        Template template,
        IDiagnosticCollector collector)
    {
        ArgumentValidation.ThrowIfNull(candidates, nameof(candidates));
        ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));
        ArgumentValidation.ThrowIfNull(replacement, nameof(replacement));
        ArgumentValidation.ThrowIfNull(result, nameof(result));
        ArgumentValidation.ThrowIfNull(disabledRepeatingTokens, nameof(disabledRepeatingTokens));
        ArgumentValidation.ThrowIfNull(matchIds, nameof(matchIds));
        ArgumentValidation.ThrowIfNull(template, nameof(template));

        var replacementValue = replacement.ToString();

        // Can't assign, so clear current context and move to next match
        if (candidates.CanAnyAssign(replacementValue) == false)
        {
            collector.Record(DiagnosticEventType.BacktrackStarted,
                tokenName: string.Join(", ", candidates.Tokens.Select(t => t.Name)),
                location: enumerator.Location,
                value: replacementValue);

            // Layer 3: Environment Guards
            // Prevent infinite loop when backtracking with empty preamble
            var advanceLength = candidates.Preamble.Length;
            if (advanceLength == 0 && candidates.Tokens.Count > 0)
            {
                var tokenNames = string.Join(", ", candidates.Tokens.Select(t => t.Name));
                log.LogError(
                    "Infinite loop detected: Cannot backtrack with empty preamble for tokens [{TokenNames}]. " +
                    "This occurs when consecutive tokens have no separator and assignment fails. " +
                    "Current position: Line {Line}, Column {Column}",
                    tokenNames, enumerator.Location.Line, enumerator.Location.Column);

                throw new InvalidOperationException(
                    $"Tokenization cannot proceed: tokens with empty preambles ({tokenNames}) cannot be " +
                    $"distinguished from each other. Add separators (preambles) between consecutive tokens, " +
                    $"or ensure the target object has writable properties.");
            }

            for (var i = 0; i < candidates.Tokens.Count; i++)
            {
                // If repeated token was the last match, then this non-match will stop it
                // matching any future results
                var token = candidates.Tokens[i];
                if (WasLastMatchedToken(result, token) && string.IsNullOrWhiteSpace(token.Preamble) && string.IsNullOrWhiteSpace(replacementValue))
                {
                    collector.Record(DiagnosticEventType.RepeatingTokenDisabled,
                        tokenName: token.Name, tokenId: token.Id,
                        location: enumerator.Location);
                    disabledRepeatingTokens.Add(token.Id);
                    candidates.Remove(token);
                    i--;
                }
                else if (token.IsSingleUse)
                {
                    collector.Record(DiagnosticEventType.SingleUseTokenRemoved,
                        tokenName: token.Name, tokenId: token.Id,
                        location: enumerator.Location);
                    candidates.Remove(token);
                    result.Tokens.AddMiss(token);
                    matchIds.Add(token.Id);
                }
            }

            replacement.Clear();
            enumerator.Advance(advanceLength);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Handles newline-terminated token processing.
    /// </summary>
    /// <param name="candidates">The list of candidate tokens</param>
    /// <param name="targetObject">The object to populate with matched token values</param>
    /// <param name="replacement">The StringBuilder containing the token value</param>
    /// <param name="options">The tokenizer options</param>
    /// <param name="location">The current file location</param>
    /// <param name="result">The result object to populate with matches</param>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="matchIds">The set of matched token IDs</param>
    /// <param name="enumerator">The token enumerator</param>
    /// <param name="disabledRepeatingTokens">The set of disabled repeating token IDs</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    private void ProcessNewlineTerminatedTokens(
        CandidateTokenList candidates,
        object? targetObject,
        StringBuilder replacement,
        TokenizerOptions options,
        FileLocation location,
        TokenizeResultBase result,
        Template template,
        HashSet<int> matchIds,
        TokenEnumerator enumerator,
        HashSet<int> disabledRepeatingTokens,
        IDiagnosticCollector collector)
    {
        ArgumentValidation.ThrowIfNull(candidates, nameof(candidates));
        // Note: targetObject can be null - this is a valid use case for Tokenize(Template, string)
        ArgumentValidation.ThrowIfNull(replacement, nameof(replacement));
        ArgumentValidation.ThrowIfNull(options, nameof(options));
        ArgumentValidation.ThrowIfNull(location, nameof(location));
        ArgumentValidation.ThrowIfNull(result, nameof(result));
        ArgumentValidation.ThrowIfNull(template, nameof(template));
        ArgumentValidation.ThrowIfNull(matchIds, nameof(matchIds));
        ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));
        ArgumentValidation.ThrowIfNull(disabledRepeatingTokens, nameof(disabledRepeatingTokens));

        var replacementValue = replacement.ToString();

        collector.Record(DiagnosticEventType.NewlineTerminatedTokenProcessed,
            tokenName: candidates.Tokens.First().Name,
            tokenId: candidates.Tokens.First().Id,
            value: replacementValue,
            location: location);

        if (candidates.Tokens.First().IsRepeating &&
            string.IsNullOrWhiteSpace(candidates.Preamble) &&
            result.Tokens.HasMatches)
        {
            if (result.Tokens.Matches.Last().Token.Id == candidates.Tokens.First().Id)
            {
                if (enumerator.Location.Line > result.Tokens.Matches.Last().Location.Line + 1)
                {
                    disabledRepeatingTokens.Add(candidates.Tokens.First().Id);
                    candidates.Remove(candidates.Tokens.First());
                }
            }
        }

        TryAssignCandidateTokens(candidates, targetObject, replacement, options, location, result, template, matchIds, collector);
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
    /// Handles matching of repeated tokens.
    /// </summary>
    private bool HandleRepeatedTokenMatching(ITokenizationContext context, Template template, TokenizeResultBase result, object? targetObject, IDiagnosticCollector collector)
    {
        if (!ProcessRepeatedTokens(context.Candidates, context.Enumerator, context.Replacement,
            result, context.DisabledRepeatingTokens, context.MatchIds, template, collector))
        {
            return false;
        }
        return true;
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
    private void HandleNewlineTerminatedToken(ITokenizationContext context, Template template, object? targetObject, TokenizeResultBase result, IDiagnosticCollector collector)
    {
        ProcessNewlineTerminatedTokens(context.Candidates, targetObject, context.Replacement,
            template.Options, context.Enumerator.Location, result, template,
            context.MatchIds, context.Enumerator, context.DisabledRepeatingTokens, collector);

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
    /// <param name="context">The tokenization context</param>
    /// <param name="template">The template containing token definitions</param>
    /// <param name="targetObject">The object to populate with matched token values</param>
    /// <param name="result">The result object to populate with matches</param>
    /// <param name="matches">The newly matched tokens</param>
    /// <param name="collector">The diagnostic collector for recording analysis information.</param>
    private void HandleTokenSwitch(ITokenizationContext context, Template template, object? targetObject, TokenizeResultBase result, IList<Token> matches, IDiagnosticCollector collector)
    {
        TryAssignCandidateTokens(context.Candidates, targetObject, context.Replacement,
            template.Options, context.ReplacementLocation, result, template, context.MatchIds, collector);

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
