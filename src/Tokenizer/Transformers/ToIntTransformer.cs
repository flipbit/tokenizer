using System.Globalization;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to an <see cref="int"/>
/// </summary>
public sealed class ToIntTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = value!;
            return false;
        }

        if (int.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value;
        return false;
    }
}
