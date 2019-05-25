using Tokens.Transformers;

namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is a date time string 
    /// </summary>
    public class IsDateTimeValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (value == null) return false;

            var valueString = value.ToString();

            if (string.IsNullOrEmpty(valueString)) return false;

            return ToDateTimeTransformer.TryParseDateTime(valueString, args, out _);
        }
    }
}
