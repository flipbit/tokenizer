using System;
using System.Collections.Generic;
using Tokens.Exceptions;

namespace Tokens
{
    public class FlatTokenParser
    {
        public IList<FlatToken> Parse(string pattern)
        {
            var tokens = new List<FlatToken>();

            var enumerator = new FlatTokenEnumerator(pattern);

            if (enumerator.IsEmpty)
            {
                return tokens;
            }

            var state = FlatTokenParserState.InPreamble;
            var token = new FlatToken();

            foreach (var c in pattern)
            {
                switch (state)
                {
                    case FlatTokenParserState.InPreamble:
                        ParsePreamble(tokens, ref token, enumerator, ref state);
                        break;

                    case FlatTokenParserState.InTokenName:
                        ParseTokenName(tokens, ref token, enumerator, ref state);
                        break;

                    default:
                        throw new TokenizerException($"Unknown FlatTokenParserState: {state}");
                }
            }

            return tokens;
        }

        private void ParsePreamble(IList<FlatToken> tokens, ref FlatToken token, FlatTokenEnumerator enumerator, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "{":
                    state = FlatTokenParserState.InTokenName;
                    break;

                default:
                    if (token.Preamble == null) token.Preamble = string.Empty;
                    token.Preamble += next;
                    break;
            }
        }

        private void ParseTokenName(IList<FlatToken> tokens, ref FlatToken token, FlatTokenEnumerator enumerator, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "{":
                    throw new TokenizerException("Unexpected character in token name: '{'"); 

                case "}":
                    tokens.Add(token);
                    token = new FlatToken();
                    state = FlatTokenParserState.InPreamble;
                    break;

                default:
                    if (token.Name == null) token.Name = string.Empty;
                    token.Name += next;
                    break;
            }
        }
    }

    internal class FlatTokenEnumerator
    {
        private string pattern;
        private int currentLocation;
        private int patternLength;

        public FlatTokenEnumerator(string pattern)
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

            return next;
        }

        public string Peek()
        {
            if (IsEmpty) return string.Empty;

            return pattern.Substring(currentLocation, 1);
        }
    }

    internal enum FlatTokenParserState
    {
        InPreamble,
        InTokenName,
    }
}
