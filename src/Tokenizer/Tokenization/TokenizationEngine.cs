using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tokens.Enumerators;
using Tokens.Logging;

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
        private readonly ILog log;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizationEngine"/> class.
        /// </summary>
        public TokenizationEngine()
        {
            log = LogProvider.For<TokenizationEngine>();
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

            log.Verbose($"Start: Processing: {template.Name}");

            using (new LogIndentation())
            {
                context.Initialize(input);

                // Main tokenization loop
                while (context.Enumerator.IsEmpty == false)
                {
                    var next = context.Enumerator.Peek();

                    // Handle Windows new lines (normalize to Unix)
                    if (next == "\r" && context.Enumerator.Peek(1) == "\n")
                    {
                        context.Enumerator.Next();
                        next = "\n";
                    }

                    // Check for repeated current token
                    if (context.Candidates.Any && context.Enumerator.Match(context.Candidates.Preamble) && context.Candidates.Preamble.Length > 0)
                    {
                        if (!ProcessRepeatedTokens(context.Candidates, context.Enumerator, context.Replacement, 
                            result, context.DisabledRepeatingTokens, context.MatchIds, template))
                        {
                            continue;
                        }
                    }

                    // Assign newline terminated token
                    if (context.Candidates.Any && context.Candidates.TerminateOnNewLine && next == "\n")
                    {
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
                        // Special case: first token found, just prepare to read token value
                        if (context.Candidates.Any == false)
                        {
                            context.Candidates.AddRange(matches);
                            context.ClearReplacement();
                            context.Enumerator.Advance(context.Candidates.Preamble.Length);
                            continue;
                        }
                        
                        if (context.Replacement.Length > 0)
                        {
                            using (new LogIndentation())
                            {
                                TryAssignCandidateTokens(context.Candidates, targetObject, context.Replacement, 
                                    template.Options, context.ReplacementLocation, result, template, context.MatchIds);
                            }

                            context.ClearCandidates(); 
                            context.Candidates.AddRange(matches);
                            context.ClearReplacement();
                            context.Enumerator.Advance(context.Candidates.Preamble.Length);
                            context.ReplacementLocation = context.Enumerator.Location;
                            continue;
                        }

                        context.Replacement.Append(next);
                        context.Enumerator.Next();
                    }
                    else 
                    {
                        // Append to replacement
                        context.Replacement.Append(next);
                        context.Enumerator.Next();
                    }
                }

                // Handle remaining candidates
                if (context.Candidates.Any && context.Replacement.Length > 0 && !context.Candidates.IsNullToken)
                {
                    using (new LogIndentation())
                    {
                        TryAssignCandidateTokens(context.Candidates, targetObject, context.Replacement, 
                            template.Options, context.ReplacementLocation, result, template, context.MatchIds);
                    }
                }

                // Process front matter tokens
                ProcessFrontMatterTokens(template, targetObject, context.Enumerator.Location, result);

                log.Verbose($"Found {result.Tokens.Matches.Count} matches.");
                log.Verbose("{0} required tokens were missing.", result.Tokens.Misses.Count(t => t.Required));
            }

            log.Verbose($"Finished: Processing: {template.Name}");
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

            try
            {
                if (candidates.TryAssign(targetObject, replacement, options, location, out var assigned, out var assignedValue))
                {
                    result.Tokens.AddMatch(assigned, assignedValue, location);
                    AddMatchedTokenIds(template, assigned, matchIds);
                    return true;
                }
                else
                {
                    foreach (var token in candidates.Tokens)
                    {
                        log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", 
                            location.Line, location.Column, token.Name, token.Id, replacement.ToString());
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                log.Verbose(e, "Error Assigning Value: {0}", e.Message);
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

            foreach (var token in template.Tokens.Where(t => t.IsFrontMatterToken))
            {
                using (new LogIndentation())
                {
                    if (token.Assign(targetObject, string.Empty, template.Options, location, out var assignedValue))
                    {
                        result.Tokens.AddMatch(token, assignedValue, token.Location);
                    }
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

            // Can't assign, so clear current context and move to next match
            if (candidates.CanAnyAssign(replacement.ToString()) == false)
            {
                for (var i = 0; i < candidates.Tokens.Count; i++)
                {
                    // If repeated token was the last match, then this non-match will stop it
                    // matching any future results
                    var token = candidates.Tokens[i];
                    if (WasLastMatchedToken(result, token) && string.IsNullOrWhiteSpace(token.Preamble) && string.IsNullOrWhiteSpace(replacement.ToString()))
                    {
                        log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", 
                            enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                        using (new LogIndentation())
                        {
                            log.Verbose("-> Disabled this repeating token.");
                            disabledRepeatingTokens.Add(token.Id);
                            candidates.Remove(token);
                            i--;
                        }
                    }
                    else if (token.ConsiderOnce)
                    {
                        log.Verbose("-> Ln: {0} Col: {1} : Skipping & removing {2} ({3}), '{4}' is not a match.", 
                            enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());

                        candidates.Remove(token);
                        result.Tokens.AddMiss(token);
                        matchIds.Add(token.Id);
                    }
                    else
                    {
                        log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", 
                            enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                    }
                }

                replacement.Clear();
                enumerator.Advance(candidates.Preamble.Length);
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

            if (candidates.Tokens.First().Repeating &&
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

            using (new LogIndentation())
            {
                TryAssignCandidateTokens(candidates, targetObject, replacement, options, location, result, template, matchIds);
            }
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
