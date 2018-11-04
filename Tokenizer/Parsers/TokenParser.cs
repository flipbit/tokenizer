using System;
using System.Collections.Generic;
using Tokens.Exceptions;
using Tokens.Extensions;
using Tokens.Transformers;
using Tokens.Validators;

namespace Tokens.Parsers
{
    internal class TokenParser
    {
        private readonly List<Type> transformers;
        private readonly List<Type> validators;

        public TokenizerOptions Options { get; set; }

        public TokenParser() : this(TokenizerOptions.Defaults)
        {
        }

        public TokenParser(TokenizerOptions options)
        {
            Options = options;

            transformers = new List<Type>();
            validators = new List<Type>();

            // Add default transformers/validators
            RegisterTransformer<ToDateTimeTransformer>();
            RegisterTransformer<ToLowerTransformer>();
            RegisterTransformer<ToUpperTransformer>();
            RegisterTransformer<TrimTransformer>();
            RegisterTransformer<SubstringAfterTransformer>();
            RegisterTransformer<SubstringBeforeTransformer>();

            RegisterValidator<IsNumericValidator>();
            RegisterValidator<MaxLengthValidator>();
            RegisterValidator<MinLengthValidator>();
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
            return Parse(content, string.Empty);
        }

        public Template Parse(string content, string name)
        {
            var template = new Template(name, content);

            var rawTemplate = new RawTokenParser().Parse(content, Options);

            template.Options = rawTemplate.Options;

            foreach (var rawToken in rawTemplate.Tokens)
            {
                var token = new Token();

                if (Options.TrimLeadingWhitespaceInTokenPreamble)
                {
                    if (rawToken.Preamble.IsOnlySpaces())
                    {
                        token.Preamble = rawToken.Preamble;
                    }
                    if (string.IsNullOrWhiteSpace(rawToken.Preamble))
                    {
                        token.Preamble = rawToken.Preamble.TrimLeadingSpaces();
                    }
                    else
                    {
                        token.Preamble = rawToken.Preamble.TrimStart();
                    }
                }
                else
                {
                    token.Preamble = rawToken.Preamble;
                }

                token.Name = rawToken.Name;
                token.Optional = rawToken.Optional;
                token.Repeating = rawToken.Repeating;
                token.TerminateOnNewLine = rawToken.TerminateOnNewline;

                // All tokens optional if out-of-order enabled
                if (template.Options.OutOfOrderTokens)
                {
                    token.Optional = true;
                }

                var tokenTransformers = new List<TransformerContext>();
                var tokenValidators = new List<ValidatorContext>();

                ParseTokenOperators(rawToken.Decorators, tokenTransformers, tokenValidators);

                foreach (var tokenOperator in tokenTransformers)
                {
                    token.Transformers.Add(tokenOperator);
                }

                foreach (var tokenValidator in tokenValidators)
                {
                    token.Validators.Add(tokenValidator);
                }

                template.AddToken(token);
            }

            return template;
        }

        private void ParseTokenOperators(IEnumerable<RawTokenDecorator> decorators, List<TransformerContext> tokenTransformers, List<ValidatorContext> tokenValidators)
        {
            foreach (var decorator in decorators)
            {
                TransformerContext transformerContext = null;
                ValidatorContext validatorContext = null;

                foreach (var operatorType in transformers)
                {
                    if (string.Compare(decorator.Name, operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare($"{decorator.Name}Transformer", operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        transformerContext = new TransformerContext(operatorType);

                        foreach (var arg in decorator.Args)
                        {
                            transformerContext.Parameters.Add(arg);
                        }

                        tokenTransformers.Add(transformerContext);
    
                        break;
                    }
                }

                if (transformerContext != null) continue;

                foreach (var validatorType in validators)
                {
                    if (string.Compare(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0 ||
                        string.Compare($"{decorator.Name}Validator", validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        validatorContext = new ValidatorContext(validatorType);

                        foreach (var arg in decorator.Args)
                        {
                            validatorContext.Parameters.Add(arg);
                        }

                        tokenValidators.Add(validatorContext);
    
                        break;
                    }
                }

                if (validatorContext == null)
                {
                    throw new TokenizerException($"Unknown Token Operation: {decorator.Name}");
                }
            }
        }
    }
}
