namespace Tokens.Operators
{
    /// <summary>
    /// Converts the token value to upper case
    /// </summary>
    public class ToUpperOperator : ITokenOperator
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
            return value.ToUpper();
        }
    }
}
