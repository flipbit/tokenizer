namespace Tokens.Transformers
{
    /// <summary>
    /// Defines an operation that can be performed on a token
    /// </summary>
    public interface ITokenTransformer : ITokenDecorator
    {
        object Transform(object value, params string[] args);
    }
}
