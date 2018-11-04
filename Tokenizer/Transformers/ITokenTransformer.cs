namespace Tokens.Transformers
{
    /// <summary>
    /// Defines an operation that can be performed on a token
    /// </summary>
    public interface ITokenTransformer
    {
        object Transform(object value, params string[] args);
    }
}
