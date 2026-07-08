namespace Tokens.Transformers;

/// <summary>
/// A transformer that receives <see cref="TokenizerOptions"/> for context-dependent
/// operations such as culture-aware date/time parsing.
/// </summary>
public interface IOptionsAwareTransformer : ITokenTransformer
{
    /// <summary>
    /// Attempts to transform the given input using the specified options for context.
    /// </summary>
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed);
}
