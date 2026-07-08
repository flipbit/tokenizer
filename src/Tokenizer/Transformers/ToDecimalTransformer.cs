using System.Globalization;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="decimal"/>
/// </summary>
public sealed class ToDecimalTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = value!;
            return false;
        }

        if (decimal.TryParse(valueString, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value;
        return false;
    }
}
