namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is not empty 
    /// </summary>
    public class IsNotEmptyValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            if (value == null) return false;

            var valueString = value.ToString();

            return !string.IsNullOrEmpty(valueString);
        }
    }
}
