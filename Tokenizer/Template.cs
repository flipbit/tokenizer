using System.Collections.Generic;

namespace Tokens
{
    /// <summary>
    /// Represents a template to use to extract data from
    /// free text.
    /// </summary>
    public class Template
    {
        public Template()
        {
            Tokens = new Queue<Token>();
        }

        public Template(string name, string content) : this()
        {
            Name = name;
            Content = content;
        }

        /// <summary>
        /// The template content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The name of the template
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The tokens contained within the template
        /// </summary>
        public Queue<Token> Tokens { get; }

        public Token DequeueUpTo(Token token)
        {
            Token match = null;

            while (Tokens.Count > 0)
            {
                if (token.Repeating)
                {
                    if (Tokens.Peek() == token)
                    {
                        return token;
                    }
                }

                match = Tokens.Dequeue();

                if (match == token) break;
            }

            return match;
        }
    }
}
