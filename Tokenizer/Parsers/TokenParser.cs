using System;
using System.Collections.Generic;
using Tokens.Exceptions;
using Tokens.Operators;
using Tokens.Validators;

namespace Tokens.Parsers
{
    public class TokenParser
    {
        private List<Type> operators;
        private List<Type> validators;

        public TokenParser()
        {
            operators = new List<Type>();
            validators = new List<Type>();

            // Add default operators/validators
            RegisterOperator<ToDateTime>();
            RegisterOperator<ToLower>();
            RegisterOperator<ToUpper>();

            RegisterValidator<IsNumeric>();
            RegisterValidator<MaxLength>();
            RegisterValidator<MinLength>();
        }

        public TokenParser RegisterOperator<T>() where T : ITokenOperator
        {
            operators.Add(typeof(T));

            return this;
        }

        public TokenParser RegisterValidator<T>() where T : ITokenValidator
        {
            validators.Add(typeof(T));

            return this;
        }

        public Template Parse(string name, string content)
        {
            var template = new Template(name, content);

            var rawTokens = new RawTokenParser().Parse(content);

            foreach (var rawToken in rawTokens)
            {
                var token = new Token();
                token.Preamble = rawToken.Preamble;
                token.Name = rawToken.Name;
                token.Optional = rawToken.Optional;
                token.Repeating = rawToken.Repeating;
                token.TerminateOnNewLine = rawToken.TerminateOnNewline;

                var tokenOperators = new List<OperatorContext>();
                var tokenValidators = new List<ValidatorContext>();

                ParseTokenOperators(rawToken.Decorators, tokenOperators, tokenValidators);

                foreach (var tokenOperator in tokenOperators)
                {
                    token.Operators.Add(tokenOperator);
                }

                foreach (var tokenValidator in tokenValidators)
                {
                    token.Validators.Add(tokenValidator);
                }

                template.Tokens.Enqueue(token);
            }

            return template;
        }

        private void ParseTokenOperators(IEnumerable<RawTokenDecorator> decorators, List<OperatorContext> tokenOperators, List<ValidatorContext> tokenValidators)
        {
            foreach (var decorator in decorators)
            {
                OperatorContext operatorContext = null;
                ValidatorContext validatorContext = null;

                foreach (var operatorType in operators)
                {
                    if (string.Compare(decorator.Name, operatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        operatorContext = new OperatorContext(operatorType);

                        foreach (var arg in decorator.Args)
                        {
                            operatorContext.Parameters.Add(arg);
                        }

                        tokenOperators.Add(operatorContext);
    
                        break;
                    }
                }

                if (operatorContext != null) continue;

                foreach (var validatorType in validators)
                {
                    if (string.Compare(decorator.Name, validatorType.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
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
                    throw new TokenizerException($"Unknown Token Operator: {decorator.Name}");
                }
            }
        }
    }
}
