using System.Collections.Generic;
using Tokens.Exceptions;

namespace Tokens
{
    public class RawTokenParser
    {
        private const string ValidTokenNameCharacters = @"abcdefghijklmnopqrstuvwxyzABCDDEFGHIJKLMNOPQRSTUVWXYZ1234567890_.";

        public IList<RawToken> Parse(string pattern)
        {
            var tokens = new List<RawToken>();

            var enumerator = new RawTokenEnumerator(pattern);

            if (enumerator.IsEmpty)
            {
                return tokens;
            }

            var state = FlatTokenParserState.InPreamble;
            var token = new RawToken();
            var decorator = new RawTokenDecorator();
            var argument = string.Empty;

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

                    case FlatTokenParserState.InDecorator:
                        ParseDecorator(tokens, ref token, enumerator, ref state, ref decorator);
                        break;

                    case FlatTokenParserState.InDecoratorArgument:
                        ParseDecoratorArgument(tokens, ref token, enumerator, ref state, ref decorator, ref argument);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentSingleQuotes:
                        ParseDecoratorArgumentInSingleQuotes(tokens, ref token, enumerator, ref state, ref decorator, ref argument);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentDoubleQuotes:
                        ParseDecoratorArgumentInDoubleQuotes(tokens, ref token, enumerator, ref state, ref decorator, ref argument);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentRunOff:
                        ParseDecoratorArgumentRunOff(tokens, ref token, enumerator, ref state, ref decorator, ref argument);
                        break;


                    default:
                        throw new TokenizerException($"Unknown FlatTokenParserState: {state}");
                }
            }

            if (string.IsNullOrEmpty(token.Preamble) == false)
            {
                tokens.Add(token);
            }

            return tokens;
        }

        private void ParsePreamble(IList<RawToken> tokens, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state)
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

        private void ParseTokenName(IList<RawToken> tokens, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();
            var peek = enumerator.Peek();

            switch (next)
            {
                case "{":
                    throw new TokenizerException("Unexpected character in token name: '{'"); 

                case "}":
                    tokens.Add(token);
                    token = new RawToken();
                    state = FlatTokenParserState.InPreamble;
                    break;

                case "$":
                    token.TerminateOnNewline = true;
                    switch (peek)
                    {
                        case "?":
                        case "#":
                        case "}":
                            break;

                        default:
                            throw new TokenizerException($"Invalid character in token name: '{peek}'");
                    }
                    break;

                case "?":
                    token.Optional = true;
                    switch (peek)
                    {
                        case "$":
                        case "#":
                        case "}":
                            break;

                        default:
                            throw new TokenizerException($"Invalid character in token name: '{peek}'");
                    }
                    break;

                case "#":
                    token.Repeating = true;
                    token.Optional = true;
                    switch (peek)
                    {
                        case "$":
                        case "?":
                        case "}":
                            break;

                        default:
                            throw new TokenizerException($"Invalid character in token name: '{peek}'");
                    }
                    break;

                case ":":
                    state = FlatTokenParserState.InDecorator;
                    break;

                default:
                    if (token.Name == null) token.Name = string.Empty;
                    if (ValidTokenNameCharacters.Contains(next))
                    {
                        token.Name += next;
                    }
                    else
                    {
                        throw new TokenizerException($"Invalid character '{next}' in token name.");
                    }
                    break;
            }
        }

        private void ParseDecorator(IList<RawToken> tokens, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator)
        {
            var next = enumerator.Next();

            if (string.IsNullOrWhiteSpace(next)) return;

            switch (next)
            {
                case "}":
                    token.Decorators.Add(decorator);
                    tokens.Add(token);
                    token = new RawToken();
                    decorator = new RawTokenDecorator();
                    state = FlatTokenParserState.InPreamble;
                    break;

                case ",":
                    token.Decorators.Add(decorator);
                    decorator = new RawTokenDecorator();
                    break;

                case "(":
                    state = FlatTokenParserState.InDecoratorArgument;
                    break;

                default:
                    decorator.AppendName(next);
                    break;
            }

        }

        private void ParseDecoratorArgument(IList<RawToken> tokens, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator, ref string argument)
        {
            var next = enumerator.Next();

            if (string.IsNullOrWhiteSpace(argument) &&
                string.IsNullOrWhiteSpace(next))
            {
                return;
            }

            switch (next)
            {
                case ")":
                    decorator.Args.Add(argument.Trim());
                    argument = string.Empty;
                    state = FlatTokenParserState.InDecorator;
                    break;

                case "'":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        argument = string.Empty;
                        state = FlatTokenParserState.InDecoratorArgumentSingleQuotes;
                    }
                    else
                    {
                        argument += next;
                    }
                    break;

                case @"""":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        argument = string.Empty;
                        state = FlatTokenParserState.InDecoratorArgumentDoubleQuotes;
                    }
                    else
                    {
                        argument += next;
                    }
                    break;

                case ",":
                    decorator.Args.Add(argument.Trim());
                    argument = string.Empty;
                    state = FlatTokenParserState.InDecoratorArgument;
                    break;

                default:
                    argument += next;
                    break;
            }

        }

        private void ParseDecoratorArgumentInSingleQuotes(IList<RawToken> tokens, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator, ref string argument)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "'":
                    decorator.Args.Add(argument);
                    argument = string.Empty;
                    state = FlatTokenParserState.InDecoratorArgumentRunOff;
                    break;

                default:
                    argument += next;
                    break;
            }
        }

        private void ParseDecoratorArgumentInDoubleQuotes(IList<RawToken> tokens, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator, ref string argument)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case @"""":
                    decorator.Args.Add(argument);
                    argument = string.Empty;
                    state = FlatTokenParserState.InDecoratorArgumentRunOff;
                    break;

                default:
                    argument += next;
                    break;
            }
        }

        private void ParseDecoratorArgumentRunOff(IList<RawToken> tokens, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator, ref string argument)
        {
            var next = enumerator.Next();

            if (string.IsNullOrWhiteSpace(next)) return;

            switch (next)
            {
                case ",":
                    state = FlatTokenParserState.InDecoratorArgument;
                    break;

                case ")":
                    state = FlatTokenParserState.InDecorator;
                    break;

                default:
                    throw new TokenizerException($"Unexpected character: '{next}'"); 
            }

        }
    }

    internal enum FlatTokenParserState
    {
        InPreamble,
        InTokenName,
        InDecorator,
        InDecoratorArgument,
        InDecoratorArgumentSingleQuotes,
        InDecoratorArgumentDoubleQuotes,
        InDecoratorArgumentRunOff

    }
}
