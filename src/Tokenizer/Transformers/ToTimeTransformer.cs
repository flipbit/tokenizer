#if NET6_0_OR_GREATER
using System.Globalization;
using Tokens.Temporal;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="TimeOnly"/>.
/// Silently drops any date component present in the value.
/// </summary>
public sealed class ToTimeTransformer : IOptionsAwareTransformer
{
    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        return TryTransform(value, args, new TokenizerOptions(), out transformed);
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, TokenizerOptions options, out object transformed)
    {
        var culture = options.Culture ?? CultureInfo.InvariantCulture;

        // Try TimeOnly-specific parsing first
        if (value?.ToString() is { Length: > 0 } str)
        {
            if (args is { Length: > 0 } && !string.IsNullOrWhiteSpace(args[0]))
            {
                // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
                foreach (var format in args)
                {
                    if (TimeOnly.TryParseExact(str, format, culture, DateTimeStyles.None, out var time))
                    {
                        transformed = time;
                        return true;
                    }
                }
            }

            // Fall back to TemporalParser for full datetime strings, extract time
            if (TemporalParser.TryParse(str, args, options, out var dto))
            {
                transformed = TimeOnly.FromTimeSpan(dto.TimeOfDay);
                return true;
            }
        }

        transformed = value!;
        return false;
    }
}
#endif
