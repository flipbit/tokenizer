using System;
using System.Linq;
using System.Text;
using Tokens.Compilation.Definitions;
using Tokens.Enumerators;
using Tokens.Exceptions;
using Tokens.Extensions;

namespace Tokens.Compilation.Parsing
{
    /// <summary>
    /// Performs an initial pass over a template input string to create a <see cref="TemplateDefinition"/>.
    /// This can then be used to create a <see cref="Template"/> that can be used to parse strings
    /// into objects.
    /// </summary>
    internal class TemplateDefinitionParser
    {
        private const string ValidTokenNameCharacters = @"abcdefghijklmnopqrstuvwxyzABCDDEFGHIJKLMNOPQRSTUVWXYZ1234567890_.";

        /// <summary>
        /// Parses the template string and constructs a <see cref="TemplateDefinition"/>.
        /// </summary>
        public TemplateDefinition Parse(string template)
        {
            return Parse(template, TokenizerOptions.Defaults);
        }

        /// <summary>
        /// Parses the template string and constructs a <see cref="TemplateDefinition"/>.
        /// </summary>
        public TemplateDefinition Parse(string template, TokenizerOptions options)
        {
            var templateDefinition = new TemplateDefinition { Options = options.Clone() };

            var enumerator = new TemplateDefinitionEnumerator(template);

            if (enumerator.IsEmpty)
            {
                return templateDefinition;
            }

            var state = TemplateDefinitionParserState.AtStart;
            var token = new TokenDefinition();
            var decorator = new DecoratorDefinition();
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
                    case TemplateDefinitionParserState.AtStart:
                        ParseStart(enumerator, ref state);
                        break;

                    case TemplateDefinitionParserState.InFrontMatter:
                        ParseFrontMatter(enumerator, ref frontMatterName, ref state);
                        break;

                    case TemplateDefinitionParserState.InFrontMatterComment:
                        ParseFrontMatterComment(enumerator, ref state);
                        break;

                    case TemplateDefinitionParserState.InFrontMatterOption:
                        ParseFrontMatterOption(enumerator, ref frontMatterName, ref state, ref inFrontMatterToken, ref token);
                        break;

                    case TemplateDefinitionParserState.InFrontMatterOptionValue:
                        ParseFrontMatterOptionValue(templateDefinition, enumerator, ref frontMatterName, ref frontMatterValue, ref state);
                        break;

                    case TemplateDefinitionParserState.InPreamble:
                        ParsePreamble(ref token, enumerator, ref state, ref tokenContent);
                        break;

                    case TemplateDefinitionParserState.InTokenName:
                        ParseTokenName(templateDefinition, ref token, enumerator, ref state, ref inFrontMatterToken, ref tokenContent, templateDefinition.Options);
                        break;

                    case TemplateDefinitionParserState.InTokenValue:
                        ParseTokenValue(templateDefinition, ref token, enumerator, ref state, ref inFrontMatterToken, ref tokenContent, templateDefinition.Options);
                        break;

                    case TemplateDefinitionParserState.InTokenValueSingleQuotes:
                        ParseTokenValueInSingleQuotes(enumerator,  ref token, ref state, ref tokenContent);
                        break;

                    case TemplateDefinitionParserState.InTokenValueDoubleQuotes:
                        ParseTokenValueInDoubleQuotes(enumerator,  ref token, ref state, ref tokenContent);
                        break;

                    case TemplateDefinitionParserState.InTokenValueRunOff:
                        ParseTokenValueRunOff(enumerator, ref templateDefinition, ref token, ref state, ref inFrontMatterToken, ref tokenContent, templateDefinition.Options);
                        break;

                    case TemplateDefinitionParserState.InDecorator:
                        ParseDecorator(templateDefinition, ref token, enumerator, ref state, ref decorator, ref inFrontMatterToken, ref tokenContent, templateDefinition.Options);
                        break;

                    case TemplateDefinitionParserState.InDecoratorArgument:
                        ParseDecoratorArgument(enumerator, ref state, ref decorator, ref argument, ref tokenContent);
                        break;

                    case TemplateDefinitionParserState.InDecoratorArgumentSingleQuotes:
                        ParseDecoratorArgumentInSingleQuotes(enumerator, ref state, ref decorator, ref argument, ref tokenContent);
                        break;

                    case TemplateDefinitionParserState.InDecoratorArgumentDoubleQuotes:
                        ParseDecoratorArgumentInDoubleQuotes(enumerator, ref state, ref decorator, ref argument, ref tokenContent);
                        break;

                    case TemplateDefinitionParserState.InDecoratorArgumentRunOff:
                        ParseDecoratorArgumentRunOff(enumerator, ref state, ref tokenContent);
                        break;


                    default:
                        throw new TokenizerException($"Unknown TemplateDefinitionParserState: {state}");
                }
            }

            // Append current token if it has contents
            // Note: allow empty token values, as these will serve to truncate the last 
            // token in the template
            if (string.IsNullOrWhiteSpace(token.Preamble) == false)
            {
                AppendToken(templateDefinition, token, ref tokenContent, templateDefinition.Options);
            }

            return templateDefinition;
        }

        private void ParseStart(TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state)
        {
            var peek = enumerator.Peek(4);

            if (peek == "---\n")
            {
                state = TemplateDefinitionParserState.InFrontMatter;
                enumerator.Next(4);
                return;
            }

            peek = enumerator.Peek(5);

            if (peek == "---\r\n")
            {
                state = TemplateDefinitionParserState.InFrontMatter;
                enumerator.Next(4); // Next() will trim /r/n
                return;
            }

            state = TemplateDefinitionParserState.InPreamble;
        }

        private void ParseFrontMatter(TemplateDefinitionEnumerator enumerator, ref StringBuilder frontMatterName, ref TemplateDefinitionParserState state)
        {
            var peek = enumerator.Peek(4);

            if (peek == "---\n")
            {
                state = TemplateDefinitionParserState.InPreamble;
                enumerator.Next(4);
                return;
            }

            peek = enumerator.Peek(5);

            if (peek == "---\r\n")
            {
                state = TemplateDefinitionParserState.InPreamble;
                enumerator.Next(5);
                return;
            }

            var next = enumerator.Next();

            switch (next)
            {
                case "#":
                    state = TemplateDefinitionParserState.InFrontMatterComment;
                    break;

                case "\n":
                case "\r":
                    break;

                default:
                    state = TemplateDefinitionParserState.InFrontMatterOption;
                    frontMatterName.Append(next);
                    break;
            }
        }

        private void ParseFrontMatterOption(TemplateDefinitionEnumerator enumerator, ref StringBuilder frontMatterName, ref TemplateDefinitionParserState state, ref bool inFrontMatterToken, ref TokenDefinition token)
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
                        state = TemplateDefinitionParserState.InTokenName;
                    }
                    else
                    {
                        state = TemplateDefinitionParserState.InFrontMatterOptionValue;
                    }

                    break;

                default:
                    frontMatterName.Append(next);
                    break;
            }
        }

        private void ParseFrontMatterOptionValue(TemplateDefinition template, TemplateDefinitionEnumerator enumerator, ref StringBuilder frontMatterName, ref StringBuilder frontMatterValue, ref TemplateDefinitionParserState state)
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
                    state = TemplateDefinitionParserState.InFrontMatter;
                    break;

                default:
                    frontMatterValue.Append(next);
                    break;
            }
        }

        private bool ConvertFrontMatterOptionToBool(string input, string rawName, TemplateDefinitionEnumerator enumerator)
        {
            if (bool.TryParse(input, out var asBool))
            {
                return asBool;
            }

            throw new ParsingException($"Unable to convert front matter option to boolean: {rawName}", enumerator);
        }

        private void ParseFrontMatterComment(TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "\n":
                    state = TemplateDefinitionParserState.InFrontMatter;
                    break;
            }
        }

        private void ParsePreamble(ref TokenDefinition token, TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state, ref StringBuilder tokenContent)
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

                        state = TemplateDefinitionParserState.InTokenName;
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

        private void ParseTokenName(TemplateDefinition template, ref TokenDefinition token, TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state, ref bool inFrontMatterToken, ref StringBuilder tokenContent, TokenizerOptions options)
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
                        token = new TokenDefinition();
                        state = TemplateDefinitionParserState.InPreamble;
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
                    state = TemplateDefinitionParserState.InDecorator;
                    break;

                case "=":
                    state = TemplateDefinitionParserState.InTokenValue;
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
                        token = new TokenDefinition();
                        inFrontMatterToken = false;
                        state = TemplateDefinitionParserState.InFrontMatter;
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
        
        private void ParseTokenValue(TemplateDefinition template, ref TokenDefinition token, TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state, ref bool inFrontMatterToken, ref StringBuilder tokenContent, TokenizerOptions options)
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
                    token = new TokenDefinition();
                    if (inFrontMatterToken)
                    {
                        inFrontMatterToken = false;
                        state = TemplateDefinitionParserState.InFrontMatter;
                    }
                    else
                    {
                        state = TemplateDefinitionParserState.InPreamble;
                    }
                    break;

                case ":":
                    state = TemplateDefinitionParserState.InDecorator;
                    break;

                case "'":
                    state = TemplateDefinitionParserState.InTokenValueSingleQuotes;
                    break;

                case "\"":
                    state = TemplateDefinitionParserState.InTokenValueDoubleQuotes;
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

        private void ParseTokenValueInSingleQuotes(TemplateDefinitionEnumerator enumerator, ref TokenDefinition token, ref TemplateDefinitionParserState state, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "'":
                    state = TemplateDefinitionParserState.InTokenValueRunOff;
                    break;

                default:
                    token.AppendValue(next);
                    break;
            }

            tokenContent.Append(next);
        }

        private void ParseTokenValueInDoubleQuotes(TemplateDefinitionEnumerator enumerator, ref TokenDefinition token, ref TemplateDefinitionParserState state, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case @"""":
                    state = TemplateDefinitionParserState.InTokenValueRunOff;
                    break;

                default:
                    token.AppendValue(next);
                    break;
            }

            tokenContent.Append(next);
        }

        private void ParseTokenValueRunOff(TemplateDefinitionEnumerator enumerator, ref TemplateDefinition template, ref TokenDefinition token, ref TemplateDefinitionParserState state, ref bool inFrontMatterToken, ref StringBuilder tokenContent, TokenizerOptions options)
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
                    state = TemplateDefinitionParserState.InDecorator;
                    break;

                case "}" when inFrontMatterToken == false:
                case "\n" when inFrontMatterToken:
                    token.IsFrontMatterToken = inFrontMatterToken;
                    AppendToken(template, token, ref tokenContent, options);
                    token = new TokenDefinition();
                    if (inFrontMatterToken)
                    {
                        inFrontMatterToken = false;
                        state = TemplateDefinitionParserState.InFrontMatter;
                    }
                    else
                    {
                        state = TemplateDefinitionParserState.InPreamble;
                    }
                    break;

                default:
                    throw new TokenizerException($"Unexpected character: '{next}'"); 
            }
        }

        private void ParseDecorator(TemplateDefinition template, ref TokenDefinition token, TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state, ref DecoratorDefinition decorator, ref bool inFrontMatterToken, ref StringBuilder tokenContent, TokenizerOptions options)
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
                    token = new TokenDefinition();
                    decorator = new DecoratorDefinition();
                    if (inFrontMatterToken)
                    {
                        inFrontMatterToken = false;
                        state = TemplateDefinitionParserState.InFrontMatter;
                    }
                    else
                    {
                        state = TemplateDefinitionParserState.InPreamble;
                    }
                    break;

                case ",":
                    AppendDecorator(enumerator, token, decorator);
                    decorator = new DecoratorDefinition();
                    break;

                case "(":
                    state = TemplateDefinitionParserState.InDecoratorArgument;
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

        private void ParseDecoratorArgument(TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state, ref DecoratorDefinition decorator, ref string argument, ref StringBuilder tokenContent)
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
                    state = TemplateDefinitionParserState.InDecorator;
                    break;

                case "'":
                    if (string.IsNullOrWhiteSpace(argument))
                    {
                        argument = string.Empty;
                        state = TemplateDefinitionParserState.InDecoratorArgumentSingleQuotes;
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
                        state = TemplateDefinitionParserState.InDecoratorArgumentDoubleQuotes;
                    }
                    else
                    {
                        argument += next;
                    }
                    break;

                case ",":
                    decorator.Args.Add(argument.Trim());
                    argument = string.Empty;
                    state = TemplateDefinitionParserState.InDecoratorArgument;
                    break;

                default:
                    argument += next;
                    break;
            }

        }

        private void ParseDecoratorArgumentInSingleQuotes(TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state, ref DecoratorDefinition decorator, ref string argument, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case "'":
                    decorator.Args.Add(argument);
                    argument = string.Empty;
                    state = TemplateDefinitionParserState.InDecoratorArgumentRunOff;
                    break;

                default:
                    argument += next;
                    break;
            }

            tokenContent.Append(next);
        }

        private void ParseDecoratorArgumentInDoubleQuotes(TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state, ref DecoratorDefinition decorator, ref string argument, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();

            switch (next)
            {
                case @"""":
                    decorator.Args.Add(argument);
                    argument = string.Empty;
                    state = TemplateDefinitionParserState.InDecoratorArgumentRunOff;
                    break;

                default:
                    argument += next;
                    break;
            }
            
            tokenContent.Append(next);
        }

        private void ParseDecoratorArgumentRunOff(TemplateDefinitionEnumerator enumerator, ref TemplateDefinitionParserState state, ref StringBuilder tokenContent)
        {
            var next = enumerator.Next();
            tokenContent.Append(next);

            if (string.IsNullOrWhiteSpace(next)) return;

            switch (next)
            {
                case ",":
                    state = TemplateDefinitionParserState.InDecoratorArgument;
                    break;

                case ")":
                    state = TemplateDefinitionParserState.InDecorator;
                    break;

                default:
                    throw new TokenizerException($"Unexpected character: '{next}'"); 
            }

        }

        private void AppendToken(TemplateDefinition template, TokenDefinition token, ref StringBuilder tokenContent, TokenizerOptions options)
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

                var repeat = new TokenDefinition
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

        private void AppendDecorator(TemplateDefinitionEnumerator enumerator, TokenDefinition token, DecoratorDefinition decorator)
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

                case "once":
                    if (decorator.Args.Any()) throw  new ParsingException($"'{decorator.Name}' decorator does not take any arguments", enumerator);
                    token.ConsiderOnce = true;
                    break;

                default:
                    token.Decorators.Add(decorator);
                    break;
            }
        }

        private string GetRepeatingMultilinePreamble(TokenDefinition token)
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
}
