using System;
using Tokens.Exceptions;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value meets a maximum length requirement
    /// </summary>
    public class MaxLength : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (args.Length == 0)
            {
                throw new ValidationException("You must specified a MaxLength value, e.g. 'MaxLength(255)'");
            }

            try
            {
                var maxLength = Convert.ToInt32(args[0]);

                return value.ToString().Length <= maxLength;
            }
            catch (FormatException ex)
            {                
                throw new ValidationException("MaxLength parameter must be an integer", ex);
            }

        }
    }
}
