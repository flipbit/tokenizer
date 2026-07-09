using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Tokens.Extensions;

namespace Tokens.Temporal;

/// <summary>
/// Central date/time parsing engine. Orchestrates timezone normalization,
/// format recognition, and DateTimeOffset parsing.
/// </summary>
[SuppressMessage("Meziantou.Analyzer", "MA0182", Justification = "Used by transformers and validators in Tasks 7-9")]
internal static partial class TemporalParser
{
    // Reuse the ordinal suffix regex pattern from ToDateTimeTransformer
#if NET8_0_OR_GREATER
#pragma warning disable MA0009 // GeneratedRegex does not support matchTimeout; source-generated regex avoids ReDoS
    [GeneratedRegex(@"\b(?<digits>\d+)(?:st|nd|rd|th)\b", RegexOptions.ExplicitCapture)]
    private static partial Regex OrdinalSuffixRegex();
#pragma warning restore MA0009
#else
    private static readonly Regex OrdinalSuffixRegexInstance =
        new(@"\b(?<digits>\d+)(?:st|nd|rd|th)\b", RegexOptions.Compiled | RegexOptions.ExplicitCapture,
            TimeSpan.FromSeconds(1));

    private static Regex OrdinalSuffixRegex() => OrdinalSuffixRegexInstance;
#endif

    /// <summary>
    /// Attempts to parse a string value into a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="value">The raw string value to parse.</param>
    /// <param name="formats">Explicit format strings, or null/empty for auto-detection.</param>
    /// <param name="options">Options providing culture, default offset/timezone, and timezone abbreviations.</param>
    /// <param name="result">The parsed result if successful.</param>
    /// <returns>true if parsing succeeded.</returns>
    public static bool TryParse(string? value, string[]? formats, TokenizerOptions options, out DateTimeOffset result)
    {
        result = default;

        if (value?.ToString() is not { Length: > 0 } rawString)
        {
            return false;
        }

        var valueString = rawString.SubstringBeforeNewLine().Trim();

        if (valueString.Length == 0)
        {
            return false;
        }

        // Normalize timezone abbreviations before format recognition
        valueString = TimezoneNormalizer.Normalize(valueString, options.TimezoneAbbreviations);

        var culture = options.Culture ?? CultureInfo.InvariantCulture;

        if (formats is { Length: > 0 } && !string.IsNullOrWhiteSpace(formats[0]))
        {
            return TryParseWithFormats(valueString, formats, culture, options, out result);
        }

        return TryParseWithRecognizer(valueString, culture, options, out result);
    }

    private static bool TryParseWithFormats(
        string value,
        string[] formats,
        CultureInfo culture,
        TokenizerOptions options,
        out DateTimeOffset result)
    {
        foreach (var format in formats)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                continue;
            }

            var (valueToParse, ordinalWasStripped) = StripOrdinalSuffixIfNeeded(value, format);

            // Candidate formats: the provided format, plus a single-d variant when ordinals were stripped
            // (e.g. "dd MMMM yyyy" becomes also "d MMMM yyyy" so that "1 August 2001" parses correctly).
            // We replace "dd " or " dd" boundaries only, to avoid corrupting "ddd"/"dddd" patterns.
            var candidateFormats = BuildCandidateFormats(format, ordinalWasStripped);

            // When the format contains an offset specifier, the parsed offset
            // came from the data and must not be overridden by defaults.
            var formatHasOffset = FormatContainsOffset(format);

            // Try exact format match
            if (DateTimeOffset.TryParseExact(valueToParse, candidateFormats, culture, DateTimeStyles.None, out result))
            {
                if (!formatHasOffset)
                {
                    result = ApplyDefaultOffset(result, valueToParse, options);
                }
                return true;
            }

            // ISO 8601 fractional-second tolerance: expand "ss" into ss.f, ss.ff, ... ss.fffffff
            if (IsIso8601Format(format))
            {
                var expandedFormats = ExpandIso8601Formats(format);
                if (DateTimeOffset.TryParseExact(valueToParse, expandedFormats, culture, DateTimeStyles.None, out result))
                {
                    if (!formatHasOffset)
                    {
                        result = ApplyDefaultOffset(result, valueToParse, options);
                    }
                    return true;
                }
            }
        }

        result = default;
        return false;
    }

    private static bool TryParseWithRecognizer(
        string value,
        CultureInfo culture,
        TokenizerOptions options,
        out DateTimeOffset result)
    {
        if (DatePatternRecognizer.TryRecognize(value, culture, out var formats) &&
            DateTimeOffset.TryParseExact(value, formats, culture, DateTimeStyles.None, out result))
        {
            result = ApplyDefaultOffset(result, value, options);
            return true;
        }

        result = default;
        return false;
    }

    private static (string value, bool stripped) StripOrdinalSuffixIfNeeded(string value, string format)
    {
        // Only strip when the format contains a day specifier
        if (format.Contains(" d ", StringComparison.Ordinal) ||
            format.Contains(" dd ", StringComparison.Ordinal) ||
            format.StartsWith("d ", StringComparison.Ordinal) ||
            format.StartsWith("dd ", StringComparison.Ordinal))
        {
            var stripped = OrdinalSuffixRegex().Replace(value, "${digits}");
            return (stripped, !string.Equals(stripped, value, StringComparison.Ordinal));
        }

        return (value, false);
    }

    private static string[] BuildCandidateFormats(string format, bool ordinalWasStripped)
    {
        if (!ordinalWasStripped)
        {
            return [format];
        }

        // Build a single-d-day variant by replacing "dd " or starting "dd" with "d"
        // Only replace where "dd" is a standalone day token, not part of "ddd"/"dddd".
        string? singleDVariant = null;

        if (format.StartsWith("dd ", StringComparison.Ordinal))
        {
            singleDVariant = "d " + format.Substring(3);
        }
        else if (format.Contains(" dd ", StringComparison.Ordinal))
        {
            var idx = format.IndexOf(" dd ", StringComparison.Ordinal);
            singleDVariant = format.Substring(0, idx) + " d " + format.Substring(idx + 4);
        }

        return singleDVariant != null ? [format, singleDVariant] : [format];
    }

    private static bool IsIso8601Format(string format)
    {
        return format.Contains("yyyy-MM-dd", StringComparison.Ordinal) &&
               format.Contains("T", StringComparison.Ordinal);
    }

    internal static string[] ExpandIso8601Formats(string baseFormat)
    {
        var ssIndex = baseFormat.IndexOf("ss", StringComparison.Ordinal);
        if (ssIndex < 0)
        {
            return [baseFormat];
        }

        var afterSs = ssIndex + 2;
        var before = baseFormat.Substring(0, afterSs);
        var after = afterSs < baseFormat.Length ? baseFormat.Substring(afterSs) : string.Empty;

        // base format + 7 fractional-second variants
        var result = new string[8];
        result[0] = baseFormat;
        for (var i = 1; i <= 7; i++)
        {
            result[i] = $"{before}.{new string('f', i)}{after}";
        }

        return result;
    }

    private static DateTimeOffset ApplyDefaultOffset(DateTimeOffset parsed, string originalValue, TokenizerOptions options)
    {
        // Explicit offset in the data always wins over defaults
        if (HasExplicitOffset(originalValue))
        {
            return parsed;
        }

        // DefaultOffset takes precedence over DefaultTimezone
        if (options.DefaultOffset.HasValue)
        {
            return new DateTimeOffset(parsed.DateTime, options.DefaultOffset.Value);
        }

        if (!string.IsNullOrEmpty(options.DefaultTimezone))
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(options.DefaultTimezone);
                var offset = tz.GetUtcOffset(parsed.DateTime);
                return new DateTimeOffset(parsed.DateTime, offset);
            }
            catch (TimeZoneNotFoundException)
            {
                // Unknown timezone — return as-is
            }
        }

        return parsed;
    }

    private static bool HasExplicitOffset(string value)
    {
        var trimmed = value.TrimEnd();

        if (trimmed.Length > 0 && trimmed[trimmed.Length - 1] == 'Z')
        {
            return true;
        }

        // Check for trailing +HH:mm or -HH:mm
        if (trimmed.Length >= 6)
        {
            var lastSix = trimmed.Substring(trimmed.Length - 6);
            return (lastSix[0] == '+' || lastSix[0] == '-') &&
                   char.IsDigit(lastSix[1]) &&
                   char.IsDigit(lastSix[2]) &&
                   lastSix[3] == ':' &&
                   char.IsDigit(lastSix[4]) &&
                   char.IsDigit(lastSix[5]);
        }

        return false;
    }

    /// <summary>
    /// Returns true if the format string contains a timezone/offset specifier
    /// (z, zz, zzz, or K), meaning the parsed value's offset came from the data.
    /// </summary>
    private static bool FormatContainsOffset(string format)
    {
        for (var i = 0; i < format.Length; i++)
        {
            var c = format[i];

            // Skip quoted literals
            if (c == '\'' || c == '"')
            {
                var quote = c;
                i++;
                while (i < format.Length && format[i] != quote) i++;
                continue;
            }

            if (c == 'z' || c == 'K') return true;
        }

        return false;
    }
}
