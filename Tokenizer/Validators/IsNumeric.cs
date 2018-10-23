namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is numeric
    /// </summary>
    public class IsNumeric : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        public bool IsValid(object value, params string[] args)
        {
            float test;

            return float.TryParse(value.ToString(), out test);
        }
    }
}
