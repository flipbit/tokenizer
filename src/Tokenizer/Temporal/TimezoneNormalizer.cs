using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Tokens.Temporal;

/// <summary>
/// Normalizes timezone abbreviations in date/time strings by replacing them with
/// numeric UTC offsets. Runs as a pre-parse step before format recognition.
/// </summary>
[SuppressMessage("Meziantou.Analyzer", "MA0182", Justification = "Used by TemporalParser in Task 6")]
internal static partial class TimezoneNormalizer
{
    private static readonly Dictionary<string, TimeSpan> BuiltInAbbreviations = new(StringComparer.Ordinal)
    {
        ["UTC"] = TimeSpan.Zero,
        ["GMT"] = TimeSpan.Zero,
        ["WET"] = TimeSpan.Zero,
        ["CET"] = TimeSpan.FromHours(1),
        ["CEST"] = TimeSpan.FromHours(2),
        ["EET"] = TimeSpan.FromHours(2),
        ["EEST"] = TimeSpan.FromHours(3),
        ["MSK"] = TimeSpan.FromHours(3),
        ["JST"] = TimeSpan.FromHours(9),
        ["KST"] = TimeSpan.FromHours(9),
        ["NZST"] = TimeSpan.FromHours(12),
        ["NZDT"] = TimeSpan.FromHours(13),
    };

#if NET8_0_OR_GREATER
#pragma warning disable MA0009 // GeneratedRegex does not support matchTimeout; source-generated regex avoids ReDoS
    [System.Text.RegularExpressions.GeneratedRegex(@"\s*\(?(?<abbr>[A-Z]{2,5})\)?\s*$", RegexOptions.ExplicitCapture)]
    private static partial Regex TrailingAbbreviationRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"[+-]\d{2}:\d{2}\s*$", RegexOptions.ExplicitCapture)]
    private static partial Regex TrailingNumericOffsetRegex();
#pragma warning restore MA0009
#else
    private static readonly Regex TrailingAbbreviationRegexInstance =
        new(@"\s*\(?(?<abbr>[A-Z]{2,5})\)?\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));
    private static Regex TrailingAbbreviationRegex() => TrailingAbbreviationRegexInstance;

    private static readonly Regex TrailingNumericOffsetRegexInstance =
        new(@"[+-]\d{2}:\d{2}\s*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));
    private static Regex TrailingNumericOffsetRegex() => TrailingNumericOffsetRegexInstance;
#endif

    /// <summary>
    /// Replaces a trailing timezone abbreviation with its numeric UTC offset.
    /// Returns the input unchanged if no known abbreviation is found or if a
    /// numeric offset is already present.
    /// </summary>
    public static string Normalize(string value, IReadOnlyDictionary<string, TimeSpan> customAbbreviations)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;

        // Skip if already has a numeric offset
        if (TrailingNumericOffsetRegex().IsMatch(value)) return value;

        var match = TrailingAbbreviationRegex().Match(value);
        if (!match.Success) return value;

        var abbreviation = match.Groups["abbr"].Value;

        // Custom abbreviations override built-in
        if (!customAbbreviations.TryGetValue(abbreviation, out var offset) &&
            !BuiltInAbbreviations.TryGetValue(abbreviation, out offset))
        {
            return value;
        }

        var prefix = value.Substring(0, match.Index).TrimEnd();
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var abs = offset < TimeSpan.Zero ? offset.Negate() : offset;

        return string.Format(CultureInfo.InvariantCulture, "{0} {1}{2:D2}:{3:D2}", prefix, sign, abs.Hours, abs.Minutes);
    }
}
