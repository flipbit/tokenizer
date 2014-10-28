using System;
using Tokens.Exceptions;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value meets a maximum length requirement
    /// </summary>
    public class MinLengthValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool IsValid(Function function, Token token, string value)
        {
            if (function.Parameters.Count == 0)
            {
                throw new ValidationException("You must specified a MinLength value, e.g. 'MinLength(50)'");
            }

            try
            {
                var minLength = Convert.ToInt32(function.Parameters[0]);

                return value.Length >= minLength;
            }
            catch (FormatException ex)
            {                
                throw new ValidationException("MinLength parameter must be an integer", ex);
            }

        }
    }
}
