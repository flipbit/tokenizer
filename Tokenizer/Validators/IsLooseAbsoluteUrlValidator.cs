using System;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is a URL
    /// </summary>
    public class IsLooseAbsoluteUrlValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (value == null) return false;

            var valueString = value.ToString();

            if (string.IsNullOrEmpty(valueString)) return false;

            var result = Uri.IsWellFormedUriString(valueString, UriKind.Absolute);

            if (!result)
			{
                result = Uri.IsWellFormedUriString(string.Format("http://{0}", valueString), UriKind.Absolute);
			}

            return result;
        }
    }
}
