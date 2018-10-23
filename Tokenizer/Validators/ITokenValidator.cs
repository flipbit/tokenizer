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
        bool IsValid(object value, params string[] args);
    }
}
