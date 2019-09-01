using System.Collections.Generic;

namespace Tokens
{
    /// <summary>
    /// Holds a list of candidate tokens to match during a Tokenize operation.
    /// </summary>
    internal class CandidateTokenList 
    {
        private readonly List<Token> tokens = new List<Token>();

        public void Add(Token token)
        {
            if (tokens.Count == 0)
            {
                Preamble = token.Preamble;
                TerminateOnNewLine = token.TerminateOnNewLine;
                IsNullToken = string.IsNullOrWhiteSpace(token.Name);
                tokens.Add(token);
            }
            else
            {
                tokens.Add(token);
            }
        }

        public void AddRange(IEnumerable<Token> tokens)
        {
            foreach (var token in tokens)
            {
                Add(token);
            }
        }

        public void Clear()
        {
            Preamble = null;
            tokens.Clear();
        }

        public bool TryAssign(object target, string value, TokenizerOptions options, int line, int column, out Token assigned)
        {
            assigned = null;

            foreach (var token in tokens)
            {
                if (token.Assign(target, value, options, line, column))
                {
                    assigned = token;

                    return true;
                }
            }

            return false;
        }

        public bool CanAnyAssign(string value)
        {
            foreach (var token in tokens)
            {
                if (token.CanAssign(value))
                {
                    return true;
                }
            }

            return false;
        }

        public bool Any => Count > 0;

        public int Count => tokens.Count;

        public string Preamble { get; private set; }

        public bool TerminateOnNewLine { get; private set; }

        public bool IsNullToken { get; private set; }

        public IList<Token> Tokens => tokens;
    }
}
