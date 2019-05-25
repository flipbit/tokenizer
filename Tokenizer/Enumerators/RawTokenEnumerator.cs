using System.Text;

namespace Tokens.Enumerators
{
    internal class RawTokenEnumerator
    {
        private readonly string pattern;
        private readonly int patternLength;

        private int currentLocation;
        private bool resetNextLine;

        public RawTokenEnumerator(string pattern)
        {
            this.pattern = pattern;

            if (string.IsNullOrEmpty(pattern))
            {
                patternLength = 0;
            }
            else
            {
                patternLength = pattern.Length;
            }

            currentLocation = 0;

            Column = 0;
            Line = 1;
        }

        public bool IsEmpty => currentLocation >= patternLength;

        public int Line { get; private set; }

        public int Column { get; private set; }

        public string Next()
        {
            if (IsEmpty) return string.Empty;

            if (resetNextLine)
            {
                Line++;
                Column = 1;
                resetNextLine = false;
            }

            var next = pattern.Substring(currentLocation, 1);

            currentLocation++;
            Column++;

            if (next == "\n")
            {
                resetNextLine = true;
            }

            return next;
        }

        public string Next(int length)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                sb.Append(Next());
            }

            return sb.ToString();
        }

        public string Peek()
        {
            if (IsEmpty) return string.Empty;

            return pattern.Substring(currentLocation, 1);
        }

        public string Peek(int length)
        {
            if (IsEmpty) return string.Empty;

            var different = (currentLocation + length) - patternLength;
            if (different > 0) length -= different;

            if (length < 1) return string.Empty;

            return pattern.Substring(currentLocation, length);
        }
    }
}