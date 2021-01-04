using System;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is a URL
    /// </summary>
    public class IsLooseUrlValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (value == null) return false;

            var valueString = value.ToString();

            if (string.IsNullOrEmpty(valueString)) return false;

            var result = Uri.IsWellFormedUriString(valueString, UriKind.RelativeOrAbsolute);

            if (!result)
			{
                result = Uri.IsWellFormedUriString($"http://{valueString}", UriKind.Absolute);
			}

            return result;
        }
    }
}
