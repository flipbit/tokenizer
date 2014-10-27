namespace Tokens.Validators
{
    /// <summary>
    /// Interface to define the validation of tokens discovered in input text
    /// </summary>
    public interface ITokenValidator
    {
        /// <summary>
        /// Determines whether the specified token is valid.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="token">The token.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        bool IsValid(Function function, Token token, string value);
    }
}
