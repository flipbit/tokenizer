using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            RegisterValidator<IsNumericValidator>();
            RegisterValidator<MaxLengthValidator>();
            RegisterValidator<MinLengthValidator>();
            RegisterValidator<IsDomainNameValidator>();
            RegisterValidator<IsPhoneNumberValidator>();
            RegisterValidator<IsEmailValidator>();
            RegisterValidator<IsUrlValidator>();
            RegisterValidator<IsDateTimeValidator>();
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
                template.Hints.Add(hint);
            }

            foreach (var preToken in preTemplate.Tokens)
            {
                var token = new Token();

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

                // All tokens optional if out-of-order enabled
                if (template.Options.OutOfOrderTokens)
                {
                    token.Optional = true;
                }

                ParseTokenDecorators(preToken.Decorators, token);

                template.AddToken(token);

                log.Trace("  -> Token {0:000}: '{1}'", token.Id, token.Name);
            }

            log.Debug("Parsed '{0}' - {1:###,###,###,##0} byte(s) in {2}", template.Name, content.Length, stopwatch?.Elapsed.ToString("g"));

            return template;
        }

        private void ParseTokenDecorators(IEnumerable<PreTokenDecorator> decorators, Token token)
        {
            foreach (var decorator in decorators)
            {
                TokenDecoratorContext context = null;

                foreach (var operatorType in transformers)
                {
                    if (string.Compare(decorator.Name, operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare($"{decorator.Name}Transformer", operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
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

                        token.Decorators.Add(context);
    
                        break;
                    }
                }

                if (context == null)
                {
                    throw new TokenizerException($"Unknown Token Operation: {decorator.Name}");
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
