using System;
using System.Collections.Generic;
using System.Linq;
using Tokens.Operators;
using Tokens.Validators;

namespace Tokens
{
    /// <summary>
    /// Factory class to create <see cref="ITokenOperator"/> objects.
    /// </summary>
    public class TokenOperatorFactory
    {
        /// <summary>
        /// Gets or sets the function parser.
        /// </summary>
        /// <value>
        /// The function parser.
        /// </value>
        public FunctionParser FunctionParser { get; set; }

        /// <summary>
        /// Gets the operators.
        /// </summary>
        /// <value>
        /// The operators.
        /// </value>
        public IList<ITokenOperator> Operators { get; private set; }

        /// <summary>
        /// Gets the validators.
        /// </summary>
        /// <value>
        /// The validators.
        /// </value>
        public IList<ITokenValidator> Validators { get; private set; }

        /// <summary>
        /// Initializes the <see cref="TokenOperatorFactory"/> class.
        /// </summary>
        public TokenOperatorFactory()
        {
            Operators = new List<ITokenOperator>();
            Validators = new List<ITokenValidator>();

            FunctionParser = new FunctionParser();

            PopulateList(Operators);
            PopulateList(Validators);
        }

        /// <summary>
        /// Populates the given list with new instances from the current assembly.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The target.</param>
        private void PopulateList<T>(ICollection<T> target) where T : class
        {
            var types = GetType().Assembly.GetTypes();

            foreach (var type in types)
            {
                if (type.GetInterfaces().Contains(typeof(T)) && !type.IsInterface)
                {
                    var @operator = Activator.CreateInstance(type) as T;

                    target.Add(@operator);
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
        public object PerformOperation(Token token, string value)
        {
            object result = value;

            foreach (var function in token.Functions)
            {
                if (IsOperation(function))
                {
                    var @operator = GetOperation(function);

                    result = @operator.Perform(function, token, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Validates the given value.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool Validate(Token token, string value)
        {
            foreach (var function in token.Functions)
            {
                if (IsValidator(function))
                {
                    var validator = GetValidator(function);

                    if (!validator.IsValid(function, token, value))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified token contains one or more operations.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool ContainsOperation(Token token)
        {
            return token.Functions.Any(IsOperation);
        }

        /// <summary>
        /// Determines whether the specified function is an operation.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns></returns>
        public bool IsOperation(Function function)
        {
            return GetItem(Operators, "Operator", function) != null;
        }

        /// <summary>
        /// Determines whether the specified function is an operation.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns></returns>
        public ITokenOperator GetOperation(Function function)
        {
            return GetItem(Operators, "Operator", function);
        }

        /// <summary>
        /// Determines whether the specified token contains one or more validators.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public bool ContainsValidator(Token token)
        {
            return token.Functions.Any(IsValidator);
        }

        /// <summary>
        /// Determines whether the specified function is a validator.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns></returns>
        public bool IsValidator(Function function)
        {
            return GetItem(Validators, "Validator", function) != null;
        }

        public ITokenValidator GetValidator(Function function)
        {
            return GetItem(Validators, "Validator", function);
        }

        private T GetItem<T>(IEnumerable<T> items, string typeName, Function function)
        {
            foreach (var item in items)
            {
                var name = item.GetType().Name.Replace(typeName, string.Empty);

                if (name == function.Name)
                {
                    return item;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Determines whether the given token has missing functions.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public bool HasMissingFunctions(Token token)
        {
            foreach (var function in token.Functions)
            {
                // Check if function is a valid Operator or Validator
                if (!(GetItem(Operators, "Operator", function) != null ||
                      GetItem(Validators, "Validator", function) != null))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
