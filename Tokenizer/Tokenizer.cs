using System.Collections.Generic;
using System.Text;
using Tokens.Enumerators;
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
        }

        public T Parse<T>(string pattern, string input) where T : class, new()
        {
            var template = parser.Parse(pattern);

            return Parse<T>(template, input);
        }

        public T Parse<T>(Template template, string input) where T : class, new()
        {
            TryParse(template, input, out _, out T result);

            return result;
        }

        public bool TryParse<T>(string pattern, string input, out int matches, out T result) where T : class, new()
        {
            var template = parser.Parse(pattern);

            var parsed = TryParse<T>(template, input, out var m, out var r);

            matches = m;
            result = r;

            return parsed;
        }

        public bool TryParse<T>(Template template, string input, out int matches, out T result) where T : class, new()
        {
            Token current = null;
            matches = 0;

            var value = new T();
            var enumerator = new TokenEnumerator(input);
            var replacement = new StringBuilder();
            var matchIds = new List<int>();

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
                    if (current == null)
                    {
                        current = match;
                        replacement.Clear();
                        enumerator.Advance(match.Preamble.Length);
                        matchIds.AddRange(template.GetTokenIdsUpTo(match));
                    }
                    else if (replacement.Length > 0 && current.Assign(value, replacement.ToString(), template.Options))
                    {
                        matches++;
                        current = match;
                        replacement.Clear();
                        enumerator.Advance(match.Preamble.Length);
                        matchIds.AddRange(template.GetTokenIdsUpTo(match));
                    }
                    else
                    {
                        replacement.Append(next);
                        enumerator.Next();
                    }
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
                current.Assign(value, replacement.ToString(), template.Options);
                matches++;
            }

            result = value;

            return matches > 0;
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
