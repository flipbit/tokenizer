namespace Tokens.Operators
{
    /// <summary>
    /// Defines an operation that can be performed on a token
    /// </summary>
    public interface ITokenOperator
    {
        /// <summary>
        /// Performs the operation on the specified value.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        string Perform(Token token, string value);
    }
}
