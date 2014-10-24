using System;
using System.Collections.Generic;
using System.Linq;
using Tokens.Operators;

namespace Tokens
{
    /// <summary>
    /// Factory class to create <see cref="ITokenOperator"/> objects.
    /// </summary>
    public class TokenOperatorFactory
    {
        /// <summary>
        /// Gets the operators.
        /// </summary>
        /// <value>
        /// The operators.
        /// </value>
        public IList<ITokenOperator> Operators { get; private set; }

        /// <summary>
        /// Initializes the <see cref="TokenOperatorFactory"/> class.
        /// </summary>
        public TokenOperatorFactory()
        {
            Operators = new List<ITokenOperator>();

            var types = GetType().Assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.GetInterfaces().Contains(typeof(ITokenOperator)) && !type.IsInterface)
                {
                    var @operator = Activator.CreateInstance(type) as ITokenOperator;

                    Operators.Add(@operator);
                }
            }
        }

        /// <summary>
        /// Performs an operation on the given value.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public string PerformOperation(Token token, string value)
        {
            if (string.IsNullOrEmpty(token.Operation))
            {
                return value;
            }

            var fired = false;

            foreach (var @operator in Operators)
            {
                if (!CanPerform(@operator, token)) continue;

                value = @operator.Perform(token, value);
                    
                fired = true;

                break;
            }

            if (!fired)
            {
                var message = "Can't perform operation: " + token.Operation;

                throw new MissingOperationException(message);
            }

            return value;
        }

        private bool CanPerform(ITokenOperator @operator, Token token)
        {
            var name = @operator.GetType().Name.Replace("Operator", string.Empty) + "(";

            return token.Operation.StartsWith(name);
        }
    }
}
