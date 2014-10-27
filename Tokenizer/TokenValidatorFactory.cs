using Tokens.Exceptions;
using Tokens.Validators;

namespace Tokens
{
    /// <summary>
    /// Factory class to create <see cref="ITokenValidator"/> objects.
    /// </summary>
    public class TokenValidatorFactory : BaseTokenFactory<ITokenValidator>
    {
        /// <summary>
        /// Validates the given value.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool Validate(Token token, string value)
        {
            if (string.IsNullOrEmpty(token.Operation))
            {
                return true;
            }

            if (!token.Operation.StartsWith("Is"))
            {
                return true;
            }

            var function = FunctionParser.Parse(token.Operation);

            var valid = true;
            var fired = false;

            foreach (var @operator in Items)
            {
                if (!CanPerform(@operator, token)) continue;

                fired = true;

                valid = @operator.IsValid(function, token, value);
                    
                if (!valid) break;
            }

            if (!fired)
            {
                throw new MissingOperationException("Unknown validation method: " + value);
            }

            return valid;
        }

        private bool CanPerform(ITokenValidator validator, Token token)
        {
            var name = validator.GetType().Name.Replace("Validator", string.Empty) + "(";

            return token.Operation.StartsWith(name);
        }
    }
}
