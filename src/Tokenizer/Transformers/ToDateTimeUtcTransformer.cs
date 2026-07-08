using Tokens.Temporal;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="DateTimeOffset"/> in UTC.
/// </summary>
[Obsolete("Use ToDateTime instead. ToDateTimeUtc will be removed in a future major version.")]
public sealed class ToDateTimeUtcTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        // Strip UTC markers before parsing
        if (value is string valueString && !string.IsNullOrWhiteSpace(valueString))
        {
            if (valueString.Contains("(UTC)", StringComparison.Ordinal))
            {
                valueString = valueString.Substring(0, valueString.IndexOf("(UTC)", StringComparison.Ordinal)).Trim();
            }
            else if (valueString.Contains("UTC", StringComparison.Ordinal))
            {
                valueString = valueString.Substring(0, valueString.IndexOf("UTC", StringComparison.Ordinal)).Trim();
            }

            value = valueString;
        }

        // Force UTC offset when no explicit offset is present in the data
        var utcOptions = options with { DefaultOffset = TimeSpan.Zero };

        if (TemporalParser.TryParse(value?.ToString(), args, utcOptions, out var result))
        {
            transformed = result.ToOffset(TimeSpan.Zero);
            return true;
        }

        transformed = value!;
        return false;
    }
}
