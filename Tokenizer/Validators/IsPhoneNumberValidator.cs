using Tokens.Extensions;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is a phone number
    /// </summary>
    public class IsPhoneNumberValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (value == null) return false;

            var valueString = value.ToString();

            if (string.IsNullOrEmpty(valueString)) return false;

            var trimmed = valueString.Keep(" ()+0123456789");

            if (string.IsNullOrWhiteSpace(trimmed)) return false;

            var numbers = trimmed.Keep("0123456789");

            return numbers.Length >= 6;
        }
    }
}
