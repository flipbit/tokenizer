namespace Tokens.Validators
{
    /// <summary>
    /// Validator to determine if a token value is numeric
    /// </summary>
    public class IsNumericValidator : ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public bool IsValid(Function function, Token token, string value)
        {
            float test;

            return float.TryParse(value, out test);
        }
    }
}
