using System.Globalization;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to title case
/// </summary>
public sealed class TitleCaseTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value?.ToString() is not { Length: > 0 } valueString)
        {
            transformed = string.Empty;
            return true;
        }

        transformed = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(valueString.ToLowerInvariant());

        return true;
    }
}
