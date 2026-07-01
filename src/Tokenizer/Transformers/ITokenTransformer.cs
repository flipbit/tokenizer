namespace Tokens.Transformers;

/// <summary>
/// Defines an operation that can be performed on a token
/// </summary>
public interface ITokenTransformer : ITokenDecorator
{
    /// <summary>
    /// Attempts to transform the given input
    /// </summary>
    bool TryTransform(object value, string[] args, out object transformed);
}
