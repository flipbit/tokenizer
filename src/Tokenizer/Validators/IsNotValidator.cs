using System;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is not the specified value
    /// </summary>
    public class IsNotValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (value == null) return true;

            var valueString = value.ToString();

            if (string.IsNullOrEmpty(valueString)) return true;

            if (args.Length != 1) throw new ArgumentException("IsNot() - Must supply a string value");

            return valueString != args[0];
        }
    }
}
