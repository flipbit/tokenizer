using System;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is a domain name 
    /// </summary>
    public class IsDomainNameValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (value == null) return false;

            var valueString = value.ToString();

            if (string.IsNullOrEmpty(valueString)) return false;

            return Uri.CheckHostName(valueString) == UriHostNameType.Dns;
        }
    }
}
