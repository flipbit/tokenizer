using System;
using Tokens.Exceptions;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value meets a maximum length requirement
    /// </summary>
    public class MinLength : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (args.Length == 0)
            {
                throw new ValidationException("You must specified a MinLength value, e.g. 'MinLength(50)'");
            }

            try
            {
                var minLength = Convert.ToInt32(args[0]);

                return value.ToString().Length >= minLength;
            }
            catch (FormatException ex)
            {                
                throw new ValidationException("MinLength parameter must be an integer", ex);
            }

        }
    }
}
