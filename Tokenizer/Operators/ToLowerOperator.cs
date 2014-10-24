namespace Tokens.Operators
{
    /// <summary>
    /// Converts the token value to lower case
    /// </summary>
    public class ToLowerOperator : ITokenOperator
    {
        /// <summary>
        /// Performs the operation on the specified value.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public string Perform(Token token, string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            return value.ToLower();
        }
    }
}
