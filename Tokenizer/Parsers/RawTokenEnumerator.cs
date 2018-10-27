namespace Tokens
{
    internal class RawTokenEnumerator
    {
        private string pattern;
        private int currentLocation;
        private int patternLength;

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

        public string Next()
        {
            if (IsEmpty) return string.Empty;

            var next = pattern.Substring(currentLocation, 1);

            currentLocation++;

            if (next == "\r" && IsEmpty == false)
            {
                var peek = pattern.Substring(currentLocation, 1);

                if (peek == "\n")
                {
                    next = "\n";
                    currentLocation++;
                }
            }

            return next;
        }

        public string Peek()
        {
            if (IsEmpty) return string.Empty;

            return pattern.Substring(currentLocation, 1);
        }
    }
}