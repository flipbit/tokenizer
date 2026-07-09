using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Tokens.Temporal;

/// <summary>
/// Identifies the date/time format of a string using an ordered list of regex-based recognizers.
/// The recognizer only identifies the format; actual parsing is done by the caller.
/// </summary>
[SuppressMessage("Meziantou.Analyzer", "MA0182", Justification = "Used by TemporalParser in Task 6")]
internal static class DatePatternRecognizer
{
    // Fractional second variants: .f through .fffffff
    private static readonly string[] FractionalSecondSuffixes =
    [
        ".f", ".ff", ".fff", ".ffff", ".fffff", ".ffffff", ".fffffff",
    ];

    /// <summary>
    /// Builds an array of format strings for ISO 8601 datetime with the given suffix,
    /// expanding into variants with 1-7 fractional second digits.
    /// </summary>
    private static string[] Iso8601Formats(string baseSuffix, string offsetPart)
    {
        // e.g. baseSuffix = "zzz", offsetPart = "zzz"  → yields  yyyy-MM-ddTHH:mm:sszzz, yyyy-MM-ddTHH:mm:ss.fzzz, ...
        var formats = new string[1 + FractionalSecondSuffixes.Length];
        formats[0] = "yyyy-MM-ddTHH:mm:ss" + baseSuffix;
        for (var i = 0; i < FractionalSecondSuffixes.Length; i++)
        {
            formats[i + 1] = "yyyy-MM-ddTHH:mm:ss" + FractionalSecondSuffixes[i] + offsetPart;
        }

        return formats;
    }

    /// <summary>
    /// Builds an array of format strings for a space-separated datetime with fractional seconds.
    /// </summary>
    private static string[] FractionalFormats(string dateTimePart)
    {
        // e.g. "yyyy-MM-dd HH:mm:ss" → yields .f, .ff, ... .fffffff variants
        var formats = new string[FractionalSecondSuffixes.Length];
        for (var i = 0; i < FractionalSecondSuffixes.Length; i++)
        {
            formats[i] = dateTimePart + FractionalSecondSuffixes[i];
        }

        return formats;
    }

    internal sealed class Recognizer
    {
        internal Recognizer(string pattern, string[] formats, bool requiresCulture = false)
        {
            Regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));
            Formats = formats;
            RequiresCulture = requiresCulture;
        }

        internal Recognizer(string pattern, string format, bool requiresCulture = false)
            : this(pattern, [format], requiresCulture)
        {
        }

        internal Regex Regex { get; }
        internal string[] Formats { get; }

        /// <summary>
        /// When true, the recognizer uses locale-aware month/day name patterns.
        /// </summary>
        internal bool RequiresCulture { get; }
    }

    // -------------------------------------------------------------------------
    // Static recognizer list — ordered most-specific first (priorities 1-32)
    // -------------------------------------------------------------------------

    private static readonly IReadOnlyList<Recognizer> StaticRecognizers = new List<Recognizer>
    {
        // 1. ISO 8601 with numeric offset: 2024-01-15T14:30:00+05:00 or -05:00
        new(
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?[+-]\d{2}:\d{2}$",
            Iso8601Formats("zzz", "zzz")),

        // 2. ISO 8601 with Z: 2024-01-15T14:30:00Z
        new(
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?Z$",
            Iso8601Formats("Z", "Z")),

        // 3. ISO 8601 no offset: 2024-01-15T14:30:00
        new(
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d{1,7})?$",
            Iso8601Formats(string.Empty, string.Empty)),

        // 4. RFC 2822 / asctime: Tue Mar 5 14:30:00 GMT 2024
        new(
            @"^[A-Za-z]{3}\s+[A-Za-z]{3}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2}\s+GMT\s+\d{4}$",
            @"ddd MMM d HH:mm:ss \G\M\T yyyy",
            requiresCulture: true),

        // 5. Day/month/year with time + offset (no space before offset): 15/01/2024 14:30:00+05:00
        new(
            @"^\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$",
            "dd/MM/yyyy HH:mm:sszzz"),

        // 6. Year-month-day with time + offset (space before offset): 2024-01-15 14:30:00 +05:00
        new(
            @"^\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\s+[+-]\d{2}:\d{2}$",
            "yyyy-MM-dd HH:mm:ss zzz"),

        // 7. Year.month.day with time: 2024.01.15 14:30:00
        new(
            @"^\d{4}\.\d{2}\.\d{2}\s+\d{2}:\d{2}:\d{2}$",
            "yyyy.MM.dd HH:mm:ss"),

        // 8. Year-month-day with time: 2024-01-15 14:30:00
        new(
            @"^\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}$",
            "yyyy-MM-dd HH:mm:ss"),

        // 9. Day-monthname-year with time: 15-Mar-2024 14:30:00
        //    Require exactly 3 alpha chars for short month name to avoid collision with priority 15.
        new(
            @"^\d{2}-[A-Za-z]{3}-\d{4}\s+\d{2}:\d{2}:\d{2}$",
            "dd-MMM-yyyy HH:mm:ss",
            requiresCulture: true),

        // 10. Dayname day fullmonthname year: Tuesday 15 March 2024
        new(
            @"^[A-Za-z]+\s+\d{1,2}\s+[A-Za-z]+\s+\d{4}$",
            "dddd d MMMM yyyy",
            requiresCulture: true),

        // 11. Day monthname year with time+offset: 15 Mar 2024 14:30+05:00
        new(
            @"^\d{2}\s+[A-Za-z]+\s+\d{4}\s+\d{2}:\d{2}[+-]\d{2}:\d{2}$",
            "dd MMM yyyy HH:mmzzz",
            requiresCulture: true),

        // 12. Monthname day, year: March 15, 2024
        new(
            @"^[A-Za-z]+\s+\d{1,2},\s+\d{4}$",
            "MMMM d, yyyy",
            requiresCulture: true),

        // 13. Day-monthname-year (short abbrev, 3 chars): 15-Mar-2024
        new(
            @"^\d{2}-[A-Za-z]{3}-\d{4}$",
            "dd-MMM-yyyy",
            requiresCulture: true),

        // 14. Day monthname year (short abbrev, 3 chars): 15 Mar 2024 or 1 May 2019
        new(
            @"^\d{1,2}\s+[A-Za-z]{3}\s+\d{4}$",
            ["dd MMM yyyy", "d MMM yyyy"],
            requiresCulture: true),

        // 15. Day-fullmonth-year (4+ chars): 15-March-2024
        new(
            @"^\d{2}-[A-Za-z]{4,}-\d{4}$",
            "dd-MMMM-yyyy",
            requiresCulture: true),

        // 16. Day.month.year with time: 15.01.2024 14:30:00
        new(
            @"^\d{2}\.\d{2}\.\d{4}\s+\d{2}:\d{2}:\d{2}$",
            "dd.MM.yyyy HH:mm:ss"),

        // 17. Year-month-day with fractional seconds (no T): 2024-01-15 14:30:00.50
        new(
            @"^\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{1,7}$",
            FractionalFormats("yyyy-MM-dd HH:mm:ss")),

        // 18. Korean style: 2024. 01. 15.
        new(
            @"^\d{4}\.\s+\d{2}\.\s+\d{2}\.$",
            "yyyy. MM. dd."),

        // 19. Turkish style: 2024-Mar-15.
        new(
            @"^\d{4}-[A-Za-z]+-\d{2}\.$",
            "yyyy-MMM-dd.",
            requiresCulture: true),

        // 20. Year/month/day with time: 2024/01/15 14:30:00
        new(
            @"^\d{4}/\d{2}/\d{2}\s+\d{2}:\d{2}:\d{2}$",
            "yyyy/MM/dd HH:mm:ss"),

        // 21. Day/month/year with time: 15/01/2024 14:30:00
        new(
            @"^\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}:\d{2}$",
            "dd/MM/yyyy HH:mm:ss"),

        // 22. Year-month-day: 2024-01-15
        new(
            @"^\d{4}-\d{2}-\d{2}$",
            "yyyy-MM-dd"),

        // 23. Year.month.day: 2024.01.15
        new(
            @"^\d{4}\.\d{2}\.\d{2}$",
            "yyyy.MM.dd"),

        // 24. Year/month/day: 2024/01/15
        new(
            @"^\d{4}/\d{2}/\d{2}$",
            "yyyy/MM/dd"),

        // 25. Day.month.year: 15.01.2024
        new(
            @"^\d{2}\.\d{2}\.\d{4}$",
            "dd.MM.yyyy"),

        // 26/28. Day/month/year or Month/day/year — culture-dependent (handled separately below)
        // Placeholder: will be skipped in the static list, handled by TryRecognize

        // 27. Day-month-year: 15-01-2024
        new(
            @"^\d{2}-\d{2}-\d{4}$",
            "dd-MM-yyyy"),

        // 29. Compact with time: 20240115143000
        new(
            @"^\d{14}$",
            "yyyyMMddHHmmss"),

        // 30. Compact date + space + time: 20240115 14:30:00
        new(
            @"^\d{8}\s+\d{2}:\d{2}:\d{2}$",
            "yyyyMMdd HH:mm:ss"),

        // 31. Compact date: 20240115
        new(
            @"^\d{8}$",
            "yyyyMMdd"),

        // 32. Relaxed day.month.year: 5.1.2024
        new(
            @"^\d{1,2}\.\d{1,2}\.\d{4}$",
            "d.M.yyyy"),
    };

    // Recognizer for dd/MM/yyyy (slash) — culture-dependent ordering handled separately
#pragma warning disable MA0110 // Use [GeneratedRegex] — not used here; this is part of a static lookup table, not a hot-path recognizer
    private static readonly Regex SlashNumericDateRegex =
        new(@"^\d{2}/\d{2}/\d{4}$", RegexOptions.Compiled | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));
#pragma warning restore MA0110

    /// <summary>
    /// Tries to identify the date/time format of <paramref name="value"/> by matching
    /// against the ordered list of recognizers.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a recognizer matched; <paramref name="formats"/> contains
    /// the candidate format strings to pass to <c>TryParseExact</c>.
    /// </returns>
    internal static bool TryRecognize(string value, CultureInfo culture, [NotNullWhen(true)] out string[]? formats)
    {
        formats = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Priority 26/28: numeric dd/MM/yyyy vs MM/dd/yyyy — resolve via culture before scanning StaticRecognizers
        // These share the same regex shape so we handle them inline.
        if (SlashNumericDateRegex.IsMatch(value))
        {
            formats = BuildSlashDateFormats(culture);
            return true;
        }

        // CodeQL cs/linq/missed-where: foreach+if is used intentionally to avoid LINQ allocation overhead
        foreach (var recognizer in StaticRecognizers)
        {
            if (recognizer.Regex.IsMatch(value))
            {
                formats = recognizer.Formats;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the format string array for ambiguous slash-separated numeric dates
    /// (e.g. 01/02/2024), ordered by culture preference.
    /// </summary>
    private static string[] BuildSlashDateFormats(CultureInfo culture)
    {
        // Check whether the culture prefers MM/dd or dd/MM by inspecting ShortDatePattern.
        // A pattern that places 'M' before 'd' (e.g. "M/d/yyyy") prefers month-first ordering.
        var shortDatePattern = culture.DateTimeFormat.ShortDatePattern;
        var mmBeforeDd = FirstIndexOf(shortDatePattern, 'M') < FirstIndexOf(shortDatePattern, 'd');

        return mmBeforeDd
            ? ["MM/dd/yyyy", "dd/MM/yyyy"]
            : ["dd/MM/yyyy", "MM/dd/yyyy"];
    }

    /// <summary>
    /// Returns the index of the first occurrence of <paramref name="ch"/> in <paramref name="s"/>,
    /// or <see cref="int.MaxValue"/> if not found. Uses an ordinal character scan to avoid
    /// cross-target StringComparison overload conflicts (MA0001/MA0089).
    /// </summary>
    private static int FirstIndexOf(string s, char ch)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == ch)
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    /// <summary>
    /// Exposes the ordered list of recognizers for diagnostic and tooling purposes.
    /// </summary>
    internal static IReadOnlyList<Recognizer> Recognizers => StaticRecognizers;
}
