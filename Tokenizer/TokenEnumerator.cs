using System.Collections.Generic;
using System.Text;

namespace Tokens
{
    internal class TokenEnumerator
    {
        private string pattern;
        private int currentLocation;
        private int patternLength;
        private StringBuilder preamble;

        public TokenEnumerator(string pattern)
        {
            preamble = new StringBuilder();

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
        }

        public bool IsEmpty => currentLocation >= patternLength;

        public string Preamble => preamble.ToString();

        public string Next()
        {
            if (IsEmpty) return string.Empty;

            var next = pattern.Substring(currentLocation, 1);

            currentLocation++;

            preamble.Append(next);

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

            return value == pattern.Substring(currentLocation, value.Length);
        }

        public void Advance(int count)
        {
            currentLocation += count;
        }

        public bool Match(Queue<Token> tokens, out Token match)
        {
            foreach (var token in tokens)
            {
                if (Match(token.Preamble))
                {
                    match = token;
                    return true;
                }

                if (token.Optional == false) break;
            }

            match = null;
            return false;
        }
    }
}
