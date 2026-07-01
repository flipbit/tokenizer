namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to upper case
/// </summary>
public sealed class ToUpperTransformer : ITokenTransformer
{
    public bool CanTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
        }
        else
        {
            transformed = valueString.ToUpperInvariant();
        }

        return true;
    }
}
