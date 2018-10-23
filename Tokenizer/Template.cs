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

        public string NextTokenPreamble
        {
            get
            {
                if (Tokens.Count == 0) return string.Empty;
                if (string.IsNullOrEmpty(Tokens.Peek().Preamble)) return string.Empty;

                return Tokens.Peek().Preamble;
            }
        }
    }
}
