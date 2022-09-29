﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Tokens.Enumerators;
using Tokens.Logging;
using Tokens.Parsers;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens
{
    /// <summary>
    /// Class that creates objects and populates their properties with values
    /// from input strings
    /// </summary>
    public class Tokenizer
    {
        private readonly TokenParser parser;
        private readonly ILogger<Tokenizer> log;

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public TokenizerOptions Options { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class.
        /// </summary>
        public Tokenizer() : this(TokenizerOptions.Defaults)
        {
        }

        public Tokenizer(TokenizerOptions options)
        {
            parser = new TokenParser(options);

            Options = options;
            log = LogProvider.For<Tokenizer>();
        }

        public TokenizeResult Tokenize(string template, string input)
        {
            var t = parser.Parse(template);

            return Tokenize(t, input);
        }

        public TokenizeResult Tokenize(Template template, string input)
        {
            var result = new TokenizeResult(template);

            Tokenize(result, null, template, input);

            return result;

        }

        public TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new()
        {
            var template = parser.Parse(pattern);

            return Tokenize<T>(template, input);
        }

        public TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new()
        {
            var result = new TokenizeResult<T>(template);

            Tokenize(result, result.Value, template, input);

            return result;
        }

        private void Tokenize(TokenizeResultBase result, object value, Template template, string input)
        {
            log.Verbose($"Start: Processing: {template.Name}");

            using (new LogIndentation())
            {
                var candidates = new CandidateTokenList();

                var enumerator = new TokenEnumerator(input);
                var replacement = new StringBuilder();
                var matchIds = new HashSet<int>();
                var disabledRepeatingTokens = new HashSet<int>();
                var replacementLocation = new FileLocation();

                var hintsMissing = FindHints(template, enumerator, result);

                while (enumerator.IsEmpty == false && hintsMissing == false)
                {
                    var next = enumerator.Peek();

                    // Handle Windows new lines (normalize to Unix)
                    if (next == "\r" && enumerator.Peek(1) == "\n")
                    {
                        enumerator.Next();
                        next = "\n";
                    }

                    // Check for repeated current token
                    if (candidates.Any && enumerator.Match(candidates.Preamble) && candidates.Preamble.Length > 0)
                    {
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
                                    log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                                    using (new LogIndentation())
                                    {
                                        log.Verbose(("-> Disabled this repeating token."));
                                        disabledRepeatingTokens.Add(token.Id);
                                        candidates.Remove(token);
                                        i--;
                                    }
                                }
                                else if (token.ConsiderOnce)
                                {
                                    log.Verbose("-> Ln: {0} Col: {1} : Skipping & removing {2} ({3}), '{4}' is not a match.", enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());

                                    candidates.Remove(token);
                                    result.Tokens.AddMiss(token);
                                    matchIds.Add(token.Id);
                                }
                                else
                                {
                                    log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                                }
                            }

                            replacement.Clear();
                            enumerator.Advance(candidates.Preamble.Length);
                            replacementLocation = enumerator.Location;
                            continue;
                        }
                    }

                    // Assign newline terminated token
                    if (candidates.Any && candidates.TerminateOnNewLine && next == "\n")
                    {
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
                            try
                            {
                                if (candidates.TryAssign(value, replacement, template.Options, replacementLocation, out var assigned, out var assignedValue))
                                {
                                    result.Tokens.AddMatch(assigned, assignedValue, enumerator.Location);
                                    AddMatchedTokenIds(template, assigned, matchIds);
                                }
                                else
                                {
                                    foreach (var token in candidates.Tokens)
                                    {
                                        log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                log.Verbose(e, "Error Assigning Value: {0}", e.Message);
                                result.Exceptions.Add(e);
                            }
                        }

                        candidates.Clear();
                        replacement.Clear();
                        replacementLocation = enumerator.Location;
                    }

                    // Check for next token
                    if (enumerator.Match(template.TokensExcluding(matchIds, candidates, disabledRepeatingTokens), template.Options.OutOfOrderTokens, out var matches))
                    {
                        // Special case: first token found, just prepare to read token value
                        if (candidates.Any == false)
                        {
                            candidates.AddRange(matches);
                            replacement.Clear();
                            enumerator.Advance(candidates.Preamble.Length);
                            continue;
                        }
                        
                        if (replacement.Length > 0)
                        {
                            using (new LogIndentation())
                            {
                                try
                                {
                                    if (candidates.TryAssign(value, replacement, template.Options, replacementLocation, out var assigned, out var assignedValue))
                                    {
                                        result.Tokens.AddMatch(assigned, assignedValue, enumerator.Location);
                                        AddMatchedTokenIds(template, assigned, matchIds);
                                    }
                                    else
                                    {
                                        foreach (var token in candidates.Tokens)
                                        {
                                            log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", enumerator.Location.Line, enumerator.Location.Column, token.Name, token.Id, replacement.ToString());
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    log.Verbose(e, "Error Assigning Value: {0}", e.Message);
                                    result.Exceptions.Add(e);
                                }
                            }

                            candidates.Clear(); 
                            candidates.AddRange(matches);
                            replacement.Clear();
                            enumerator.Advance(candidates.Preamble.Length);
                            replacementLocation = enumerator.Location;
                            continue;
                        }

                        replacement.Append(next);
                        enumerator.Next();
                    }

                    // Append to replacement
                    else 
                    {
                        replacement.Append(next);
                        enumerator.Next();
                    }
                }

                if (candidates.Any && replacement.Length > 0 && !candidates.IsNullToken)
                {
                    using (new LogIndentation())
                    {
                        try
                        {
                            if (candidates.TryAssign(value, replacement, template.Options, replacementLocation, out var assigned, out var assignedValue))
                            {
                                result.Tokens.AddMatch(assigned, assignedValue, replacementLocation);
                                AddMatchedTokenIds(template, assigned, matchIds);
                            }
                        }
                        catch (Exception e)
                        {
                            log.Verbose(e, "Error Assigning Value: {0}", e.Message);
                            result.Exceptions.Add(e);
                        }
                    }
                }

                // Process front matter tokens
                if (hintsMissing == false && (matchIds.Any() || template.HasOnlyFrontMatterTokens))
                {
                    foreach (var token in template.Tokens.Where(t => t.IsFrontMatterToken))
                    {
                        using (new LogIndentation())
                        {
                            if (token.Assign(value, string.Empty, template.Options, enumerator.Location, out var assignedValue))
                            {
                                result.Tokens.AddMatch(token, assignedValue, token.Location);
                            }
                        }
                    }
                }
                // Build unmatched collection
                foreach (var token in template.Tokens)
                {
                    if (result.Tokens.Matches.Any(m => m.Token.Id == token.Id) == false)
                    {
                        result.Tokens.Misses.Add(token);
                    }
                }

                log.Verbose($"Found {result.Tokens.Matches.Count} matches.");
                log.Verbose("{0} required tokens were missing.", result.Tokens.Misses.Count(t => t.Required));
            }

            log.Verbose($"Finished: Processing: {template.Name}");
        }

        private void AddMatchedTokenIds(Template template, Token match, HashSet<int> matchIds)
        {
            var tokenIdsToAdd = template.GetTokenIdsUpTo(match);
            
            foreach (var tokenId in tokenIdsToAdd)
            {
                if (matchIds.Contains(tokenId) == false)
                {
                    matchIds.Add(tokenId);
                }
            }
        }

        private bool WasLastMatchedToken(TokenizeResultBase result, Token token)
        {
            var lastMatch = result.Tokens.Matches.LastOrDefault();

            if (lastMatch != null)
            {
                return lastMatch.Token.Id == token.Id;
            }

            return false;
        }

        private bool FindHints(Template template, TokenEnumerator enumerator, TokenizeResultBase result) 
        {
            if (template.Hints.Count == 0) return false;

            while (enumerator.IsEmpty == false)
            {
                // Check hints
                foreach (var hint in template.Hints)
                {
                    if (enumerator.Match(hint.Text) &&
                        result.Hints.AddMatch(hint, enumerator))
                    {
                        log.Verbose("  -> Ln:{0} Col:{1} Found Hint: {2}", enumerator.Location.Line, enumerator.Location.Column, hint.Text);
                    }
                }

                // Exit early if all hints found
                if (result.Hints.Matches.Count == template.Hints.Count) break;

                enumerator.Next();
            }

            // Build unmatched hint collection
            foreach (var hint in template.Hints)
            {
                if (result.Hints.AddMiss(hint))
                {
                    log.Verbose("  -> Missing Hint: {0}", hint.Text);
                }
            }

            enumerator.Reset();

            return result.Hints.Misses.Any(h => h.Optional == false);
        }

        public Tokenizer RegisterTransformer<T>() where T : ITokenTransformer
        {
            parser.RegisterTransformer<T>();

            return this;
        }

        public Tokenizer RegisterValidator<T>() where T : ITokenValidator
        {
            parser.RegisterValidator<T>();

            return this;
        }
        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
