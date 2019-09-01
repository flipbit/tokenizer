using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tokens.Enumerators
{
    internal class TokenEnumerator
    {
        private string pattern;
        private int currentLocation;
        private int patternLength;

        private bool resetNextLine;

        public TokenEnumerator(string pattern)
        {
            if (string.IsNullOrEmpty(pattern) == false)
            {
                if (pattern.Contains("\r\n"))
                {
                    pattern = pattern.Replace("\r\n", "\n");
                }
            }

            if (string.IsNullOrEmpty(pattern))
            {
                patternLength = 0;
            }
            else
            {
                patternLength = pattern.Length;
            }

            this.pattern = pattern;

            currentLocation = 0;
            Column = 1;
            Line = 1;
        }

        public bool IsEmpty => currentLocation >= patternLength;
        
        public int Line { get; private set; }

        public int Column { get; private set; }

        public string Next()
        {
            if (IsEmpty) return string.Empty;

            var next = pattern.Substring(currentLocation, 1);

            currentLocation++;
            Column++;
            
            if (resetNextLine)
            {
                resetNextLine = false;
                Column = 1;
                Line++;
            }

            if (next == "\n")
            {
                resetNextLine = true;
            }

            return next;
        }

        public string Peek()
        {
            if (IsEmpty) return string.Empty;

            return pattern.Substring(currentLocation, 1);
        }

        public string Peek(int offset)
        {
            if (IsEmpty) return string.Empty;

            var location = currentLocation + offset;

            if (location > patternLength) return string.Empty;

            return pattern.Substring(currentLocation + offset, 1);
        }

        public bool Match(string value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            if (currentLocation + value.Length > pattern.Length) return false;

            var candidate = pattern.Substring(currentLocation, value.Length);

            return value == candidate;
        }

        public void Advance(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Next();
            }
        }

        public bool Match(IEnumerable<Token> tokens, bool outOfOrderTokens, out IList<Token> matches)
        {
            matches = new List<Token>();

            foreach (var token in tokens)
            {
                // Special case: if matching out of order template,
                // don't match any tokens without a value
                if (outOfOrderTokens && string.IsNullOrWhiteSpace(token.Name))
                {
                    continue;
                }

                if (Match(token.Preamble))
                {
                    matches.Add(token);
                }

                if (token.Optional == false) break;
            }

            return matches.Any();
        }

        public void Reset()
        {
            currentLocation = 0;
            Column = 1;
            Line = 1;
        }
    }
}
