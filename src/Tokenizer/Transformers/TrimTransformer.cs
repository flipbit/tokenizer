namespace Tokens.Transformers;

/// <summary>
/// Trims the token value 
/// </summary>
public sealed class TrimTransformer : ITokenTransformer
{
    public bool CanTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        transformed = valueString.Trim();

        return true;
    }
}
