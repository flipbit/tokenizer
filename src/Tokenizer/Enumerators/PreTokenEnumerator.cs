using System.Text;

namespace Tokens.Enumerators
{
    internal class PreTokenEnumerator
    {
        private readonly string pattern;
        private readonly int patternLength;

        private int currentLocation;
        private bool resetNextLine;

        public PreTokenEnumerator(string pattern)
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
            Location = new FileLocation();
        }

        public bool IsEmpty => currentLocation >= patternLength;

        public FileLocation Location { get; }

        public string Next()
        {
            if (IsEmpty) return string.Empty;

            var next = pattern.Substring(currentLocation, 1);
            currentLocation++;

            if (resetNextLine)
            {
                Location.NewLine();
                resetNextLine = false;
            }
            else
            {
                Location.Increment(next);
            }

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