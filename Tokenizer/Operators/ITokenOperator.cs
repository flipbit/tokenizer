namespace Tokens.Operators
{
    /// <summary>
    /// Defines an operation that can be performed on a token
    /// </summary>
    public interface ITokenOperator
    {
        object Perform(object value, params string[] args);
    }
}
