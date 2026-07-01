namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to lower case
/// </summary>
public sealed class ToLowerTransformer : ITokenTransformer
{
    public bool CanTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        transformed = valueString.ToLowerInvariant();

        return true;
    }
}
