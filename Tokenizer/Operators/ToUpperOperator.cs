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
        /// <param name="function"></param>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public object Perform(Function function, Token token, object value)
        {
            if (value == null) return string.Empty;

            return value.ToString().ToUpper();
        }
    }
}
