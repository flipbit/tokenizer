using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly ILog log;

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

            Tokenize(result, result.Values, template, input);

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
                var line = 1;
                var column = 1;

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
                            foreach (var token in candidates.Tokens)
                            {
                                log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", line, column, token.Name, token.Id, replacement.ToString());
                            }
                            replacement.Clear();
                            enumerator.Advance(candidates.Preamble.Length);
                            line = enumerator.Line;
                            column = enumerator.Column;
                            continue;
                        }
                    }

                    // Assign newline terminated token
                    if (candidates.Any && candidates.TerminateOnNewLine && next == "\n")
                    {
                        using (new LogIndentation())
                        {
                            try
                            {
                                if (candidates.TryAssign(value, replacement.ToString(), template.Options, line, column, out var assigned))
                                {
                                    result.Tokens.AddMatch(assigned, replacement.ToString());
                                    AddMatchedTokenIds(template, assigned, matchIds);
                                }
                                else
                                {
                                    foreach (var token in candidates.Tokens)
                                    {
                                        log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", line, column, token.Name, token.Id, replacement.ToString());
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
                    }

                    // Check for next token
                    if (enumerator.Match(template.TokensExcluding(matchIds, candidates), template.Options.OutOfOrderTokens, out var matches))
                    {
                        // Special case: first token found, just prepare to read token value
                        if (candidates.Any == false)
                        {
                            candidates.AddRange(matches);
                            replacement.Clear();
                            enumerator.Advance(candidates.Preamble.Length);
                            line = enumerator.Line;
                            column = enumerator.Column;
                            continue;
                        }
                        
                        if (replacement.Length > 0)
                        {
                            using (new LogIndentation())
                            {
                                try
                                {
                                    if (candidates.TryAssign(value, replacement.ToString(), template.Options, line, column, out var assigned))
                                    {
                                        result.Tokens.AddMatch(assigned, replacement.ToString());
                                        AddMatchedTokenIds(template, assigned, matchIds);
                                    }
                                    else
                                    {
                                        foreach (var token in candidates.Tokens)
                                        {
                                            log.Verbose("-> Ln: {0} Col: {1} : Skipping {2} ({3}), '{4}' is not a match.", line, column, token.Name, token.Id, replacement.ToString());
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
                            line = enumerator.Line;
                            column = enumerator.Column;
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
                            if (candidates.TryAssign(value, replacement.ToString(), template.Options, line, column, out var assigned))
                            {
                                result.Tokens.AddMatch(assigned, replacement.ToString());
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
                            if (token.Assign(value, string.Empty, template.Options, line, column))
                            {
                                result.Tokens.AddMatch(token, string.Empty);
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
                        log.Debug("  -> Ln:{0} Col:{1} Found Hint: {2}", enumerator.Line, enumerator.Column, hint.Text);
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
                    log.Debug("  -> Missing Hint: {0}", hint.Text);
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
