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
            this.pattern = pattern;
            preamble = new StringBuilder();

            if (string.IsNullOrEmpty(pattern))
            {
                patternLength = 0;
            }
            else
            {
                patternLength = pattern.Length;
            }

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
    }
}
