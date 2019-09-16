using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Logging;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Parsers
{
    /// <summary>
    /// Parser that converts a string into a <see cref="Template"/> that can be
    /// used to extract objects from input strings.
    /// </summary>
    internal class TokenParser
    {
        private readonly List<Type> transformers;
        private readonly List<Type> validators;

        private readonly ILog log;

        public TokenizerOptions Options { get; set; }

        public TokenParser() : this(TokenizerOptions.Defaults)
        {
        }

        public TokenParser(TokenizerOptions options)
        {
            log = LogProvider.For<TokenParser>();

            Options = options;

            transformers = new List<Type>();
            validators = new List<Type>();

            // Add default transformers/validators
            RegisterTransformer<ToDateTimeTransformer>();
            RegisterTransformer<ToDateTimeUtcTransformer>();
            RegisterTransformer<ToLowerTransformer>();
            RegisterTransformer<ToUpperTransformer>();
            RegisterTransformer<TrimTransformer>();
            RegisterTransformer<SubstringAfterTransformer>();
            RegisterTransformer<SubstringBeforeTransformer>();
            RegisterTransformer<SetTransformer>();
            RegisterTransformer<ReplaceTransformer>();
            RegisterTransformer<SubstringAfterLastTransformer>();
            RegisterTransformer<SubstringBeforeLastTransformer>();

            RegisterValidator<IsNumericValidator>();
            RegisterValidator<MaxLengthValidator>();
            RegisterValidator<MinLengthValidator>();
            RegisterValidator<IsDomainNameValidator>();
            RegisterValidator<IsPhoneNumberValidator>();
            RegisterValidator<IsEmailValidator>();
            RegisterValidator<IsUrlValidator>();
            RegisterValidator<IsDateTimeValidator>();
            RegisterValidator<IsNotEmptyValidator>();
            RegisterValidator<IsNotValidator>();
            RegisterValidator<StartsWithValidator>();
        }

        public TokenParser RegisterTransformer<T>() where T : ITokenTransformer
        {
            transformers.Add(typeof(T));

            return this;
        }

        public TokenParser RegisterValidator<T>() where T : ITokenValidator
        {
            validators.Add(typeof(T));

            return this;
        }

        public Template Parse(string content)
        {
            var name = GenerateTemplateName(content);

            return Parse(content, name);
        }

        public Template Parse(string content, string name)
        {
            Stopwatch stopwatch = null;

            if (log.IsDebugEnabled())
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }

            var template = new Template(name, content);

            log.Trace("Start: Parsing Template: {0}", template.Name);

            var preTemplate = new PreTokenParser().Parse(content, Options);

            template.Options = preTemplate.Options;

            if (string.IsNullOrWhiteSpace(preTemplate.Name) == false)
            {
                template.Name = preTemplate.Name;
            }

            foreach (var hint in preTemplate.Hints)
            {
                if (template.Hints.Any(t => t == hint) == false)
                {
                    template.Hints.Add(hint);
                }
            }

            foreach (var tag in preTemplate.Tags)
            {
                if (template.Tags.Any(t => t == tag) == false)
                {
                    template.Tags.Add(tag);
                }
            }

            foreach (var preToken in preTemplate.Tokens)
            {
                var token = new Token(preToken.Content);

                if (Options.TrimLeadingWhitespaceInTokenPreamble)
                {
                    if (preToken.Preamble.IsOnlySpaces())
                    {
                        token.Preamble = preToken.Preamble;
                    }
                    else if (string.IsNullOrWhiteSpace(preToken.Preamble))
                    {
                        token.Preamble = preToken.Preamble.TrimLeadingSpaces();
                    }
                    else
                    {
                        token.Preamble = preToken.Preamble.TrimStart();
                    }
                }
                else
                {
                    token.Preamble = preToken.Preamble;
                }

                token.Name = preToken.Name;
                token.Optional = preToken.Optional;
                token.Repeating = preToken.Repeating;
                token.TerminateOnNewLine = preToken.TerminateOnNewline;
                token.Required = preToken.Required;
                token.Id = preToken.Id;
                token.DependsOnId = preToken.DependsOnId;
                token.IsFrontMatterToken = preToken.IsFrontMatterToken;
                token.IsNull = preToken.IsNull;
                token.Location = preToken.Location;

                // All tokens optional if out-of-order enabled
                if (template.Options.OutOfOrderTokens)
                {
                    token.Optional = true;
                }

                ParseTokenDecorators(preToken, token);

                template.AddToken(token);

                if (string.IsNullOrEmpty(token.Name) == false)
                {
                    log.Trace("  -> Token[{0:000}]: '{1}'", token.Id, token.Name);
                }
            }

            log.Debug("Parsed '{0}' - {1:###,###,###,##0} byte(s) in {2}", template.Name, content.Length, stopwatch?.Elapsed.ToString("g"));

            return template;
        }

        private void ParseTokenDecorators(PreToken preToken, Token token)
        {
            // If pre-token has value set, add transformer to set it when parsing
            if (string.IsNullOrEmpty(preToken.Value) == false)
            {
                var setContext = new TokenDecoratorContext(typeof(SetTransformer));
                setContext.Parameters.Add(preToken.Value);
                token.Decorators.Add(setContext);
            }

            foreach (var decorator in preToken.Decorators)
            {
                TokenDecoratorContext context = null;

                foreach (var operatorType in transformers)
                {
                    if (string.Compare(decorator.Name, operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare($"{decorator.Name}Transformer", operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        if (decorator.IsNotDecorator)
                        {
                            throw new TokenizerException($"{decorator.Name} cannot be prefixed with '!' character.");
                        }

                        context = new TokenDecoratorContext(operatorType);

                        foreach (var arg in decorator.Args)
                        {
                            context.Parameters.Add(arg);
                        }

                        token.Decorators.Add(context);
    
                        break;
                    }
                }

                if (context != null) continue;

                foreach (var validatorType in validators)
                {
                    if (string.Compare(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare($"{decorator.Name}Validator", validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        context = new TokenDecoratorContext(validatorType);

                        foreach (var arg in decorator.Args)
                        {
                            context.Parameters.Add(arg);
                        }

                        context.IsNotValidator = decorator.IsNotDecorator;

                        token.Decorators.Add(context);
    
                        break;
                    }
                }

                if (context == null)
                {
                    throw new TokenizerException($"Unknown Token Operation: {decorator.Name}");
                }
            }

            if (preToken.IsFrontMatterToken)
            {
                var hasSetTransformer = token.Decorators.Any(d => d.DecoratorType == typeof(SetTransformer));

                if (hasSetTransformer == false)
                {
                    throw new TokenizerException($"Front Matter Token '{preToken.Name}' must have an assignment operation.");
                }
            }
        }

        private string GenerateTemplateName(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "(empty)";

            var name = new StringBuilder();

            var words = 0;
            var lastCharWasANewLine = false;

            var startIndex = 0;
            var hasFrontmatter = content.StartsWith("---\n") || content.StartsWith("---\r\n");

            if (hasFrontmatter)
            {
                var frontmatterEndIndex = content.IndexOf("\n---", 5);

                if (frontmatterEndIndex > -1) startIndex = frontmatterEndIndex + 4;
            }

            for (var i = startIndex; i < content.Length; i++)
            {
                var c = content[i];
                


                if (char.IsWhiteSpace(c))
                {
                    if (lastCharWasANewLine) continue;
                    if (name.Length == 0) continue;

                    lastCharWasANewLine = true;

                    words++;
                    if (words <= 2)
                    {
                        name.Append(' ');
                        continue;
                    }

                    name.Append("...");
                    break;
                }

                name.Append(c);
                lastCharWasANewLine = false;
            }

            return name.ToString();
        }
    }
}
