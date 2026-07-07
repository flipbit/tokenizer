using System.Globalization;
using System.Text.RegularExpressions;
using Tokens.Extensions;

namespace Tokens.Transformers;

/// <summary>
/// Converts the token value to a <see cref="DateTime"/>
/// </summary>
public sealed partial class ToDateTimeTransformer : ITokenTransformer
{
    private static readonly Dictionary<string, string[]> MonthAbbreviations;
    private static readonly object LockHandle;
#if NET8_0_OR_GREATER
#pragma warning disable MA0009 // GeneratedRegex does not support matchTimeout; source-generated regex avoids ReDoS
    [System.Text.RegularExpressions.GeneratedRegex(@"\b(?<digits>\d+)(?:st|nd|rd|th)\b", RegexOptions.ExplicitCapture)]
    private static partial Regex OrdinalSuffixRegex();
#pragma warning restore MA0009
#else
    private static readonly Regex OrdinalSuffixRegexInstance = new(@"\b(?<digits>\d+)(?:st|nd|rd|th)\b", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(-1));
    private static Regex OrdinalSuffixRegex() => OrdinalSuffixRegexInstance;
#endif

    static ToDateTimeTransformer()
    {
        MonthAbbreviations = new Dictionary<string, string[]>(StringComparer.Ordinal);
        LockHandle = new object();
    }

    /// <inheritdoc />
    public bool TryTransform(object value, string[] args, out object transformed)
    {
        if (TryParseDateTime(value, args, out var result))
        {
            transformed = result;
            return true;
        }

        transformed = value;

        return false;
    }

    /// <summary>
    /// Attempts to parse a date/time value using the specified format strings and invariant culture.
    /// </summary>
    /// <param name="value">The value to parse</param>
    /// <param name="formats">An array of format strings to try, or null/empty for default parsing</param>
    /// <param name="result">The parsed <see cref="DateTime"/> if successful; otherwise <see cref="DateTime.MinValue"/></param>
    /// <returns>true if parsing succeeded; otherwise false</returns>
    public static bool TryParseDateTime(object value, string[] formats, out DateTime result)
    {
        return TryParseDateTime(value, formats, DateTimeStyles.None, out result);
    }

    /// <summary>
    /// Attempts to parse a date/time value using the specified format strings, date/time styles, and locale-aware cultures.
    /// </summary>
    /// <param name="value">The value to parse</param>
    /// <param name="formats">An array of format strings to try, or null/empty for default parsing</param>
    /// <param name="dateTimeStyles">Style flags to apply during parsing (e.g., AssumeUniversal)</param>
    /// <param name="result">The parsed <see cref="DateTime"/> if successful; otherwise <see cref="DateTime.MinValue"/></param>
    /// <returns>true if parsing succeeded; otherwise false</returns>
    public static bool TryParseDateTime(object value, string[] formats, DateTimeStyles dateTimeStyles, out DateTime result)
    {
        if (value?.ToString() is not { Length: > 0 } rawString)
        {
            result = default;
            return false;
        }

        var valueString = rawString.SubstringBeforeNewLine();

        if (string.IsNullOrWhiteSpace(valueString))
        {
            result = default;
            return false;
        }

        var cultures = GetCultures(valueString, formats);

        foreach (var culture in cultures)
        {
            if (formats == null || formats.Length == 0 || string.IsNullOrEmpty(formats[0]))
            {
                if (DateTime.TryParse(valueString, culture, dateTimeStyles, out result))
                {
                    return true;
                }
            }
            else
            {
                foreach (var format in formats)
                {
                    if (string.IsNullOrWhiteSpace(format)) continue;

                    var valueToFormat = valueString;

                    // Remove day ordinals
                    if (format.Contains(" d ", StringComparison.Ordinal) ||
                        format.Contains(" dd ", StringComparison.Ordinal) ||
                        format.StartsWith("d ", StringComparison.Ordinal) ||
                        format.StartsWith("dd ", StringComparison.Ordinal))
                    {
                        valueToFormat = OrdinalSuffixRegex().Replace(valueToFormat, "${digits}");
                    }

                    if (DateTime.TryParseExact(valueToFormat, format, culture, dateTimeStyles, out result))
                    {
                        return true;
                    }
                }
            }

        }

        result = default;

        return false;
    }

    private static IEnumerable<CultureInfo> GetCultures(string value, IReadOnlyCollection<string> formats)
    {
        var cultures = new List<CultureInfo> { CultureInfo.InvariantCulture };

        if (value == null) return cultures;
        if (formats == null) return cultures;
        if (formats.Count < 1) return cultures;

        InitializeCulture("es-US");
        InitializeCulture("es-ES");

        foreach (var format in formats)
        {
            if (string.IsNullOrWhiteSpace(format)) continue;

            if (!format.Contains("MMM", StringComparison.Ordinal)) continue;

            foreach (var key in MonthAbbreviations.Keys)
            {
                foreach (var abbreviation in MonthAbbreviations[key])
                {
                    if (value.IndexOf(abbreviation, StringComparison.InvariantCultureIgnoreCase) <= -1) continue;

                    cultures.Add(CultureInfo.GetCultureInfo(key));

                    break;
                }
            }
        }

        return cultures;
    }

    private static void InitializeCulture(string code)
    {
        if (MonthAbbreviations.ContainsKey(code)) return;

        lock (LockHandle)
        {
            if (MonthAbbreviations.ContainsKey(code)) return;

            try
            {
                var culture = CultureInfo.GetCultureInfo(code);

                var list = culture
                    .DateTimeFormat
                    .AbbreviatedMonthNames
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToArray();

                MonthAbbreviations.Add(code, list);
            }
            catch (CultureNotFoundException)
            {
            }
        }
    }
}
