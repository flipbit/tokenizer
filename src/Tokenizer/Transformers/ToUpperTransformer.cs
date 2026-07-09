namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to upper case
/// </summary>
public sealed class ToUpperTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        transformed = value?.ToString() is not { Length: > 0 } valueString
            ? string.Empty
            : valueString.ToUpperInvariant();

        return true;
    }
}
