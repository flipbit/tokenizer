using System.Text;

namespace Tokens.Enumerators
{
    internal class RawTokenEnumerator
    {
        private string pattern;
        private int currentLocation;
        private int patternLength;

        private bool resetNextLine = true;

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
        }

        public bool IsEmpty => currentLocation >= patternLength;

        public int Line { get; private set; }

        public int Character { get; private set; }

        public string Next()
        {
            if (IsEmpty) return string.Empty;

            if (resetNextLine)
            {
                Line++;
                Character = 1;
                resetNextLine = false;
            }

            var next = pattern.Substring(currentLocation, 1);

            currentLocation++;
            Character++;

            if (next == "\r" && IsEmpty == false)
            {
                var peek = pattern.Substring(currentLocation, 1);

                if (peek == "\n")
                {
                    next = "\n";
                    currentLocation++;
                    resetNextLine = true;
                }
            }
            else if (next == "\n")
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