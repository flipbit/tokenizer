using System.Globalization;
using Tokens.Extensions;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="DateTime"/> in UTC format
/// </summary>
public sealed class ToDateTimeUtcTransformer : ITokenTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (value is string valueString && !string.IsNullOrWhiteSpace(valueString))
        {
            if (valueString.Contains("(UTC)"))
            {
                valueString = valueString.SubstringBeforeString("(UTC)");
            }

            if (valueString.Contains("UTC"))
            {
                valueString = valueString.SubstringBeforeString("UTC");
            }

            value = valueString.Trim();
        }

        if (ToDateTimeTransformer.TryParseDateTime(value, args, DateTimeStyles.AssumeUniversal, out var result))
        {
            transformed = DateTime.SpecifyKind(result.ToUniversalTime(), DateTimeKind.Utc);
            return true;
        }

        transformed = value;

        return false;
    }
}
