using System;
using System.Linq;
using System.Text;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Parsers
{
    /// <summary>
    /// Performs an initial pass over a template input string to create a <see cref="PreTemplate"/>.
    /// This can then be used to create a <see cref="Template"/> that can be used to parse strings
    /// into objects.
    /// </summary>
    internal class PreTokenParser
    {
        private const string ValidTokenNameCharacters = @"abcdefghijklmnopqrstuvwxyzABCDDEFGHIJKLMNOPQRSTUVWXYZ1234567890_.";

        /// <summary>
        /// Parses the template string and constructs a <see cref="PreTemplate"/>.
        /// </summary>
        public PreTemplate Parse(string template)
        {
            return Parse(template, TokenizerOptions.Defaults);
        }

        /// <summary>
        /// Parses the template string and constructs a <see cref="PreTemplate"/>.
        /// </summary>
        public PreTemplate Parse(string template, TokenizerOptions options)
        {
            var preTemplate = new PreTemplate { Options = options.Clone() };

            var enumerator = new PreTokenEnumerator(template);

            if (enumerator.IsEmpty)
            {
                return preTemplate;
            }

            var state = FlatTokenParserState.AtStart;
            var token = new PreToken();
            var decorator = new PreTokenDecorator();
            var argument = string.Empty;
            var tokenContent = new StringBuilder();
            var frontMatterName = new StringBuilder();
            var frontMatterValue = new StringBuilder();
            var inFrontMatterToken = false;

            // Basic State Machine to parse the template input
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
                        ParseFrontMatterOption(enumerator, ref frontMatterName, ref state, ref inFrontMatterToken, ref token);
                        break;

                    case FlatTokenParserState.InFrontMatterOptionValue:
                        ParseFrontMatterOptionValue(preTemplate, enumerator, ref frontMatterName, ref frontMatterValue, ref state);
                        break;

                    case FlatTokenParserState.InPreamble:
                        ParsePreamble(ref token, enumerator, ref state, ref tokenContent);
                        break;

                    case FlatTokenParserState.InTokenName:
                        ParseTokenName(preTemplate, ref token, enumerator, ref state, ref inFrontMatterToken, ref tokenContent, preTemplate.Options);
                        break;

                    case FlatTokenParserState.InTokenValue:
                        ParseTokenValue(preTemplate, ref token, enumerator, ref state, ref inFrontMatterToken, ref tokenContent, preTemplate.Options);
                        break;

                    case FlatTokenParserState.InTokenValueSingleQuotes:
                        ParseTokenValueInSingleQuotes(enumerator,  ref token, ref state, ref tokenContent);
                        break;

                    case FlatTokenParserState.InTokenValueDoubleQuotes:
                        ParseTokenValueInDoubleQuotes(enumerator,  ref token, ref state, ref tokenContent);
                        break;

                    case FlatTokenParserState.InTokenValueRunOff:
                        ParseTokenValueRunOff(enumerator, ref preTemplate, ref token, ref state, ref inFrontMatterToken, ref tokenContent, preTemplate.Options);
                        break;

                    case FlatTokenParserState.InDecorator:
                        ParseDecorator(preTemplate, ref token, enumerator, ref state, ref decorator, ref inFrontMatterToken, ref tokenContent, preTemplate.Options);
                        break;

                    case FlatTokenParserState.InDecoratorArgument:
                        ParseDecoratorArgument(enumerator, ref state, ref decorator, ref argument, ref tokenContent);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentSingleQuotes:
                        ParseDecoratorArgumentInSingleQuotes(enumerator, ref state, ref decorator, ref argument, ref tokenContent);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentDoubleQuotes:
                        ParseDecoratorArgumentInDoubleQuotes(enumerator, ref state, ref decorator, ref argument, ref tokenContent);
                        break;

                    case FlatTokenParserState.InDecoratorArgumentRunOff:
                        ParseDecoratorArgumentRunOff(enumerator, ref state, ref tokenContent);
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
                AppendToken(preTemplate, token, ref tokenContent, preTemplate.Options);
            }

            return preTemplate;
        }

        private void ParseStart(PreTokenEnumerator enumerator, ref FlatTokenParserState state)
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

        private void ParseFrontMatter(PreTokenEnumerator enumerator, ref StringBuilder frontMatterName, ref FlatTokenParserState state)
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
                enumerator.Next(5);
                return;
            }

            var next = enumerator.Next();

            switch (next)
            {
                case "#":
                    state = FlatTokenParserState.InFrontMatterComment;
                    break;

                case "\n":
                case "\r":
                    break;

                default:
                    state = FlatTokenParserState.InFrontMatterOption;
                    frontMatterName.Append(next);
                    break;
            }
        }

        private void ParseFrontMatterOption(PreTokenEnumerator enumerator, ref StringBuilder frontMatterName, ref FlatTokenParserState state, ref bool inFrontMatterToken, ref PreToken token)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case ":":
                    if (frontMatterName.ToString().Trim().ToLowerInvariant() == "set")
                    {
                        inFrontMatterToken = true;
                        frontMatterName.Clear();
                        token.Location = enumerator.Location.Clone(); 
                        state = FlatTokenParserState.InTokenName;
                    }
                    else
                    {
                        state = FlatTokenParserState.InFrontMatterOptionValue;
                    }

                    break;

                default:
                    frontMatterName.Append(next);
                    break;
            }
        }

        private void ParseFrontMatterOptionValue(PreTemplate template, PreTokenEnumerator enumerator, ref StringBuilder frontMatterName, ref StringBuilder frontMatterValue, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "\n":
                    var rawName = frontMatterName.ToString().Trim();
                    var name = frontMatterName.ToString().Trim().ToLowerInvariant();
                    var value = frontMatterValue.ToString().Trim().ToLowerInvariant();

                    switch (name)
                    {
                        case "trimleadingwhitespace":
                            var trimLeadingWhitespaceInTokenPreamble = ConvertFrontMatterOptionToBool(value, rawName, enumerator);
                            template.Options.TrimLeadingWhitespaceInTokenPreamble = trimLeadingWhitespaceInTokenPreamble;
                            break;
                        case "trimtrailingwhitespace":
                            var trimTrailingWhiteSpace = ConvertFrontMatterOptionToBool(value, rawName, enumerator);
                            template.Options.TrimTrailingWhiteSpace = trimTrailingWhiteSpace;
                            break;
                        case "trimpreamblebeforenewline":
                            var trimPreambleBeforeNewLine = ConvertFrontMatterOptionToBool(value, rawName, enumerator);
                            template.Options.TrimPreambleBeforeNewLine = trimPreambleBeforeNewLine;
                            break;
                        case "outoforder":
                            var outOfOrderTokens = ConvertFrontMatterOptionToBool(value, rawName, enumerator);
                            template.Options.OutOfOrderTokens = outOfOrderTokens;
                            break;
                        case "terminateonnewline":
                            var terminateOnNewline = ConvertFrontMatterOptionToBool(value, rawName, enumerator);
                            template.Options.TerminateOnNewline = terminateOnNewline;
                            break;
                        case "ignoremissingproperties":
                            var ignoreMissingProperties = ConvertFrontMatterOptionToBool(value, rawName, enumerator);
                            template.Options.IgnoreMissingProperties = ignoreMissingProperties;
                            break;
                        case "name":
                            template.Name = frontMatterValue.ToString().Trim();
                            break;
                        case "hint":
                            template.Hints.Add(new Hint
                            {
                                Text = frontMatterValue.ToString().Trim(),
                                Optional = false
                            }); 
                            break;
                        case "hint?":
                            template.Hints.Add(new Hint
                            {
                                Text = frontMatterValue.ToString().Trim(),
                                Optional = true
                            }); 
                            break;
                        case "casesensitive":
                            var caseSensitive = ConvertFrontMatterOptionToBool(value, rawName, enumerator);
                            if (caseSensitive)
                            {
                                template.Options.TokenStringComparison = StringComparison.InvariantCulture;
                            }
                            else
                            {
                                template.Options.TokenStringComparison = StringComparison.InvariantCultureIgnoreCase;
                            }
                            break;
                        case "tag":
                            template.Tags.Add(frontMatterValue.ToString().Trim());
                            break;

                        default:
                            throw new ParsingException($"Unknown front matter option: {rawName}", enumerator);
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

        private bool ConvertFrontMatterOptionToBool(string input, string rawName, PreTokenEnumerator enumerator)
        {
            if (bool.TryParse(input, out var asBool))
            {
                return asBool;
            }

            throw new ParsingException($"Unable to convert front matter option to boolean: {rawName}", enumerator);
        }

        private void ParseFrontMatterComment(PreTokenEnumerator enumerator, ref FlatTokenParserState state)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "\n":
                    state = FlatTokenParserState.InFrontMatter;
                    break;
            }
        }

        private void ParsePreamble(ref PreToken token, PreTokenEnumerator enumerator, ref FlatTokenParserState state, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "{":
                    if (enumerator.Peek() == "{")
                    {
                        token.AppendPreamble("{");
                        enumerator.Next();
                    }
                    else
                    {
                        token.Location = enumerator.Location.Clone();
                        tokenContent.Append("{");

                        state = FlatTokenParserState.InTokenName;
                    }
                    break;

                case "}":
                    if (enumerator.Peek() == "}")
                    {
                        token.AppendPreamble("}");
                        enumerator.Next();
                        break;
                    }
                    throw new ParsingException($"Unescaped character '}}' in template.", enumerator); 


                default:
                    token.AppendPreamble(next);
                    break;
            }
        }

        private void ParseTokenName(PreTemplate template, ref PreToken token, PreTokenEnumerator enumerator, ref FlatTokenParserState state, ref bool inFrontMatterToken, ref StringBuilder tokenContent, TokenizerOptions options)
        {
            var next = enumerator.Next();
            var peek = enumerator.Peek();
            tokenContent.Append(next);

            switch (next)
            {
                case "{":
                    throw new ParsingException($"Unexpected character '{{' in token '{token.Name}'", enumerator); 

                case "}":
                    if (inFrontMatterToken)
                    {
                        throw new ParsingException($"Invalid character '{next}' in token '{token.Name}'", enumerator);
                    }
                    else
                    {
                        AppendToken(template, token, ref tokenContent, options);
                        token = new PreToken();
                        state = FlatTokenParserState.InPreamble;
                    }
                    break;

                case "$":
                    token.TerminateOnNewline = true;
                    switch (peek)
                    {
                        case " ":
                        case "?":
                        case "*":
                        case "}":
                        case ":":
                        case "!":
                            break;

                        default:
                            throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
                    }
                    break;

                case "?":
                    token.Optional = true;
                    switch (peek)
                    {
                        case " ":
                        case "$":
                        case "*":
                        case "}":
                        case ":":
                        case "!":
                            break;

                        default:
                            throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
                    }

                    if (token.Required) throw new ParsingException($"Required token {token.Name} can't be Optional", enumerator);

                    break;

                case "*":
                    token.Repeating = true;
                    token.Optional = true;
                    switch (peek)
                    {
                        case " ":
                        case "$":
                        case "?":
                        case "}":
                        case ":":
                        case "!":
                            break;

                        default:
                            throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
                    }
                    break;

                case "!":
                    token.Required = true;
                    switch (peek)
                    {
                        case " ":
                        case "*":
                        case "$":
                        case "?":
                        case "}":
                        case ":":
                            break;

                        default:
                            throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
                    }

                    if (token.Optional) throw new ParsingException($"Optional token {token.Name} can't be Required", enumerator);

                    break;

                case ":":
                    state = FlatTokenParserState.InDecorator;
                    break;

                case "=":
                    state = FlatTokenParserState.InTokenValue;
                    break;

                case " ":
                    switch (peek)
                    {
                        case " ":
                        case "*":
                        case "$":
                        case "?":
                        case "}":
                        case ":":
                        case "!":
                        case "=":
                            break;

                        case "\n" when inFrontMatterToken:
                            break;

                        default:
                            if (string.IsNullOrWhiteSpace(token.Name) == false)
                            {
                                throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
                            }
                            break;
                    }

                    break;

                case "\n":
                    if (inFrontMatterToken)
                    {
                        token.IsFrontMatterToken = true;
                        AppendToken(template, token, ref tokenContent, options);
                        token = new PreToken();
                        inFrontMatterToken = false;
                        state = FlatTokenParserState.InFrontMatter;
                    }
                    else
                    {
                        throw new ParsingException($"Invalid character '{next}' in token '{token.Name}'", enumerator);
                    }
                    break;

                default:
                    if (ValidTokenNameCharacters.Contains(next))
                    {
                        token.AppendName(next);
                    }
                    else
                    {
                        throw new ParsingException($"Invalid character '{next}' in token '{token.Name}'", enumerator);
                    }
                    break;
            }
        }
        
        private void ParseTokenValue(PreTemplate template, ref PreToken token, PreTokenEnumerator enumerator, ref FlatTokenParserState state, ref bool inFrontMatterToken, ref StringBuilder tokenContent, TokenizerOptions options)
        {
            var next = enumerator.Next();
            var peek = enumerator.Peek();

            tokenContent.Append(next);

            switch (next)
            {
                case "{":
                    throw new ParsingException($"Unexpected character '{{' in token '{token.Name}'", enumerator); 

                case "}" when inFrontMatterToken == false:
                case "\n" when inFrontMatterToken:
                    token.IsFrontMatterToken = inFrontMatterToken;
                    AppendToken(template, token, ref tokenContent, options);
                    token = new PreToken();
                    if (inFrontMatterToken)
                    {
                        inFrontMatterToken = false;
                        state = FlatTokenParserState.InFrontMatter;
                    }
                    else
                    {
                        state = FlatTokenParserState.InPreamble;
                    }
                    break;

                case ":":
                    state = FlatTokenParserState.InDecorator;
                    break;

                case "'":
                    state = FlatTokenParserState.InTokenValueSingleQuotes;
                    break;

                case "\"":
                    state = FlatTokenParserState.InTokenValueDoubleQuotes;
                    break;

                case " ":
                    switch (peek)
                    {
                        case " ":
                        case "}" when inFrontMatterToken == false:
                        case "\n" when inFrontMatterToken:
                        case ":":
                           break;

                        default:
                            if (token.HasValue)
                            {
                                throw new ParsingException($"Invalid character '{peek}' in token '{token.Name}'", enumerator);
                            }
                            break;
                    }

                    break;

                case "}" when inFrontMatterToken:
                case "\n" when inFrontMatterToken == false:
                    throw  new ParsingException($"'{token.Name}' unexpected character: {next}", enumerator);

                default:
                    token.AppendValue(next);
                    break;
            }
        }

        private void ParseTokenValueInSingleQuotes(PreTokenEnumerator enumerator, ref PreToken token, ref FlatTokenParserState state, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "'":
                    state = FlatTokenParserState.InTokenValueRunOff;
                    break;

                default:
                    token.AppendValue(next);
                    break;
            }

            tokenContent.Append(next);
        }

        private void ParseTokenValueInDoubleQuotes(PreTokenEnumerator enumerator, ref PreToken token, ref FlatTokenParserState state, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case @"""":
                    state = FlatTokenParserState.InTokenValueRunOff;
                    break;

                default:
                    token.AppendValue(next);
                    break;
            }

            tokenContent.Append(next);
        }

        private void ParseTokenValueRunOff(PreTokenEnumerator enumerator, ref PreTemplate template, ref PreToken token, ref FlatTokenParserState state, ref bool inFrontMatterToken, ref StringBuilder tokenContent, TokenizerOptions options)
        {
            var next = enumerator.Next();
            tokenContent.Append(next);

            if (string.IsNullOrWhiteSpace(next))
            {
                if (inFrontMatterToken == false) return;
                if (next != "\n") return;
            }

            switch (next)
            {
                case ":":
                    state = FlatTokenParserState.InDecorator;
                    break;

                case "}" when inFrontMatterToken == false:
                case "\n" when inFrontMatterToken:
                    token.IsFrontMatterToken = inFrontMatterToken;
                    AppendToken(template, token, ref tokenContent, options);
                    token = new PreToken();
                    if (inFrontMatterToken)
                    {
                        inFrontMatterToken = false;
                        state = FlatTokenParserState.InFrontMatter;
                    }
                    else
                    {
                        state = FlatTokenParserState.InPreamble;
                    }
                    break;

                default:
                    throw new TokenizerException($"Unexpected character: '{next}'"); 
            }
        }

        private void ParseDecorator(PreTemplate template, ref PreToken token, PreTokenEnumerator enumerator, ref FlatTokenParserState state, ref PreTokenDecorator decorator, ref bool inFrontMatterToken, ref StringBuilder tokenContent, TokenizerOptions options)
        {
            var next = enumerator.Next();

            tokenContent.Append(next);

            if (string.IsNullOrWhiteSpace(next))
            {
                if (inFrontMatterToken == false) return;
                if (next != "\n") return;
            }

            switch (next)
            {
                case "}" when inFrontMatterToken == false:
                case "\n" when inFrontMatterToken:
                    token.IsFrontMatterToken = inFrontMatterToken;
                    AppendDecorator(enumerator, token, decorator);
                    AppendToken(template, token, ref tokenContent, options);
                    token = new PreToken();
                    decorator = new PreTokenDecorator();
                    if (inFrontMatterToken)
                    {
                        inFrontMatterToken = false;
                        state = FlatTokenParserState.InFrontMatter;
                    }
                    else
                    {
                        state = FlatTokenParserState.InPreamble;
                    }
                    break;

                case ",":
                    AppendDecorator(enumerator, token, decorator);
                    decorator = new PreTokenDecorator();
                    break;

                case "(":
                    state = FlatTokenParserState.InDecoratorArgument;
                    break;

                case "}" when inFrontMatterToken:
                case "\n" when inFrontMatterToken == false:
                    throw  new ParsingException($"'{decorator.Name}' unexpected character: {next}", enumerator);

                case "!":
                    if (string.IsNullOrWhiteSpace(decorator.Name))
                    {
                        decorator.IsNotDecorator = true;
                    }
                    else
                    {
                        throw  new ParsingException($"'{decorator.Name}' unexpected character: {next}", enumerator);
                    }
                    break;

                default:
                    decorator.AppendName(next);
                    break;
            }

        }

        private void ParseDecoratorArgument(PreTokenEnumerator enumerator, ref FlatTokenParserState state, ref PreTokenDecorator decorator, ref string argument, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();
            tokenContent.Append(next);

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

        private void ParseDecoratorArgumentInSingleQuotes(PreTokenEnumerator enumerator, ref FlatTokenParserState state, ref PreTokenDecorator decorator, ref string argument, ref StringBuilder tokenContent)
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

            tokenContent.Append(next);
        }

        private void ParseDecoratorArgumentInDoubleQuotes(PreTokenEnumerator enumerator, ref FlatTokenParserState state, ref PreTokenDecorator decorator, ref string argument, ref StringBuilder tokenContent)
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
            
            tokenContent.Append(next);
        }

        private void ParseDecoratorArgumentRunOff(PreTokenEnumerator enumerator, ref FlatTokenParserState state, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();
            tokenContent.Append(next);

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

        private void AppendToken(PreTemplate template, PreToken token, ref StringBuilder tokenContent, TokenizerOptions options)
        {
            token.Content = tokenContent.ToString();
            token.Id = template.Tokens.Count + 1;
            token.IsNull = string.Compare(token.Name, "null", StringComparison.InvariantCultureIgnoreCase) == 0;

            if (options.TrimPreambleBeforeNewLine)
            {
                token.TrimPreambleBeforeNewLine();
            }

            if (options.TerminateOnNewline)
            {
                token.TerminateOnNewline = true;
            }

            tokenContent.Clear();

            var preamble = GetRepeatingMultilinePreamble(token);

            if (string.IsNullOrEmpty(preamble) == false && token.Repeating)
            {
                token.Repeating = false;
                template.Tokens.Add(token);

                var repeat = new PreToken
                {
                    Optional = true,
                    Repeating = true,
                    TerminateOnNewline = token.TerminateOnNewline,
                    Content = token.Content
                };

                repeat.AppendName(token.Name);
                repeat.AppendPreamble(preamble);
                repeat.AppendDecorators(token.Decorators);

                repeat.Id = template.Tokens.Count + 1;
                repeat.DependsOnId = token.Id;
                template.Tokens.Add(repeat);
            }
            else
            {
                template.Tokens.Add(token);
            }
        }

        private void AppendDecorator(PreTokenEnumerator enumerator, PreToken token, PreTokenDecorator decorator)
        {
            if (decorator == null) return;
            if (string.IsNullOrEmpty(decorator.Name)) return;

            switch (decorator.Name.ToLowerInvariant())
            {
                case "eol":
                case "$":
                    if (decorator.Args.Any()) throw  new ParsingException($"'{decorator.Name}' decorator does not take any arguments", enumerator);
                    token.TerminateOnNewline = true;
                    break;

                case "optional":
                case "?":
                    if (decorator.Args.Any()) throw  new ParsingException($"'{decorator.Name}' decorator does not take any arguments", enumerator);
                    token.Optional = true;
                    break;

                case "repeating":
                case "*":
                    if (decorator.Args.Any()) throw  new ParsingException($"'{decorator.Name}' decorator does not take any arguments", enumerator);
                    token.Repeating = true;
                    break;

                case "required":
                case "!":
                    if (decorator.Args.Any()) throw  new ParsingException($"'{decorator.Name}' decorator does not take any arguments", enumerator);
                    token.Required = true;
                    break;

                default:
                    token.Decorators.Add(decorator);
                    break;
            }
        }

        private string GetRepeatingMultilinePreamble(PreToken token)
        {
            if (token.Repeating == false) return null;
            if (string.IsNullOrEmpty(token.Preamble)) return null;
            if (token.Preamble.IndexOf('\n') == -1) return null;

            var pre = token.Preamble.SubstringBeforeLastString("\n");
            var post = token.Preamble.SubstringAfterLastString("\n");

            if (string.IsNullOrWhiteSpace(pre) == false &&
                string.IsNullOrWhiteSpace(post))
            {
                return "\n" + post;
            }

            return null;
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
        InDecoratorArgumentRunOff,
        InTokenValue,
        InTokenValueSingleQuotes,
        InTokenValueDoubleQuotes,
        InTokenValueRunOff
    }
}
