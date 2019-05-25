using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

        public TokenizeResult<T> Tokenize<T>(string pattern, string input) where T : class, new()
        {
            var template = parser.Parse(pattern);

            return Tokenize<T>(template, input);
        }

        public TokenizeResult<T> Tokenize<T>(Template template, string input) where T : class, new()
        {
            log.Debug($"Start: Processing: {template.Name}");

            Token current = null;

            var result = new TokenizeResult<T>(template);
            var value = new T();
            var enumerator = new TokenEnumerator(input);
            var replacement = new StringBuilder();
            var matchIds = new List<int>();

            FindHints(template, enumerator, result);

            while (enumerator.IsEmpty == false)
            {
                var next = enumerator.Peek();

                // Handle Windows new lines (normalize to Unix)
                if (next == "\r" && enumerator.Peek(1) == "\n")
                {
                    enumerator.Next();
                    next = "\n";
                }

                // Check for repeated current token
                if (current != null && enumerator.Match(current.Preamble))
                {
                    // Can't assign, so clear current context and move to next match
                    if (current.CanAssign(replacement.ToString()) == false)
                    {
                        replacement.Clear();
                        enumerator.Advance(current.Preamble.Length);
                        continue;
                    }
                }

                // Check for next token
                if (enumerator.Match(template.TokensExcluding(matchIds), out var match))
                {
                    // Special case: first token found, just prepare to read token value
                    if (current == null)
                    {
                        current = match;
                        replacement.Clear();
                        enumerator.Advance(match.Preamble.Length);
                        matchIds.AddRange(template.GetTokenIdsUpTo(match));
                        continue;
                    }
                    
                    if (replacement.Length > 0)
                    {
                        try
                        {
                            if (current.Assign(value, replacement.ToString(), template.Options, log))
                            {
                                result.Tokens.AddMatch(current, replacement.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                            log.Error(e, "     Error Assigning Value: {0}", e.Message);
                            result.Exceptions.Add(e);
                        }
                        
                        current = match;
                        replacement.Clear();
                        enumerator.Advance(match.Preamble.Length);
                        matchIds.AddRange(template.GetTokenIdsUpTo(match));
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

            if (current != null && replacement.Length > 0 && !string.IsNullOrEmpty(current.Name))
            {
                try
                {
                    if (current.Assign(value, replacement.ToString(), template.Options, log))
                    {
                        result.Tokens.AddMatch(current, replacement.ToString());
                    }
                }
                catch (Exception e)
                {
                    log.Error(e, "    Error Assigning Value: {0}", e.Message);
                    result.Exceptions.Add(e);
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

            result.Value = value;

            log.Debug($"  Found {result.Tokens.Matches.Count} matches.");
            log.Debug("  {0} required tokens were missing.", result.Tokens.Misses.Count(t => t.Required));


            log.Debug($"Finished: Processing: {template.Name}");

            return result;
        }

        private void FindHints<T>(Template template, TokenEnumerator enumerator, TokenizeResult<T> result) where T : class, new()
        {
            if (template.Hints.Count == 0) return;

            while (enumerator.IsEmpty == false)
            {
                // Check hints
                foreach (var hint in template.Hints)
                {
                    if (enumerator.Match(hint.Text) &&
                        result.Hints.AddMatch(hint, enumerator))
                    {
                        log.Debug("  -> Ln:{0} Col:{1} Found Hint: {2}", enumerator.Line, enumerator.Character, hint.Text);
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

    }
}
