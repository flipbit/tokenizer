using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Tokens.Validators;

/// <summary>
/// Validator to determine if a token value matches a regular expression pattern.
/// Patterns are cached for performance. The cache is bounded to <see cref="MaxCacheSize"/>
/// entries; patterns should be finite and developer-controlled (not user input).
/// </summary>
public sealed class MatchesRegexValidator : ITokenValidator
{
    private const int MaxCacheSize = 1024;
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Determines whether the specified token is valid.
    /// </summary>
    public bool IsValid(object value, params string[] args)
    {
        if (args == null || args.Length == 0)
        {
            throw new ArgumentException("MatchesRegex(pattern): missing argument — you must specify a regex pattern", nameof(args));
        }

        if (value == null) return false;

        var valueString = value.ToString();

        if (string.IsNullOrEmpty(valueString)) return false;

        if (RegexCache.Count >= MaxCacheSize)
        {
            RegexCache.Clear();
        }

        var regex = RegexCache.GetOrAdd(args[0],
            pattern => new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1)));

        return regex.IsMatch(valueString);
    }
}
