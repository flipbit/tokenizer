using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tokens.Enumerators;

namespace Tokens.Tokenization
{
    internal static class ArgumentValidation
    {
        public static void ThrowIfNull(object argument, string paramName)
        {
            if (argument == null) throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// Core tokenization engine that processes input text and matches tokens according to template patterns.
    /// This service encapsulates the main tokenization algorithm and handles candidate token processing,
    /// input enumeration, and token matching logic.
    /// </summary>
    public class TokenizationEngine : ITokenizationEngine
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
        /// <param name="input">The input text to tokenize</param>
        /// <param name="targetObject">The object to populate with matched token values</param>
        /// <param name="context">The tokenization context containing shared state</param>
        /// <param name="result">The result object to populate with matches and misses</param>
        public void ProcessTokenization(
            Template template, 
            string input, 
            object? targetObject, 
            ITokenizationContext context, 
            TokenizeResultBase result)
        {
            ArgumentValidation.ThrowIfNull(template, nameof(template));
            ArgumentValidation.ThrowIfNull(input, nameof(input));
            // Note: targetObject can be null - this is a valid use case for Tokenize(Template, string)
            ArgumentValidation.ThrowIfNull(context, nameof(context));
            ArgumentValidation.ThrowIfNull(result, nameof(result));

            log.LogTrace("Start: Processing: {TemplateName}", template.Name);
            log.LogDebug("Tokenization started for template '{TemplateName}' with input length {InputLength}",
                template.Name, input.Length);

            context.Initialize(input);

            log.LogDebug("Phase: Initialization completed. Starting main tokenization loop with {TokenCount} tokens",
                template.Tokens.Count);

                // Main tokenization loop
                while (context.Enumerator.IsEmpty == false)
                {
                    var next = context.Enumerator.Peek();
                    log.LogTrace("Enumerator position: Line {Line}, Column {Column}, Peeking character '{NextChar}'",
                        context.Enumerator.Location.Line, context.Enumerator.Location.Column,
                        next == "\n" ? "\\n" : next == "\r" ? "\\r" : next);

                    // Handle Windows new lines (normalize to Unix)
                    if (next == "\r" && context.Enumerator.Peek(1) == "\n")
                    {
                        log.LogTrace("Normalizing Windows line ending (CRLF) to Unix (LF) at position Line {Line}, Column {Column}",
                            context.Enumerator.Location.Line, context.Enumerator.Location.Column);
                        context.Enumerator.Next();
                        next = "\n";
                    }

                    // Check for repeated current token
                    if (context.Candidates.Any && context.Enumerator.Match(context.Candidates.Preamble) && context.Candidates.Preamble.Length > 0)
                    {
                        log.LogTrace("Attempting to match repeated token with preamble '{Preamble}' at Line {Line}, Column {Column}",
                            context.Candidates.Preamble, context.Enumerator.Location.Line, context.Enumerator.Location.Column);

                        if (!ProcessRepeatedTokens(context.Candidates, context.Enumerator, context.Replacement,
                            result, context.DisabledRepeatingTokens, context.MatchIds, template))
                        {
                            log.LogTrace("Repeated token processing resulted in backtrack. Clearing candidates and continuing.");
                            continue;
                        }
                    }

                    // Assign newline terminated token
                    if (context.Candidates.Any && context.Candidates.TerminateOnNewLine && next == "\n")
                    {
                        log.LogTrace("Newline detected at Line {Line}, Column {Column}. Processing newline-terminated token with {CandidateCount} candidates",
                            context.Enumerator.Location.Line, context.Enumerator.Location.Column, context.Candidates.Tokens.Count);

                        ProcessNewlineTerminatedTokens(context.Candidates, targetObject, context.Replacement,
                            template.Options, context.Enumerator.Location, result, template,
                            context.MatchIds, context.Enumerator, context.DisabledRepeatingTokens);

                        context.ClearCandidates();
                        context.ClearReplacement();
                        context.ReplacementLocation = context.Enumerator.Location;
                    }

                    // Check for next token
                    if (context.Enumerator.Match(template.TokensExcluding(context.MatchIds, context.Candidates, context.DisabledRepeatingTokens), template.Options.OutOfOrderTokens, out var matches))
                    {
                        log.LogTrace("Token match found at Line {Line}, Column {Column}. Matched {MatchCount} token(s): {TokenNames}",
                            context.Enumerator.Location.Line, context.Enumerator.Location.Column,
                            matches.Count, string.Join(", ", matches.Select(m => m.Name)));

                        // Special case: first token found, just prepare to read token value
                        if (context.Candidates.Any == false)
                        {
                            log.LogTrace("First token match. Adding {MatchCount} candidates and advancing {AdvanceLength} positions",
                                matches.Count, matches.First().Preamble.Length);

                            context.Candidates.AddRange(matches);
                            context.ClearReplacement();
                            context.Enumerator.Advance(context.Candidates.Preamble.Length);

                            log.LogTrace("Enumerator advanced to Line {Line}, Column {Column}",
                                context.Enumerator.Location.Line, context.Enumerator.Location.Column);
                            continue;
                        }

                        if (context.Replacement.Length > 0)
                        {
                            log.LogTrace("Processing previous token value '{ReplacementValue}' with {CandidateCount} candidates",
                                context.Replacement.ToString(), context.Candidates.Tokens.Count);

                            TryAssignCandidateTokens(context.Candidates, targetObject, context.Replacement,
                                template.Options, context.ReplacementLocation, result, template, context.MatchIds);

                            context.ClearCandidates();
                            context.Candidates.AddRange(matches);
                            context.ClearReplacement();
                            context.Enumerator.Advance(context.Candidates.Preamble.Length);
                            context.ReplacementLocation = context.Enumerator.Location;

                            log.LogTrace("Switched to new token. Enumerator advanced to Line {Line}, Column {Column}",
                                context.Enumerator.Location.Line, context.Enumerator.Location.Column);
                            continue;
                        }

                        log.LogTrace("Appending character to replacement buffer at Line {Line}, Column {Column}",
                            context.Enumerator.Location.Line, context.Enumerator.Location.Column);
                        context.Replacement.Append(next);
                        context.Enumerator.Next();
                    }
                    else
                    {
                        log.LogTrace("No token match at Line {Line}, Column {Column}. Appending character '{NextChar}' to replacement buffer",
                            context.Enumerator.Location.Line, context.Enumerator.Location.Column,
                            next == "\n" ? "\\n" : next == "\r" ? "\\r" : next);

                        // Append to replacement
                        context.Replacement.Append(next);
                        context.Enumerator.Next();

                        log.LogTrace("Enumerator moved to Line {Line}, Column {Column}",
                            context.Enumerator.Location.Line, context.Enumerator.Location.Column);
                    }
                }

                log.LogDebug("Phase: Main tokenization loop completed. Processing remaining candidates and front matter");

                // Handle remaining candidates
                if (context.Candidates.Any && context.Replacement.Length > 0 && !context.Candidates.IsNullToken)
                {
                    log.LogTrace("Processing {CandidateCount} remaining candidates with replacement value '{ReplacementValue}'",
                        context.Candidates.Tokens.Count, context.Replacement.ToString());

                    TryAssignCandidateTokens(context.Candidates, targetObject, context.Replacement,
                        template.Options, context.ReplacementLocation, result, template, context.MatchIds);
                }
                else if (context.Candidates.Any)
                {
                    log.LogTrace("Skipping remaining candidates: ReplacementLength={ReplacementLength}, IsNullToken={IsNullToken}",
                        context.Replacement.Length, context.Candidates.IsNullToken);
                }

                // Process front matter tokens
                log.LogDebug("Phase: Processing front matter tokens");
                ProcessFrontMatterTokens(template, targetObject, context.Enumerator.Location, result);

                log.LogTrace("Found {MatchCount} matches.", result.Tokens.Matches.Count);
                log.LogTrace("{MissingCount} required tokens were missing.", result.Tokens.Misses.Count(t => t.Required));
                log.LogDebug("Phase: Tokenization summary - Matches: {MatchCount}, Misses: {MissCount}, Exceptions: {ExceptionCount}",
                    result.Tokens.Matches.Count, result.Tokens.Misses.Count, result.Exceptions.Count);

            log.LogTrace("Finished: Processing: {TemplateName}", template.Name);
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
        /// <returns>True if any tokens were successfully assigned</returns>
        public bool TryAssignCandidateTokens(
            CandidateTokenList candidates, 
            object targetObject, 
            StringBuilder replacement, 
            TokenizerOptions options, 
            FileLocation location, 
            TokenizeResultBase result, 
            Template template, 
            HashSet<int> matchIds)
        {
            ArgumentValidation.ThrowIfNull(candidates, nameof(candidates));
            // Note: targetObject can be null - this is a valid use case for Tokenize(Template, string)
            ArgumentValidation.ThrowIfNull(replacement, nameof(replacement));
            ArgumentValidation.ThrowIfNull(options, nameof(options));
            ArgumentValidation.ThrowIfNull(location, nameof(location));
            ArgumentValidation.ThrowIfNull(result, nameof(result));
            ArgumentValidation.ThrowIfNull(template, nameof(template));
            ArgumentValidation.ThrowIfNull(matchIds, nameof(matchIds));

            log.LogTrace("Attempting to assign {CandidateCount} candidate token(s) with value '{ReplacementValue}' at Line {Line}, Column {Column}",
                candidates.Tokens.Count, replacement.ToString(), location.Line, location.Column);

            try
            {
                if (candidates.TryAssign(targetObject, replacement, options, location, out var assigned, out var assignedValue))
                {
                    log.LogTrace("Token assignment succeeded: Token '{TokenName}' ({TokenId}) = '{AssignedValue}' at Line {Line}, Column {Column}",
                        assigned.Name, assigned.Id, assignedValue, location.Line, location.Column);

                    result.Tokens.AddMatch(assigned, assignedValue, location);
                    AddMatchedTokenIds(template, assigned, matchIds);

                    log.LogDebug("Token matched: '{TokenName}' = '{AssignedValue}' at Line {Line}, Column {Column}",
                        assigned.Name, assignedValue, location.Line, location.Column);
                    return true;
                }
                else
                {
                    log.LogTrace("Token assignment failed for {CandidateCount} candidate(s) at Line {Line}, Column {Column}",
                        candidates.Tokens.Count, location.Line, location.Column);

                    foreach (var token in candidates.Tokens)
                    {
                        log.LogTrace("Ln: {Line} Col: {Column} : Skipping {TokenName} ({TokenId}), '{Replacement}' is not a match.",
                            location.Line, location.Column, token.Name, token.Id, replacement.ToString());
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                log.LogTrace(e, "Error Assigning Value: {Message}", e.Message);
                result.Exceptions.Add(e);
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
        public void ProcessFrontMatterTokens(
            Template template, 
            object targetObject, 
            FileLocation location, 
            TokenizeResultBase result)
        {
            ArgumentValidation.ThrowIfNull(template, nameof(template));
            // Note: targetObject can be null - this is a valid use case for Tokenize(Template, string)
            ArgumentValidation.ThrowIfNull(location, nameof(location));
            ArgumentValidation.ThrowIfNull(result, nameof(result));

            var frontMatterTokens = template.Tokens.Where(t => t.IsFrontMatterToken).ToList();
            log.LogTrace("Processing {FrontMatterCount} front matter tokens", frontMatterTokens.Count);

            foreach (var token in frontMatterTokens)
            {
                log.LogTrace("Attempting front matter token assignment: '{TokenName}' ({TokenId})", token.Name, token.Id);

                if (token.Assign(targetObject, string.Empty, template.Options, location, out var assignedValue))
                {
                    log.LogTrace("Front matter token assigned: '{TokenName}' = '{AssignedValue}'", token.Name, assignedValue);
                    result.Tokens.AddMatch(token, assignedValue, token.Location);
                }
                else
                {
                    log.LogTrace("Front matter token assignment failed: '{TokenName}'", token.Name);
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
        /// <returns>True if processing should continue, false if candidates were cleared</returns>
        public bool ProcessRepeatedTokens(
            CandidateTokenList candidates, 
            TokenEnumerator enumerator, 
            StringBuilder replacement, 
            TokenizeResultBase result, 
            HashSet<int> disabledRepeatingTokens, 
            HashSet<int> matchIds, 
            Template template)
        {
            ArgumentValidation.ThrowIfNull(candidates, nameof(candidates));
            ArgumentValidation.ThrowIfNull(enumerator, nameof(enumerator));
            ArgumentValidation.ThrowIfNull(replacement, nameof(replacement));
            ArgumentValidation.ThrowIfNull(result, nameof(result));
            ArgumentValidation.ThrowIfNull(disabledRepeatingTokens, nameof(disabledRepeatingTokens));
            ArgumentValidation.ThrowIfNull(matchIds, nameof(matchIds));
            ArgumentValidation.ThrowIfNull(template, nameof(template));

            log.LogTrace("Checking if any of {CandidateCount} candidate(s) can assign replacement value '{ReplacementValue}'",
                candidates.Tokens.Count, replacement.ToString());

            // Can't assign, so clear current context and move to next match
            if (candidates.CanAnyAssign(replacement.ToString()) == false)
            {
                log.LogTrace("Backtracking: None of the {CandidateCount} candidates can assign the replacement value at Line {Line}, Column {Column}",
                    candidates.Tokens.Count, enumerator.Location.Line, enumerator.Location.Column);

                for (var i = 0; i < candidates.Tokens.Count; i++)
                {
                    // If repeated token was the last match, then this non-match will stop it
                    // matching any future results
                    var token = candidates.Tokens[i];
                    if (WasLastMatchedToken(result, token) && string.IsNullOrWhiteSpace(token.Preamble) && string.IsNullOrWhiteSpace(replacement.ToString()))
                    {
                        log.LogTrace("Ln: {Line} Col: {Column} : Skipping {TokenName} ({TokenId}), '{Replacement}' is not a match.",
                            enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                        log.LogTrace("Backtracking: Disabling repeating token '{TokenName}' ({TokenId}) - was last matched and failed to repeat",
                            token.Name, token.Id);
                        disabledRepeatingTokens.Add(token.Id);
                        candidates.Remove(token);
                        i--;
                    }
                    else if (token.ConsiderOnce)
                    {
                        log.LogTrace("Ln: {Line} Col: {Column} : Skipping & removing {TokenName} ({TokenId}), '{Replacement}' is not a match.",
                            enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                        log.LogTrace("Backtracking: Removing ConsiderOnce token '{TokenName}' ({TokenId}) and marking as miss",
                            token.Name, token.Id);

                        candidates.Remove(token);
                        result.Tokens.AddMiss(token);
                        matchIds.Add(token.Id);
                    }
                    else
                    {
                        log.LogTrace("Ln: {Line} Col: {Column} : Skipping {TokenName} ({TokenId}), '{Replacement}' is not a match.",
                            enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                    }
                }

                replacement.Clear();
                var advanceLength = candidates.Preamble.Length;
                log.LogTrace("Backtracking: Advancing {AdvanceLength} positions to retry from Line {Line}, Column {Column}",
                    advanceLength, enumerator.Location.Line, enumerator.Location.Column);
                enumerator.Advance(advanceLength);

                log.LogTrace("Backtracking: Enumerator advanced to Line {Line}, Column {Column}",
                    enumerator.Location.Line, enumerator.Location.Column);
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
        public void ProcessNewlineTerminatedTokens(
            CandidateTokenList candidates, 
            object targetObject, 
            StringBuilder replacement, 
            TokenizerOptions options, 
            FileLocation location, 
            TokenizeResultBase result, 
            Template template, 
            HashSet<int> matchIds, 
            TokenEnumerator enumerator, 
            HashSet<int> disabledRepeatingTokens)
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

            log.LogTrace("Processing newline-terminated token with {CandidateCount} candidates, replacement '{ReplacementValue}'",
                candidates.Tokens.Count, replacement.ToString());

            if (candidates.Tokens.First().Repeating &&
                string.IsNullOrWhiteSpace(candidates.Preamble) &&
                result.Tokens.HasMatches)
            {
                if (result.Tokens.Matches.Last().Token.Id == candidates.Tokens.First().Id)
                {
                    if (enumerator.Location.Line > result.Tokens.Matches.Last().Location.Line + 1)
                    {
                        log.LogTrace("Disabling repeating token '{TokenName}' ({TokenId}) due to line gap: current line {CurrentLine}, last match line {LastMatchLine}",
                            candidates.Tokens.First().Name, candidates.Tokens.First().Id,
                            enumerator.Location.Line, result.Tokens.Matches.Last().Location.Line);

                        disabledRepeatingTokens.Add(candidates.Tokens.First().Id);
                        candidates.Remove(candidates.Tokens.First());
                    }
                }
            }

            TryAssignCandidateTokens(candidates, targetObject, replacement, options, location, result, template, matchIds);
        }

        /// <summary>
        /// Adds matched token IDs to the tracking set for template ordering logic.
        /// </summary>
        /// <param name="template">The template containing token definitions</param>
        /// <param name="matchedToken">The token that was matched</param>
        /// <param name="matchIds">The set of matched token IDs to update</param>
        private void AddMatchedTokenIds(Template template, Token matchedToken, HashSet<int> matchIds)
        {
            var tokenIdsToAdd = template.GetTokenIdsUpTo(matchedToken);
            
            foreach (var tokenId in tokenIdsToAdd)
            {
                if (matchIds.Contains(tokenId) == false)
                {
                    matchIds.Add(tokenId);
                }
            }
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
    }
}
