using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Parsers;

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
        public Tokenizer()
        {
            parser = new TokenParser();

            Options = TokenizerOptions.Defaults;
        }

        public T Parse<T>(string pattern, string input) where T : class, new()
        {
            var value = new T();

            var template = parser.Parse(string.Empty, pattern);
            var enumerator = new TokenEnumerator(input);
            Token current = null;
            var replacement = new StringBuilder();

            while (enumerator.IsEmpty == false)
            {
                var next = enumerator.Peek();

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
                if (template.Tokens.Count > 0 && enumerator.Match(template.NextTokenPreamble))
                {
                    if (current == null)
                    {
                        replacement.Clear();
                        enumerator.Advance(template.NextTokenPreamble.Length);
                        current = template.Tokens.Dequeue();
                    }
                    else if (replacement.Length > 0 && current.Assign(value, replacement.ToString()))
                    {
                        replacement.Clear();

                        enumerator.Advance(template.NextTokenPreamble.Length);

                        current = template.Tokens.Dequeue();
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

            if (current != null && replacement.Length > 0)
            {
                current.Assign(value, replacement.ToString());
            }


            return value;
        }
    }
}
