using System;
using System.Collections.Generic;
using System.Text;
using Tokens.Enumerators;
using Tokens.Exceptions;

namespace Tokens.Parsers
{
    internal class RawTokenParser
    {
        private const string ValidTokenNameCharacters = @"abcdefghijklmnopqrstuvwxyzABCDDEFGHIJKLMNOPQRSTUVWXYZ1234567890_.";

        public RawTemplate Parse(string pattern)
        {
            return Parse(pattern, TokenizerOptions.Defaults);
        }

        public RawTemplate Parse(string pattern, TokenizerOptions options)
        {
            var template = new RawTemplate();
            template.Options = options.Clone();

            var enumerator = new RawTokenEnumerator(pattern);

            if (enumerator.IsEmpty)
            {
                return template;
            }

            var state = FlatTokenParserState.AtStart;
            var token = new RawToken();
            var decorator = new RawTokenDecorator();
            var argument = string.Empty;
            var frontMatterName = new StringBuilder();
            var frontMatterValue = new StringBuilder();

            while (enumerator.IsEmpty == false)
            {
                switch (state)
                {
                    case FlatTokenParserState.AtStart:
                        ParseStart(enumerator, ref state);
                        break;

                    case FlatTokenParserState.InFrontMatter:
                        ParseFrontMatter(enumerator, ref frontMatterName, ref state);
                        break;

                    case FlatTokenParserState.InFrontMatterComment:
                        ParseFrontMatterComment(enumerator, ref state);
                        break;

                    case FlatTokenParserState.InFrontMatterOption:
                        ParseFrontMatterOption(enumerator, ref frontMatterName, ref state);
                        break;

                    case FlatTokenParserState.InFrontMatterOptionValue:
                        ParseFrontMatterOptionValue(template, enumerator, ref frontMatterName, ref frontMatterValue, ref state);
                        break;

                    case FlatTokenParserState.InPreamble:
                        ParsePreamble(ref token, enumerator, ref state);
                        break;

                    case FlatTokenParserState.InTokenName:
                        ParseTokenName(template, ref token, enumerator, ref state);
                        break;

                    case FlatTokenParserState.InDecorator:
                        ParseDecorator(template, ref token, enumerator, ref state, ref decorator);
                        break;

                    case FlatTokenParserState.InDecoratorArgument:
                        ParseDecoratorArgument(enumerator, ref state, ref decorator, ref argument);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentSingleQuotes:
                        ParseDecoratorArgumentInSingleQuotes(enumerator, ref state, ref decorator, ref argument);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentDoubleQuotes:
                        ParseDecoratorArgumentInDoubleQuotes(enumerator, ref state, ref decorator, ref argument);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentRunOff:
                        ParseDecoratorArgumentRunOff(enumerator, ref state, ref decorator, ref argument);
                        break;


                    default:
                        throw new TokenizerException($"Unknown FlatTokenParserState: {state}");
                }
            }

            // Append current token if it has contents
            // Note: allow empty token values, as these will serve to truncate the last 
            // token in the template
            if (string.IsNullOrWhiteSpace(token.Preamble) == false)
            {
                template.Tokens.Add(token);
            }

            return template;
        }

        private void ParseStart(RawTokenEnumerator enumerator, ref FlatTokenParserState state)
        {
            var peek = enumerator.Peek(4);

            if (peek == "---\n")
            {
                state = FlatTokenParserState.InFrontMatter;
                enumerator.Next(4);
                return;
            }

            peek = enumerator.Peek(5);

            if (peek == "---\r\n")
            {
                state = FlatTokenParserState.InFrontMatter;
                enumerator.Next(4); // Next() will trim /r/n
                return;
            }

            state = FlatTokenParserState.InPreamble;
        }

        private void ParseFrontMatter(RawTokenEnumerator enumerator, ref StringBuilder frontMatterName, ref FlatTokenParserState state)
        {
            var peek = enumerator.Peek(4);

            if (peek == "---\n")
            {
                state = FlatTokenParserState.InPreamble;
                enumerator.Next(4);
                return;
            }

            peek = enumerator.Peek(5);

            if (peek == "---\r\n")
            {
                state = FlatTokenParserState.InPreamble;
                enumerator.Next(4); // Next() will trim \r\n
                return;
            }

            var next = enumerator.Next();

            switch (next)
            {
                case "#":
                    state = FlatTokenParserState.InFrontMatterComment;
                    break;

                case "\n":
                    break;

                default:
                    state = FlatTokenParserState.InFrontMatterOption;
                    frontMatterName.Append(next);
                    break;
            }
        }

        private void ParseFrontMatterOption(RawTokenEnumerator enumerator, ref StringBuilder frontMatterName, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case ":":
                    state = FlatTokenParserState.InFrontMatterOptionValue;
                    break;

                default:
                    frontMatterName.Append(next);
                    break;
            }
        }

        private void ParseFrontMatterOptionValue(RawTemplate template, RawTokenEnumerator enumerator, ref StringBuilder frontMatterName, ref StringBuilder frontMatterValue, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "\n":
                    var rawName = frontMatterName.ToString().Trim();
                    var name = frontMatterName.ToString().Trim().ToLowerInvariant();
                    var value = frontMatterValue.ToString().Trim().ToLowerInvariant();

                    if (bool.TryParse(value, out var asBool))
                    {
                        switch (name)
                        {
                            case "throwexceptiononmissingproperty":
                                template.Options.ThrowExceptionOnMissingProperty = asBool;
                                break;
                            case "trimleadingwhitespace":
                                template.Options.TrimLeadingWhitespaceInTokenPreamble = asBool;
                                break;
                            case "trimtrailingwhitespace":
                                template.Options.TrimTrailingWhiteSpace = asBool;
                                break;
                            case "outoforder":
                                template.Options.OutOfOrderTokens = asBool;
                                break;
                            case "casesensitive":
                                if (asBool)
                                {
                                    template.Options.TokenStringComparison = StringComparison.InvariantCulture;
                                }
                                else
                                {
                                    template.Options.TokenStringComparison = StringComparison.InvariantCultureIgnoreCase;
                                }
                                break;

                            default:
                                throw new ParsingException($"Unknown front matter option: {rawName}", enumerator);
                        }
                    }
                    else
                    {
                        throw new ParsingException($"Unable to convert front matter option to boolean: {rawName}", enumerator);
                    }

                    frontMatterName.Clear();
                    frontMatterValue.Clear();
                    state = FlatTokenParserState.InFrontMatter;
                    break;

                default:
                    frontMatterValue.Append(next);
                    break;
            }
        }

        private void ParseFrontMatterComment(RawTokenEnumerator enumerator, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "\n":
                    state = FlatTokenParserState.InFrontMatter;
                    break;
            }
        }

        private void ParsePreamble(ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state)
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

        private void ParseTokenName(RawTemplate template, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();
            var peek = enumerator.Peek();

            switch (next)
            {
                case "{":
                    throw new ParsingException($"Unexpected character '{{' in token '{token.Name}'", enumerator); 

                case "}":
                    template.Tokens.Add(token);
                    token = new RawToken();
                    state = FlatTokenParserState.InPreamble;
                    break;

                case "$":
                    token.TerminateOnNewline = true;
                    switch (peek)
                    {
                        case "?":
                        case "*":
                        case "}":
                        case ":":
                            break;

                        default:
                            throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
                    }
                    break;

                case "?":
                    token.Optional = true;
                    switch (peek)
                    {
                        case "$":
                        case "*":
                        case "}":
                        case ":":
                            break;

                        default:
                            throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
                    }
                    break;

                case "*":
                    token.Repeating = true;
                    token.Optional = true;
                    switch (peek)
                    {
                        case "$":
                        case "?":
                        case "}":
                        case ":":
                            break;

                        default:
                            throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
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
                        throw new ParsingException($"Invalid character '{next}' in token '{token.Name}'", enumerator);
                    }
                    break;
            }
        }

        private void ParseDecorator(RawTemplate template, ref RawToken token, RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator)
        {
            var next = enumerator.Next();

            if (string.IsNullOrWhiteSpace(next)) return;

            switch (next)
            {
                case "}":
                    token.Decorators.Add(decorator);
                    template.Tokens.Add(token);
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

        private void ParseDecoratorArgument(RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator, ref string argument)
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

        private void ParseDecoratorArgumentInSingleQuotes(RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator, ref string argument)
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

        private void ParseDecoratorArgumentInDoubleQuotes(RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator, ref string argument)
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

        private void ParseDecoratorArgumentRunOff(RawTokenEnumerator enumerator, ref FlatTokenParserState state, ref RawTokenDecorator decorator, ref string argument)
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
        AtStart,
        InFrontMatter,
        InFrontMatterOption,
        InFrontMatterOptionValue,
        InFrontMatterComment,
        InPreamble,
        InTokenName,
        InDecorator,
        InDecoratorArgument,
        InDecoratorArgumentSingleQuotes,
        InDecoratorArgumentDoubleQuotes,
        InDecoratorArgumentRunOff

    }
}
