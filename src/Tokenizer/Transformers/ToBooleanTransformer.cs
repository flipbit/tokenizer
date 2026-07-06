namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="bool"/>
/// </summary>
public sealed class ToBooleanTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = value!;
            return false;
        }

        if (valueString.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            valueString.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            valueString == "1")
        {
            transformed = true;
            return true;
        }

        if (valueString.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            valueString.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            valueString == "0")
        {
            transformed = false;
            return true;
        }

        transformed = value;
        return false;
    }
}
