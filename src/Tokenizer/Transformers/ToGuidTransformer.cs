namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="Guid"/>
/// </summary>
public sealed class ToGuidTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = value!;
            return false;
        }

        if (Guid.TryParse(valueString, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value;
        return false;
    }
}
